using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// Inspector de ZonaCalva (añadir esferas / aplicar a la malla) y menú
/// GasLeaking > Añadir peine y zona calva.
[CustomEditor(typeof(ZonaCalva))]
public class ZonaCalvaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var z = (ZonaCalva)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1) 'Añadir esfera' y colócala/escálala sobre la zona calva (escala X = diámetro).\n" +
            "2) Pulsa 'Aplicar a la malla'. Los puntos rojos muestran lo que quedará calvo.",
            MessageType.Info);
        if (GUILayout.Button("Añadir esfera")) HerramientasCalvicie.AnadirEsfera(z);
        if (GUILayout.Button("Aplicar a la malla", GUILayout.Height(30))) HerramientasCalvicie.Aplicar(z);
    }
}

public static class HerramientasCalvicie
{
    const string Carpeta = "Assets/PrototipoCabeza";
    const string RutaEscena = "Assets/Scenes/PrototipoCabeza.unity";

    // ---------------- Zona calva ----------------

    public static Transform AsegurarContenedor(ZonaCalva z)
    {
        if (z.contenedorEsferas) return z.contenedorEsferas;
        var padre = z.transform.parent;
        var existente = padre ? padre.Find("ZonaCalva_Esferas") : null;
        if (existente == null)
        {
            var go = new GameObject("ZonaCalva_Esferas");
            Undo.RegisterCreatedObjectUndo(go, "Zona calva");
            go.transform.SetParent(padre, false);
            existente = go.transform;
        }
        Undo.RecordObject(z, "Zona calva");
        z.contenedorEsferas = existente;
        return existente;
    }

    public static Transform AnadirEsfera(ZonaCalva z, Vector3? posicion = null, float diametro = 0.3f)
    {
        var cont = AsegurarContenedor(z);
        var go = new GameObject("Esfera_" + (cont.childCount + 1));
        Undo.RegisterCreatedObjectUndo(go, "Añadir esfera");
        go.transform.SetParent(cont, false);
        Vector3 pos = posicion ?? (z.malla ? z.malla.GetComponent<Renderer>().bounds.center + Vector3.up * z.malla.GetComponent<Renderer>().bounds.extents.y : z.transform.position);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * diametro;
        Selection.activeGameObject = go;
        return go.transform;
    }

    public static void Aplicar(ZonaCalva z)
    {
        if (!z.malla || !z.malla.sharedMesh) { Debug.LogError("[ZonaCalva] Asigna la malla."); return; }
        var src = z.malla.sharedMesh;
        var tr = z.malla.transform;
        var v = src.vertices;
        int n = src.subMeshCount;
        int calva = Mathf.Max(1, z.submallaCalva);
        bool nueva = calva >= n;
        if (nueva) calva = n;
        int total = nueva ? n + 1 : n;

        var listas = new List<int>[total];
        for (int i = 0; i < total; i++) listas[i] = new List<int>();
        int trisCalva = 0;
        for (int s = 0; s < n; s++)
        {
            var t = src.GetTriangles(s);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                bool dentro = z.Contiene(tr.TransformPoint(c));
                int destino = dentro ? calva : (s == calva ? 0 : s);   // lo que deja de ser calvo vuelve a la piel
                if (dentro) trisCalva++;
                listas[destino].Add(t[i]); listas[destino].Add(t[i + 1]); listas[destino].Add(t[i + 2]);
            }
        }

        // Si la malla es nuestra (.asset en la carpeta del prototipo) se modifica; si no, se crea una copia.
        string rutaSrc = AssetDatabase.GetAssetPath(src);
        Mesh destinoMalla;
        if (rutaSrc.StartsWith(Carpeta + "/Mallas/") && rutaSrc.EndsWith(".asset"))
        {
            Undo.RecordObject(src, "Aplicar zona calva");
            destinoMalla = src;
        }
        else
        {
            destinoMalla = Object.Instantiate(src);
            destinoMalla.name = src.name + "_ZonaCalva";
            string ruta = AssetDatabase.GenerateUniqueAssetPath($"{Carpeta}/Mallas/{destinoMalla.name}.asset");
            AssetDatabase.CreateAsset(destinoMalla, ruta);
        }
        destinoMalla.subMeshCount = total;
        for (int s = 0; s < total; s++) destinoMalla.SetTriangles(listas[s], s);
        EditorUtility.SetDirty(destinoMalla);

