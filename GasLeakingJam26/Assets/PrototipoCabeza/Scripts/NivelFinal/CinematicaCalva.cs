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
    public float anguloHorizontalInicial = 0f;

    [Header("Final (zoom en la calva)")]
    public float distanciaFinal = 1.3f;
    public float anguloVerticalFinal = 62f;
    [Tooltip("Segundos que tarda en llegar al zoom")]
    public float duracionZoom = 6f;
    public float retrasoInicial = 0.8f;

    [Tooltip("Grados por segundo que gira el gato (frena al llegar al zoom)")]
    public float velocidadGiro = 40f;
    [Range(0f, 1f)] public float giroAlFinal = 0.25f;
    [Tooltip("El gato empieza de espaldas a la cámara (se ve la calva desde detrás)")]
    public bool empezarDeEspaldas = true;
    [Tooltip("No gira hasta terminar el zoom a la calva")]
    public bool girarDespuesDelZoom = true;

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
            if (empezarDeEspaldas)
            {
                // gira el gato para que su nuca mire a la cámara
                Vector3 camPos = pivote + Quaternion.Euler(0f, anguloHorizontalInicial, 0f) * new Vector3(0f, 0f, -1f);
                Vector3 cara = Vector3.ProjectOnPlane(gato.forward, Vector3.up);
                Vector3 lejosDeCamara = Vector3.ProjectOnPlane(pivote - camPos, Vector3.up);
                float a = Vector3.SignedAngle(cara, lejosDeCamara, Vector3.up);
                gato.RotateAround(pivote, Vector3.up, a);
            }
        }
        Aplicar(0f);
    }

    void LateUpdate()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01((t - retrasoInicial) / Mathf.Max(0.01f, duracionZoom));
        k = k * k * (3f - 2f * k);   // suave al empezar y al llegar
        if (gato)
        {
            // primero el zoom a la calva sin girar; el giro arranca al terminar el zoom
            float giro = girarDespuesDelZoom ? Mathf.Clamp01((t - retrasoInicial - duracionZoom) / 1.5f) * giroAlFinal
                                             : Mathf.Lerp(1f, giroAlFinal, k);
            gato.RotateAround(pivote, Vector3.up, velocidadGiro * giro * Time.deltaTime);
        }
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
