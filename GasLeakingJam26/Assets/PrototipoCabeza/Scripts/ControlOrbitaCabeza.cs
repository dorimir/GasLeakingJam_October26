using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// Control de vista (va en la Main Camera). Con clic derecho:
/// - girarObjetivo = true  -> la cámara queda FIJA y gira el gato (el busto).
/// - girarObjetivo = false -> la cámara orbita alrededor del objetivo (modo antiguo).
/// RestaurarVista() deja la nuca mirando a cámara.
[DefaultExecutionOrder(-100)]
public class ControlOrbitaCabeza : MonoBehaviour
{
    [Tooltip("Punto al que mira la cámara (centro de la cabeza)")]
    public Transform objetivo;

    [Header("Girar el gato")]
    public bool girarObjetivo = true;
    [Tooltip("Lo que gira. Si está vacío, la raíz del objetivo (el Busto).")]
    public Transform objetoGirar;
    [Tooltip("Cuánto puede inclinarse el gato hacia la cámara para enseñar la calva (grados). Arrastra hacia abajo.")]
    public float inclinacionHaciaCamara = 80f;
    [Tooltip("Cuánto puede inclinarse hacia atrás (grados). Arrastra hacia arriba.")]
    public float inclinacionHaciaAtras = 30f;

    [Header("Rotación (clic derecho)")]
    public float sensibilidadHorizontal = 0.3f;   // grados por píxel
    public float sensibilidadVertical = 0.25f;
    public bool invertirHorizontal = false;
    public bool invertirVertical = false;
    public float anguloVerticalMin = -15f;
    public float anguloVerticalMax = 80f;

    [Header("Zoom (rueda)")]
    public float distancia = 4.2f;
    public float distanciaMin = 2.5f;
    public float distanciaMax = 6.3f;
    public float sensibilidadZoom = 0.004f;
    [Tooltip("Si está desactivado, el jugador no puede hacer zoom con la rueda")]
    public bool permitirZoom = false;

    [Header("Vista inicial (posición de la cámara)")]
    public float anguloHorizontalInicial = 180f;   // 180 = de frente, 0 = nuca
    public float anguloVerticalInicial = 10f;

    [Header("Vista restaurada (nuca)")]
    public float anguloHorizontalNuca = 0f;
    public float anguloVerticalNuca = 25f;
    public float distanciaNuca = 4.2f;
    public float duracionRestaurar = 0.6f;

    // cámara
    float yaw, pitch;
    // gato
    float yawGato, pitchGato;
    Vector3 pivote, posInicial, ejeDerecha;
    Quaternion rotInicial;
    Transform girado;
    Coroutine animacion;

    void Start()
    {
        yaw = anguloHorizontalInicial;
        pitch = anguloVerticalInicial;
        AplicarCamara();
        if (girarObjetivo && objetivo)
        {
            girado = objetoGirar ? objetoGirar : objetivo.root;
            pivote = objetivo.position;
            posInicial = girado.position;
            rotInicial = girado.rotation;
            ejeDerecha = transform.right;
        }
    }

    void LateUpdate()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                if (d.sqrMagnitude > 0f)
                {
                    DetenerAnimacion();
                    float dx = (invertirHorizontal ? -d.x : d.x) * sensibilidadHorizontal;
                    float dy = (invertirVertical ? -d.y : d.y) * sensibilidadVertical;
                    if (girado)
                    {
                        yawGato = Mathf.Repeat(yawGato - dx + 180f, 360f) - 180f;
                        // negativo = la coronilla se acerca a la cámara (ves la calva desde arriba)
                        pitchGato = Mathf.Clamp(pitchGato + dy, -inclinacionHaciaCamara, inclinacionHaciaAtras);
                    }
                    else
                    {
                        yaw = Mathf.Repeat(yaw + dx + 180f, 360f) - 180f;
                        pitch = Mathf.Clamp(pitch - dy, anguloVerticalMin, anguloVerticalMax);
                    }
                }
            }
            float scroll = permitirZoom ? mouse.scroll.ReadValue().y : 0f;
            if (scroll != 0f)
            {
                DetenerAnimacion();
                distancia = Mathf.Clamp(distancia - scroll * sensibilidadZoom, distanciaMin, distanciaMax);
            }
        }
        AplicarCamara();
        AplicarGato();
    }

    void AplicarCamara()
    {
        if (objetivo == null) return;
        Vector3 centro = girado ? pivote : objetivo.position;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(centro + rot * new Vector3(0f, 0f, -distancia), rot);
    }

    void AplicarGato()
    {
        if (!girado) return;
        Quaternion r = Quaternion.AngleAxis(pitchGato, ejeDerecha) * Quaternion.AngleAxis(yawGato, Vector3.up);
        girado.SetPositionAndRotation(pivote + r * (posInicial - pivote), r * rotInicial);
    }

    /// Llamado por el botón "Restaurar vista": deja la nuca mirando a cámara.
    public void RestaurarVista()
    {
        DetenerAnimacion();
        if (girado)
        {
            Vector3 atras = Vector3.ProjectOnPlane(-(rotInicial * Vector3.forward), Vector3.up);
            Vector3 haciaCamara = Vector3.ProjectOnPlane(transform.position - pivote, Vector3.up);
            float yawObjetivo = Vector3.SignedAngle(atras, haciaCamara, Vector3.up);
            float pitchObjetivo = Mathf.Clamp(-(anguloVerticalNuca - anguloVerticalInicial), -inclinacionHaciaCamara, inclinacionHaciaAtras);
            animacion = StartCoroutine(AnimarGato(yawObjetivo, pitchObjetivo));
        }
        else
            animacion = StartCoroutine(AnimarCamara(anguloHorizontalNuca, anguloVerticalNuca, permitirZoom ? distanciaNuca : distancia));
    }

    IEnumerator AnimarGato(float yawFin, float pitchFin)
    {
        float y0 = yawGato, p0 = pitchGato, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionRestaurar);
            float k = Mathf.SmoothStep(0f, 1f, t);
            yawGato = Mathf.LerpAngle(y0, yawFin, k);
            pitchGato = Mathf.Lerp(p0, pitchFin, k);
            yield return null;
        }
        animacion = null;
    }

    IEnumerator AnimarCamara(float yawFin, float pitchFin, float distFin)
    {
        float yawIni = yaw, pitchIni = pitch, distIni = distancia;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionRestaurar);
            float k = Mathf.SmoothStep(0f, 1f, t);
            yaw = Mathf.LerpAngle(yawIni, yawFin, k);
            pitch = Mathf.Lerp(pitchIni, pitchFin, k);
            distancia = Mathf.Lerp(distIni, distFin, k);
            yield return null;
        }
        animacion = null;
    }

    void DetenerAnimacion()
    {
        if (animacion != null) { StopCoroutine(animacion); animacion = null; }
    }
}