        Undo.RecordObject(z.malla, "Aplicar zona calva");
        z.malla.sharedMesh = destinoMalla;

        var mr = z.malla.GetComponent<MeshRenderer>();
        if (mr && mr.sharedMaterials.Length < total)
        {
            Undo.RecordObject(mr, "Aplicar zona calva");
            var mats = new List<Material>(mr.sharedMaterials);
            while (mats.Count < total) mats.Add(z.materialCalva ? z.materialCalva : mats[0]);
            mr.sharedMaterials = mats.ToArray();
        }
        var mc = z.malla.GetComponent<MeshCollider>();
        if (mc) { Undo.RecordObject(mc, "Aplicar zona calva"); mc.sharedMesh = destinoMalla; }

        Undo.RecordObject(z, "Aplicar zona calva");
        z.submallaCalva = calva;

        var gestor = Object.FindFirstObjectByType<GestorMechones>();
        if (gestor && gestor.mallaCabeza == z.malla)
        {
            Undo.RecordObject(gestor, "Aplicar zona calva");
            gestor.submallaCalva = calva;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(z.gameObject.scene);
        Debug.Log($"[ZonaCalva] Aplicada: {trisCalva} triángulos calvos (submalla {calva}).");
    }

    // ---------------- Menú de configuración ----------------

    [MenuItem("GasLeaking/Añadir peine y zona calva")]
    public static void Configurar()
    {
        var activa = EditorSceneManager.GetActiveScene();
        if (activa.path != RutaEscena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            activa = EditorSceneManager.OpenScene(RutaEscena);
        }
        var cabeza = GameObject.Find("Cabeza");
        var canvas = GameObject.Find("Canvas");
        var gestor = Object.FindFirstObjectByType<GestorMechones>();
        if (!cabeza || !canvas || !gestor) { Debug.LogError("[Peine] Falta Cabeza, Canvas o SistemaMechones (usa antes 'Añadir sistema de mechones')."); return; }

        // Prefab del mechón con parche + pelos (solo si aún es el antiguo sin pelos)
        string rutaPrefab = $"{Carpeta}/Prefabs/Mechon.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        if (prefab == null || prefab.transform.Find("Pelos") == null)
            gestor.prefabMechon = CrearPrefabMechon(rutaPrefab);

        // Zona calva editable
        var z = cabeza.GetComponent<ZonaCalva>();
        if (!z) z = cabeza.AddComponent<ZonaCalva>();
        z.malla = cabeza.GetComponent<MeshFilter>();
        z.submallaCalva = gestor.submallaCalva;
        z.materialCalva = AssetDatabase.LoadAssetAtPath<Material>($"{Carpeta}/Materiales/PielCalva.mat");
        var cont = AsegurarContenedor(z);
        if (cont.childCount == 0)
            AnadirEsfera(z, cabeza.transform.TransformPoint(new Vector3(0f, 1f, -0.35f).normalized * 0.5f), 0.75f);

        // Peine
        var peine = gestor.GetComponent<Peine>();
        if (!peine) peine = gestor.gameObject.AddComponent<Peine>();
        peine.gestor = gestor;

        Borrar("BotonPeine");
        var cursorViejo = canvas.transform.Find("CursorPeine");   // puede estar inactivo
        if (cursorViejo) Object.DestroyImmediate(cursorViejo.gameObject);
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var botonGO = new GameObject("BotonPeine", typeof(RectTransform), typeof(Image), typeof(Button));
        botonGO.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)botonGO.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(30f, 115f);
        rt.sizeDelta = new Vector2(260f, 70f);
        var img = botonGO.GetComponent<Image>();
        img.sprite = sprite; img.type = Image.Type.Sliced;
        var boton = botonGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(boton.onClick, peine.Alternar);

