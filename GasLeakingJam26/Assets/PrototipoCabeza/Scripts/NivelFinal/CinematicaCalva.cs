using UnityEngine;

/// Cinemática del nivel final: el GATO gira sobre sí mismo y la cámara (sin orbitar) se va
/// acercando y subiendo hasta quedar enfocando la calva (coronilla). Va en la Main Camera.
public class CinematicaCalva : MonoBehaviour
{
    [Tooltip("Si está vacío busca 'CentroCabeza' (del busto que viene del nivel)")]
    public Transform centroCabeza;
    public string nombreCentro = "CentroCabeza";
    [Tooltip("Desplazamiento desde el centro de la cabeza hasta la calva")]
    public Vector3 desplazamientoCalva = new Vector3(0f, 0.45f, -0.1f);

    [Header("Inicio (plano medio)")]
    public float distanciaInicial = 4.2f;
    public float anguloVerticalInicial = 12f;
    [Tooltip("Lado desde el que mira la cámara: 180 = de frente")]
    public float anguloHorizontalInicial = 180f;

    [Header("Final (zoom en la calva)")]
    public float distanciaFinal = 1.3f;
    public float anguloVerticalFinal = 62f;
    [Tooltip("Segundos que tarda en llegar al zoom")]
    public float duracionZoom = 6f;
    public float retrasoInicial = 0.8f;

    [Tooltip("Grados por segundo que gira el gato (frena al llegar al zoom)")]
    public float velocidadGiro = 40f;
    [Range(0f, 1f)] public float giroAlFinal = 0.25f;

    Transform gato;
    Vector3 pivote;
    float t;

    void Start()
    {
        if (!centroCabeza)
        {
            var go = GameObject.Find(nombreCentro);
            if (go) centroCabeza = go.transform;
        }
        pivote = centroCabeza ? centroCabeza.position : new Vector3(0f, 1.45f, 0f);
        if (centroCabeza)
        {
            gato = centroCabeza.root;
            Quaternion enderezar = Quaternion.FromToRotation(gato.up, Vector3.up);   // por si llega inclinado
            gato.SetPositionAndRotation(pivote + enderezar * (gato.position - pivote), enderezar * gato.rotation);
        }
        Aplicar(0f);
    }

    void LateUpdate()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01((t - retrasoInicial) / Mathf.Max(0.01f, duracionZoom));
        k = k * k * (3f - 2f * k);   // suave al empezar y al llegar
        if (gato) gato.RotateAround(pivote, Vector3.up, velocidadGiro * Mathf.Lerp(1f, giroAlFinal, k) * Time.deltaTime);
        Aplicar(k);
    }

    void Aplicar(float k)
    {
        Vector3 calva = pivote + Vector3.up * desplazamientoCalva.y;   // la coronilla está en el eje de giro
        Vector3 objetivo = Vector3.Lerp(pivote, calva, k);
        float dist = Mathf.Lerp(distanciaInicial, distanciaFinal, k);
        float pitch = Mathf.Lerp(anguloVerticalInicial, anguloVerticalFinal, k);
        Quaternion rot = Quaternion.Euler(pitch, anguloHorizontalInicial, 0f);
        transform.SetPositionAndRotation(objetivo + rot * new Vector3(0f, 0f, -dist), rot);
    }
}
