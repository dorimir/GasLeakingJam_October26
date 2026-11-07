using UnityEngine;

/// Pone una imagen de fondo detrás de todo, pegada a la cámara y ajustada a la pantalla
/// (modo "cubrir": rellena sin deformar). Va en la Main Camera. No le afecta la luz.
[RequireComponent(typeof(Camera))]
public class FondoCamara : MonoBehaviour
{
    public Texture2D imagen;
    [Tooltip("Distancia a la cámara (debe quedar detrás del gato y antes del Far Clip)")]
    public float distancia = 20f;
    public Color tinte = Color.white;
    [Tooltip("Material Unlit de URP para el fondo (así el shader se incluye en la build)")]
    public Material materialBase;

    Camera cam;
    Transform plano;
    Material material;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        if (!imagen) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "FondoImagen";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        material = materialBase ? new Material(materialBase) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetTexture("_BaseMap", imagen);
        material.SetColor("_BaseColor", tinte);
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.GetComponent<MeshRenderer>().receiveShadows = false;
        plano = go.transform;
        if (cam.farClipPlane < distancia + 1f) cam.farClipPlane = distancia + 1f;
        Ajustar();
    }

    void OnDisable()
    {
        if (plano) Destroy(plano.gameObject);
        if (material) Destroy(material);
    }

    void LateUpdate() { Ajustar(); }

    void Ajustar()
    {
        if (!plano || !imagen) return;
        float alto = 2f * distancia * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float aspectoPantalla = Screen.height > 0 ? (float)Screen.width / Screen.height : cam.aspect;
        float ancho = alto * aspectoPantalla;
        float aspectoImagen = (float)imagen.width / imagen.height;
        // cubrir: el lado que falte se agranda
        if (ancho / alto > aspectoImagen) alto = ancho / aspectoImagen; else ancho = alto * aspectoImagen;
        plano.localPosition = new Vector3(0f, 0f, distancia);
        plano.localRotation = Quaternion.identity;
        plano.localScale = new Vector3(ancho, alto, 1f);
    }
}