        var textoGO = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        textoGO.transform.SetParent(botonGO.transform, false);
        var trt = (RectTransform)textoGO.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        var texto = textoGO.GetComponent<Text>();
        texto.font = fuente; texto.fontSize = 28; texto.text = "Peine";
        texto.alignment = TextAnchor.MiddleCenter; texto.color = new Color(0.15f, 0.15f, 0.15f);

        var cursorGO = new GameObject("CursorPeine", typeof(RectTransform), typeof(Image));
        cursorGO.transform.SetParent(canvas.transform, false);
        var crt = (RectTransform)cursorGO.transform;
        crt.sizeDelta = new Vector2(80f, 22f);
        var cimg = cursorGO.GetComponent<Image>();
        cimg.sprite = sprite; cimg.type = Image.Type.Sliced;
        cimg.color = new Color(0.95f, 0.85f, 0.55f, 0.9f);
        cimg.raycastTarget = false;
        cursorGO.transform.SetAsLastSibling();
        cursorGO.SetActive(false);

        peine.fondoBoton = img;
        peine.textoBoton = texto;
        peine.cursorPeine = crt;

        EditorUtility.SetDirty(gestor);
        EditorSceneManager.MarkSceneDirty(activa);
        EditorSceneManager.SaveScene(activa);
        Selection.activeGameObject = cabeza;
        Debug.Log("[Peine] Peine y zona calva añadidos.");
    }

    public static Mechon CrearPrefabMechon(string ruta)
    {
        if (!AssetDatabase.IsValidFolder($"{Carpeta}/Prefabs")) AssetDatabase.CreateFolder(Carpeta, "Prefabs");
        string rutaMat = $"{Carpeta}/Materiales/Pelo.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.2f, 0.12f, 0.07f));
            mat.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(mat, rutaMat);
        }

        var raiz = new GameObject("Mechon");
        var col = raiz.AddComponent<BoxCollider>();
        col.size = new Vector3(0.26f, 0.08f, 0.32f);

        var parche = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        parche.name = "Parche";
        Object.DestroyImmediate(parche.GetComponent<Collider>());
        parche.transform.SetParent(raiz.transform, false);
        parche.transform.localScale = new Vector3(0.24f, 0.035f, 0.3f);
        parche.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var pelos = new GameObject("Pelos").transform;
        pelos.SetParent(raiz.transform, false);
        var rnd = new System.Random(7);
        for (int i = 0; i < 16; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rr = Mathf.Sqrt((float)rnd.NextDouble()) * 0.8f;
            var pelo = new GameObject("Pelo_" + (i + 1)).transform;
            pelo.SetParent(pelos, false);
            pelo.localPosition = new Vector3(Mathf.Cos(ang) * 0.11f * rr, 0.012f, Mathf.Sin(ang) * 0.14f * rr - 0.03f);
            float yaw = ((float)rnd.NextDouble() - 0.5f) * 30f;
            float elev = 8f + (float)rnd.NextDouble() * 12f;
            pelo.localRotation = Quaternion.Euler(-elev, yaw, 0f);

            float largo = 0.14f + (float)rnd.NextDouble() * 0.06f;
            var hebra = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hebra.name = "Hebra";
            Object.DestroyImmediate(hebra.GetComponent<Collider>());
            hebra.transform.SetParent(pelo, false);
            hebra.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hebra.transform.localPosition = new Vector3(0f, 0f, largo * 0.5f);
            hebra.transform.localScale = new Vector3(0.016f, largo * 0.5f, 0.016f);
            hebra.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        var m = raiz.AddComponent<Mechon>();
        m.contenedorPelos = pelos;
        var p = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        return p.GetComponent<Mechon>();
    }

    static void Borrar(string nombre)
    {
        var go = GameObject.Find(nombre);
        if (go != null) Object.DestroyImmediate(go);
    }
}
