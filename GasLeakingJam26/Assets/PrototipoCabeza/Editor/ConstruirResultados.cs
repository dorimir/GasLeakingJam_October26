using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// Menú: GasLeaking > Crear escena de resultados
/// - Crea Assets/Scenes/Resultados.unity (cámara 360°, luz, UI y botón "Volver a probar").
/// - Añade el botón "Ver resultado" al prototipo.
/// - Añade ambas escenas a Build Settings.
public static class ConstruirResultados
{
    const string RutaPrototipo = "Assets/Scenes/PrototipoCabeza.unity";
    const string RutaResultados = "Assets/Scenes/Resultados.unity";

    [MenuItem("GasLeaking/Crear escena de resultados")]
    public static void Construir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Datos que copiamos del prototipo (fondo de cámara, pixelado)
        var proto = EditorSceneManager.OpenScene(RutaPrototipo, OpenSceneMode.Single);
        var camProto = Camera.main;
        Color fondo = camProto ? camProto.backgroundColor : new Color(0.16f, 0.17f, 0.2f);
        var pixProto = camProto ? camProto.GetComponent<EfectoPixelado>() : null;
        int resolucion = pixProto ? pixProto.resolucionVertical : 240;
        // El prototipo ya está conectado (botón "Listo!" -> FlujoPartida.IrAResultados); no se toca.
        EditorSceneManager.SaveScene(proto);

        CrearEscenaResultados(fondo, resolucion);

        AnadirABuild(RutaPrototipo);
        AnadirABuild(RutaResultados);

