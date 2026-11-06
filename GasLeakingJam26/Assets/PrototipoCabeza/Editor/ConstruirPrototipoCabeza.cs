using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Unity.Cinemachine;

/// Menú: GasLeaking > Construir prototipo cabeza
/// Genera la escena Assets/Scenes/PrototipoCabeza.unity con busto, cámara orbital y botón de restaurar vista.
public static class ConstruirPrototipoCabeza
{
    const string Carpeta = "Assets/PrototipoCabeza";
    const string RutaEscena = "Assets/Scenes/PrototipoCabeza.unity";

    [MenuItem("GasLeaking/Construir prototipo cabeza")]
    public static void Construir()
    {
        CrearCarpeta("Assets", "PrototipoCabeza");
        CrearCarpeta(Carpeta, "Materiales");
        CrearCarpeta(Carpeta, "Mallas");
        CrearCarpeta("Assets", "Scenes");

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- Materiales ----
        var matPiel = Material("Piel", new Color(0.93f, 0.76f, 0.64f), 0.3f);
        var matCalva = Material("PielCalva", new Color(0.97f, 0.78f, 0.68f), 0.85f);
        var matOjo = Material("Ojo", new Color(0.12f, 0.1f, 0.1f), 0.9f);
        var matRopa = Material("Ropa", new Color(0.25f, 0.32f, 0.45f), 0.2f);

        // ---- Busto ----
        var busto = new GameObject("Busto");

        var cabeza = new GameObject("Cabeza");
        cabeza.transform.SetParent(busto.transform);
        cabeza.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cabeza.transform.localScale = new Vector3(0.85f, 1.05f, 1f);
        // La cara mira a +Z; la zona calva ocupa coronilla y parte alta (hacia atrás).
        var malla = CrearMallaCabeza(new Vector3(0f, 1f, -0.35f).normalized, 0.7f);
        cabeza.AddComponent<MeshFilter>().sharedMesh = malla;
        cabeza.AddComponent<MeshRenderer>().sharedMaterials = new[] { matPiel, matCalva };
        var col = cabeza.AddComponent<SphereCollider>();
        col.radius = 0.5f;

        Decoracion(PrimitiveType.Sphere, "Nariz", cabeza.transform, new Vector3(0f, -0.05f, 0.5f), new Vector3(0.12f, 0.16f, 0.14f), matPiel);
        Decoracion(PrimitiveType.Sphere, "OjoIzq", cabeza.transform, new Vector3(-0.15f, 0.08f, 0.44f), new Vector3(0.08f, 0.07f, 0.06f), matOjo);
        Decoracion(PrimitiveType.Sphere, "OjoDer", cabeza.transform, new Vector3(0.15f, 0.08f, 0.44f), new Vector3(0.08f, 0.07f, 0.06f), matOjo);
        Decoracion(PrimitiveType.Sphere, "OrejaIzq", cabeza.transform, new Vector3(-0.5f, 0f, 0f), new Vector3(0.08f, 0.2f, 0.14f), matPiel);
        Decoracion(PrimitiveType.Sphere, "OrejaDer", cabeza.transform, new Vector3(0.5f, 0f, 0f), new Vector3(0.08f, 0.2f, 0.14f), matPiel);
        Decoracion(PrimitiveType.Cylinder, "Cuello", busto.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.32f, 0.25f, 0.32f), matPiel);
        var hombros = Decoracion(PrimitiveType.Capsule, "Hombros", busto.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.5f, 0.9f, 0.45f), matRopa);
        hombros.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Decoracion(PrimitiveType.Capsule, "Torso", busto.transform, new Vector3(0f, 0.25f, 0f), new Vector3(1.0f, 0.65f, 0.55f), matRopa);
        Decoracion(PrimitiveType.Capsule, "BrazoIzq", busto.transform, new Vector3(-0.78f, 0.2f, 0f), new Vector3(0.26f, 0.55f, 0.26f), matRopa).transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
        Decoracion(PrimitiveType.Capsule, "BrazoDer", busto.transform, new Vector3(0.78f, 0.2f, 0f), new Vector3(0.26f, 0.55f, 0.26f), matRopa).transform.localRotation = Quaternion.Euler(0f, 0f, 6f);

        var foco = new GameObject("PuntoEnfoque");
        foco.transform.SetParent(busto.transform);
        foco.transform.localPosition = new Vector3(0f, 1.15f, 0f);

        // ---- Cámara ----
        var camPrincipal = Camera.main;
        camPrincipal.clearFlags = CameraClearFlags.SolidColor;
        camPrincipal.orthographic = false;
        camPrincipal.backgroundColor = new Color(0.16f, 0.17f, 0.2f);
        camPrincipal.gameObject.AddComponent<CinemachineBrain>();

        var camGO = new GameObject("CamaraOrbital");
        var cm = camGO.AddComponent<CinemachineCamera>();
        cm.Target.TrackingTarget = foco.transform;
        cm.Lens.FieldOfView = 35f;
        cm.Lens.ModeOverride = LensSettings.OverrideModes.Perspective;

        var orbital = camGO.AddComponent<CinemachineOrbitalFollow>();
        orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orbital.Radius = 4.2f;   // plano medio
        orbital.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        orbital.TrackerSettings.PositionDamping = Vector3.zero;

        orbital.HorizontalAxis.Range = new Vector2(-180f, 180f);
        orbital.HorizontalAxis.Wrap = true;
        orbital.HorizontalAxis.Value = 180f;      // empieza de frente
        orbital.HorizontalAxis.Center = 180f;

        orbital.VerticalAxis.Range = new Vector2(-15f, 80f);
        orbital.VerticalAxis.Value = 10f;
        orbital.VerticalAxis.Center = 10f;

        orbital.RadialAxis.Range = new Vector2(0.6f, 1.5f);   // límites de zoom
        orbital.RadialAxis.Value = 1f;
        orbital.RadialAxis.Center = 1f;

        var composer = camGO.AddComponent<CinemachineRotationComposer>();
        composer.Damping = Vector2.zero;

        var control = camGO.AddComponent<ControlOrbitaCabeza>();

        // ---- UI ----
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var botonGO = new GameObject("BotonRestaurarVista", typeof(RectTransform), typeof(Image), typeof(Button));
        botonGO.transform.SetParent(canvasGO.transform, false);
        var rt = botonGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(30f, 30f);
        rt.sizeDelta = new Vector2(260f, 70f);
        var img = botonGO.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        var boton = botonGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(boton.onClick, control.RestaurarVista);

        var textoGO = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        textoGO.transform.SetParent(botonGO.transform, false);
        var trt = textoGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var texto = textoGO.GetComponent<Text>();
        texto.text = "Restaurar vista";
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 28;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(0.15f, 0.15f, 0.15f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.SaveScene(escena, RutaEscena);
        AssetDatabase.SaveAssets();
        Debug.Log("[PrototipoCabeza] Escena creada en " + RutaEscena);
    }

    static Material Material(string nombre, Color color, float suavidad)
    {
        string ruta = $"{Carpeta}/Materiales/{nombre}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", suavidad);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject Decoracion(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// Esfera UV con dos submallas: 0 = piel, 1 = zona calva (triángulos orientados hacia dirCalva).
    static Mesh CrearMallaCabeza(Vector3 dirCalva, float umbral)
    {
        const int lon = 64, lat = 48;
        const float r = 0.5f;
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int y = 0; y <= lat; y++)
        {
            float phi = Mathf.PI * y / lat;
            for (int x = 0; x <= lon; x++)
            {
                float th = 2f * Mathf.PI * x / lon;
                var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                verts.Add(n * r); norms.Add(n); uvs.Add(new Vector2((float)x / lon, 1f - (float)y / lat));
            }
        }
        var piel = new List<int>();
        var calva = new List<int>();
        void Tri(int a, int b, int c)
        {
            Vector3 cruz = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (cruz.sqrMagnitude < 1e-12f) return;
            Vector3 centro = (verts[a] + verts[b] + verts[c]) / 3f;
            if (Vector3.Dot(cruz, centro) < 0f) { int t = b; b = c; c = t; }
            var lista = Vector3.Dot(centro.normalized, dirCalva) > umbral ? calva : piel;
            lista.Add(a); lista.Add(b); lista.Add(c);
        }
        for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int i0 = y * (lon + 1) + x, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                Tri(i0, i1, i2);
                Tri(i1, i3, i2);
            }

        var malla = new Mesh { name = "CabezaCalva" };
        malla.SetVertices(verts);
        malla.SetNormals(norms);
        malla.SetUVs(0, uvs);
        malla.subMeshCount = 2;
        malla.SetTriangles(piel, 0);
        malla.SetTriangles(calva, 1);
        malla.RecalculateBounds();
        malla.RecalculateTangents();

        string ruta = $"{Carpeta}/Mallas/CabezaCalva.asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }

    static void CrearCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder($"{padre}/{nombre}"))
            AssetDatabase.CreateFolder(padre, nombre);
    }
}
