using UnityEngine;
using UnityEngine.UI;

/// Pixelado de la imagen 3D sin afectar a la UI.
/// La cámara renderiza a una RenderTexture de baja resolución (filtro Point) que se muestra
/// a pantalla completa en un Canvas por DETRÁS de la UI normal.
/// Va en la Main Camera. No asignes una Target Texture a mano: este script la crea.
[RequireComponent(typeof(Camera))]
public class EfectoPixelado : MonoBehaviour
{
    [Tooltip("Resolución vertical de la imagen pixelada (menos = más pixelado)")]
    [Min(16)] public int resolucionVertical = 240;
    public bool activo = true;

    Camera cam;
    RenderTexture rt;
    RawImage imagen;
    GameObject canvasGO, camaraPantallaGO;
    int anchoPantalla, altoPantalla, resolucionUsada;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        CrearSalida();
        Actualizar(true);
    }

    void OnDisable()
    {
        if (cam) cam.targetTexture = null;
        if (canvasGO) Destroy(canvasGO);
        if (camaraPantallaGO) Destroy(camaraPantallaGO);
        LiberarRT();
    }

    void LateUpdate() { Actualizar(false); }

    void CrearSalida()
    {
        // Canvas de fondo (detrás de la UI) con la imagen pixelada a pantalla completa
        canvasGO = new GameObject("CanvasPixelado", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;
        var imgGO = new GameObject("ImagenPixelada", typeof(RectTransform), typeof(RawImage));
        imgGO.transform.SetParent(canvasGO.transform, false);
        var r = (RectTransform)imgGO.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        imagen = imgGO.GetComponent<RawImage>();
        imagen.raycastTarget = false;

        // Cámara mínima que limpia la pantalla (ninguna otra renderiza directamente a ella)
        camaraPantallaGO = new GameObject("CamaraPantalla");
        var cp = camaraPantallaGO.AddComponent<Camera>();
        cp.cullingMask = 0;
        cp.clearFlags = CameraClearFlags.SolidColor;
        cp.backgroundColor = Color.black;
        cp.depth = cam.depth - 10;
    }

    void Actualizar(bool forzar)
    {
        if (!activo)
        {
            if (cam.targetTexture == rt && rt != null) cam.targetTexture = null;
            if (imagen) imagen.enabled = false;
            return;
        }
        if (!forzar && rt != null && Screen.width == anchoPantalla && Screen.height == altoPantalla && resolucionVertical == resolucionUsada)
        {
            if (cam.targetTexture != rt) cam.targetTexture = rt;
            return;
        }

        anchoPantalla = Screen.width;
        altoPantalla = Screen.height;
        resolucionUsada = resolucionVertical;
        int alto = Mathf.Min(resolucionVertical, altoPantalla);
        int ancho = Mathf.Max(1, Mathf.RoundToInt(alto * (float)anchoPantalla / altoPantalla));

        LiberarRT();
        rt = new RenderTexture(ancho, alto, 24) { filterMode = FilterMode.Point, name = "RT_Pixelado" };
        rt.Create();
        cam.targetTexture = rt;
        if (imagen) { imagen.texture = rt; imagen.enabled = true; }
    }

    void LiberarRT()
    {
        if (rt == null) return;
        if (cam && cam.targetTexture == rt) cam.targetTexture = null;
        rt.Release();
        Destroy(rt);
        rt = null;
    }

    /// Rayo desde una posición de pantalla (ratón). Úsalo en vez de ScreenPointToRay,
    /// porque con Target Texture la cámara trabaja en píxeles de la textura.
    public static Ray RayoDesdePantalla(Camera camara, Vector2 posPantalla)
    {
        return camara.ViewportPointToRay(new Vector3(posPantalla.x / Screen.width, posPantalla.y / Screen.height, 0f));
    }
}
