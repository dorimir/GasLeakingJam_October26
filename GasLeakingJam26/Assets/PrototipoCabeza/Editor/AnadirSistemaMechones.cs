using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// Menú: GasLeaking > Añadir sistema de mechones
/// Añade a la escena PrototipoCabeza el gestor de mechones y la UI de calvicie, sin regenerar el resto.
/// Se puede volver a ejecutar: reemplaza solo lo que crea. El prefab y el material solo se crean si no existen.
public static class AnadirSistemaMechones
{
    const string Carpeta = "Assets/PrototipoCabeza";
    const string RutaEscena = "Assets/Scenes/PrototipoCabeza.unity";

    [MenuItem("GasLeaking/Añadir sistema de mechones")]
    public static void Anadir()
    {
        var activa = EditorSceneManager.GetActiveScene();
        if (activa.path != RutaEscena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            activa = EditorSceneManager.OpenScene(RutaEscena);
        }

        var prefab = ObtenerPrefabMechon();

        var cabeza = GameObject.Find("Cabeza");
        var canvas = GameObject.Find("Canvas");
        if (cabeza == null || canvas == null)
        {
            Debug.LogError("[Mechones] No encuentro 'Cabeza' o 'Canvas' en la escena.");
            return;
        }

        Borrar("SistemaMechones");
        Borrar("PanelCalvicie");
        Borrar("TextoMechones");
        Borrar("PanelMechones");

        // ---- UI ----
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var panel = NuevoUI("PanelCalvicie", canvas.transform);
        Anclar(panel, new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(640f, 100f));

        var texto = NuevoTexto("TextoCalvicie", panel, fuente, 34, "Calvicie: 100%");
        Estirar(texto.rectTransform, new Vector2(0f, 0.5f), Vector2.one, Vector2.zero);

        var fondo = NuevoUI("FondoBarra", panel).gameObject.AddComponent<Image>();
        fondo.sprite = sprite; fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        fondo.raycastTarget = false;
        Estirar(fondo.rectTransform, Vector2.zero, new Vector2(1f, 0.45f), Vector2.zero);

        var relleno = NuevoUI("RellenoBarra", fondo.transform).gameObject.AddComponent<Image>();
        relleno.sprite = null;
        relleno.raycastTarget = false;
        relleno.type = Image.Type.Filled;
        relleno.fillMethod = Image.FillMethod.Horizontal;
        relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
        relleno.fillAmount = 1f;
        relleno.color = new Color(0.9f, 0.3f, 0.25f);
        Estirar(relleno.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 5f));

        // Panel lateral izquierdo: pulsa y arrastra desde aquí para coger un mechón
        var lateral = NuevoUI("PanelMechones", canvas.transform);
        lateral.anchorMin = lateral.anchorMax = lateral.pivot = new Vector2(0f, 0.5f);
        lateral.anchoredPosition = new Vector2(30f, 0f);
        lateral.sizeDelta = new Vector2(200f, 300f);
        var fondoLateral = lateral.gameObject.AddComponent<Image>();
        fondoLateral.sprite = sprite; fondoLateral.type = Image.Type.Sliced;
        fondoLateral.color = new Color(0.08f, 0.08f, 0.1f, 0.75f);
        var panelUI = lateral.gameObject.AddComponent<PanelMechonesUI>();

        var titulo = NuevoTexto("Titulo", lateral, fuente, 26, "Mechones");
        Estirar(titulo.rectTransform, new Vector2(0f, 0.8f), Vector2.one, Vector2.zero);

        var icono = NuevoUI("Icono", lateral).gameObject.AddComponent<Image>();
        icono.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        icono.color = new Color(0.25f, 0.15f, 0.09f);
        icono.raycastTarget = false;
        var irt = icono.rectTransform;
        irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = new Vector2(0f, 10f);
        irt.sizeDelta = new Vector2(90f, 130f);

        var textoMechones = NuevoTexto("TextoMechones", lateral, fuente, 34, "x 0");
        Estirar(textoMechones.rectTransform, Vector2.zero, new Vector2(1f, 0.22f), Vector2.zero);

        // ---- Gestor ----
        var sistema = new GameObject("SistemaMechones");
        var gestor = sistema.AddComponent<GestorMechones>();
        gestor.prefabMechon = prefab;
        gestor.colliderCabeza = cabeza.GetComponent<Collider>();
        gestor.mallaCabeza = cabeza.GetComponent<MeshFilter>();
        gestor.camara = Camera.main;
        gestor.rellenoBarra = relleno;
        gestor.textoCalvicie = texto;
        gestor.textoMechones = textoMechones;
        panelUI.gestor = gestor;

        EditorSceneManager.MarkSceneDirty(activa);
        EditorSceneManager.SaveScene(activa);
        Selection.activeGameObject = sistema;
        Debug.Log("[Mechones] Sistema de mechones añadido.");
    }

    static Mechon ObtenerPrefabMechon()
    {
        if (!AssetDatabase.IsValidFolder($"{Carpeta}/Prefabs")) AssetDatabase.CreateFolder(Carpeta, "Prefabs");
        string ruta = $"{Carpeta}/Prefabs/Mechon.prefab";
        var existente = AssetDatabase.LoadAssetAtPath<Mechon>(ruta);
        if (existente != null) return existente;

        string rutaMat = $"{Carpeta}/Materiales/Pelo.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.2f, 0.12f, 0.07f));
            mat.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(mat, rutaMat);
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // incluye SphereCollider
        go.name = "Mechon";
        go.transform.localScale = new Vector3(0.24f, 0.05f, 0.3f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<Mechon>();
        var p = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        return p.GetComponent<Mechon>();
    }

    static void Borrar(string nombre)
    {
        var go = GameObject.Find(nombre);
        if (go != null) Object.DestroyImmediate(go);
    }

    static RectTransform NuevoUI(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    static Text NuevoTexto(string nombre, Transform padre, Font fuente, int tam, string contenido)
    {
        var t = NuevoUI(nombre, padre).gameObject.AddComponent<Text>();
        t.font = fuente; t.fontSize = tam; t.text = contenido;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        var sombra = t.gameObject.AddComponent<Shadow>();
        sombra.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt, Vector2 min, Vector2 max, Vector2 margen)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = margen; rt.offsetMax = -margen;
    }
}