        EditorSceneManager.OpenScene(RutaPrototipo, OpenSceneMode.Single);
        Debug.Log("[Resultados] Escena de resultados creada y botón 'Ver resultado' añadido.");
    }

    static void AnadirBotonPrototipo()
    {
        var canvas = GameObject.Find("Canvas");
        var gestor = Object.FindFirstObjectByType<GestorMechones>();
        var busto = GameObject.Find("Busto");
        if (!canvas || !gestor || !busto) { Debug.LogError("[Resultados] Falta Canvas, SistemaMechones o Busto en el prototipo."); return; }

        var flujo = gestor.GetComponent<FlujoPartida>();
        if (!flujo) flujo = gestor.gameObject.AddComponent<FlujoPartida>();
        flujo.gestor = gestor;
        flujo.busto = busto.transform;

        var viejo = canvas.transform.Find("BotonVerResultado");
        if (viejo) Object.DestroyImmediate(viejo.gameObject);
        var boton = CrearBoton(canvas.transform, "BotonVerResultado", "Ver resultado",
            new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(260f, 70f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(boton.onClick, flujo.IrAResultados);
        EditorUtility.SetDirty(gestor.gameObject);
        EditorSceneManager.MarkSceneDirty(gestor.gameObject.scene);
    }

    static void CrearEscenaResultados(Color fondo, int resolucion)
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Iluminación básica
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.36f, 0.4f);
        var luzGO = new GameObject("Luz Principal");
        var luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.1f;
        luz.shadows = LightShadows.Soft;
        luzGO.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        RenderSettings.sun = luz;

        // Cámara 360°
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fondo;
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<Giro360>();
        var pix = camGO.AddComponent<EfectoPixelado>();
        pix.resolucionVertical = resolucion;
        int indice3D = IndiceRenderer3D();
        if (indice3D >= 0) cam.GetUniversalAdditionalCameraData().SetRenderer(indice3D);

        // UI
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Columna de textos a la izquierda, para no tapar la cabeza
        var izq = new Vector2(0f, 0.5f);
        Texto(canvasGO.transform, "Titulo", fuente, 44, "RESULTADO", izq, new Vector2(90f, 170f), new Vector2(700f, 70f));
        var veredicto = Texto(canvasGO.transform, "TextoVeredicto", fuente, 54, "", izq, new Vector2(90f, 85f), new Vector2(700f, 90f));
        var valor = Texto(canvasGO.transform, "TextoValoracion", fuente, 34, "", izq, new Vector2(90f, 0f), new Vector2(700f, 60f));
        var mech = Texto(canvasGO.transform, "TextoMechones", fuente, 28, "", izq, new Vector2(90f, -65f), new Vector2(700f, 50f));

        var pantalla = canvasGO.AddComponent<PantallaResultados>();
        pantalla.textoValoracion = valor;
        pantalla.textoMechones = mech;

        // ---- Barra gacha (abajo, encima del botón) ----
        var panel = new GameObject("PanelBarra", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 140f);
        prt.sizeDelta = new Vector2(1240f, 260f);
        var pimg = panel.GetComponent<Image>();
        pimg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        pimg.raycastTarget = false;

        var porcBarra = Texto(panel.transform, "TextoPorcentaje", fuente, 46, "0%", new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(400f, 60f));
        porcBarra.alignment = TextAnchor.MiddleCenter;
        porcBarra.rectTransform.pivot = new Vector2(0.5f, 1f);

        var barra = new GameObject("Barra", typeof(RectTransform), typeof(Image));
        barra.transform.SetParent(panel.transform, false);
        var brt = (RectTransform)barra.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, 0f);
        brt.sizeDelta = new Vector2(1150f, 40f);
        var bimg = barra.GetComponent<Image>();
        bimg.color = new Color(0.05f, 0.97f, 0.33f);   // verde: no se dan cuenta
        bimg.raycastTarget = false;

        var rojo = new GameObject("ZonaRoja", typeof(RectTransform), typeof(Image));
        rojo.transform.SetParent(barra.transform, false);
        var rrt = (RectTransform)rojo.transform;
        rrt.anchorMin = new Vector2(0.75f, 0f); rrt.anchorMax = new Vector2(0.86f, 1f);
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        var rimg = rojo.GetComponent<Image>();
        rimg.color = new Color(0.9f, 0.3f, 0.25f);    // rojo: se dan cuenta
        rimg.raycastTarget = false;

        var marca = new GameObject("Marcador", typeof(RectTransform), typeof(Image));
        marca.transform.SetParent(barra.transform, false);
        var mrt = (RectTransform)marca.transform;
        mrt.anchorMin = new Vector2(0f, 0.5f); mrt.anchorMax = new Vector2(0f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.sizeDelta = new Vector2(18f, 90f);
        var mimg = marca.GetComponent<Image>();
        mimg.color = new Color(0.85f, 0.85f, 0.85f);
        mimg.raycastTarget = false;

        var lPelo = Texto(panel.transform, "EtiquetaPelo", fuente, 36, "pelo", new Vector2(0f, 0f), new Vector2(45f, 15f), new Vector2(300f, 55f));
        lPelo.fontStyle = FontStyle.Normal;
        var lCalvo = Texto(panel.transform, "EtiquetaCalvo", fuente, 36, "calvo", new Vector2(1f, 0f), new Vector2(-45f, 15f), new Vector2(300f, 55f));
        lCalvo.fontStyle = FontStyle.Normal;
        lCalvo.alignment = TextAnchor.MiddleRight;

        var gacha = panel.AddComponent<BarraGacha>();
        gacha.zonaRoja = rrt;
        gacha.marcador = mrt;
        gacha.textoPorcentaje = porcBarra;
        gacha.textoVeredicto = veredicto;

        var boton = CrearBoton(canvasGO.transform, "BotonVolverAProbar", "Volver a probar",
            new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(340f, 80f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(boton.onClick, pantalla.VolverAProbar);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.SaveScene(escena, RutaResultados);
    }

    static int IndiceRenderer3D()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (!rp) return -1;
        var lista = new SerializedObject(rp).FindProperty("m_RendererDataList");
        for (int i = 0; i < lista.arraySize; i++)
            if (lista.GetArrayElementAtIndex(i).objectReferenceValue is UniversalRendererData) return i;
        return -1;
    }

    static Text Texto(Transform padre, string nombre, Font fuente, int tam, string contenido, Vector2 ancla, Vector2 pos, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;
        var t = go.GetComponent<Text>();
        t.font = fuente; t.fontSize = tam; t.text = contenido;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleLeft;
        t.color = Color.white;
        t.raycastTarget = false;
        go.GetComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
        return t;
    }

    static Button CrearBoton(Transform padre, string nombre, string texto, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;

        var tGO = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        tGO.transform.SetParent(go.transform, false);
        var trt = (RectTransform)tGO.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = tGO.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28; t.text = texto;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.15f, 0.15f, 0.15f);
        return go.GetComponent<Button>();
    }

    static void AnadirABuild(string ruta)
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in lista) if (s.path == ruta) { s.enabled = true; EditorBuildSettings.scenes = lista.ToArray(); return; }
        lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
