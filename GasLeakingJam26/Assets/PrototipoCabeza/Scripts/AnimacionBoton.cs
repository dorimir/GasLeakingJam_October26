using UnityEngine;
using UnityEngine.EventSystems;

/// Animación de botones:
/// - al enfocar (ratón encima o seleccionado con teclado/mando) crece suavemente;
/// - al pulsar se encoge un poco;
/// - opcional: "latido" de escala para llamar la atención (se para al pulsarlo).
/// Si el botón tiene un hijo llamado "Sprite" se anima ese hijo (así el tooltip/contador no se deforman).
[DisallowMultipleComponent]
public class AnimacionBoton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("Lo que se escala. Vacío = el hijo 'Sprite' si existe, si no el propio botón.")]
    public Transform objetivo;
    public float escalaEnfoque = 1.1f;
    public float escalaPulsado = 0.94f;
    [Tooltip("Rapidez de la transición")]
    public float velocidad = 14f;

    [Header("Llamar la atención (latido)")]
    public bool latido = false;
    [Tooltip("Cuánto crece en cada latido (0.1 = 10%)")]
    public float amplitudLatido = 0.1f;
    [Tooltip("Latidos por segundo")]
    public float frecuenciaLatido = 1.2f;
    public bool pararLatidoAlPulsar = true;

    Vector3 escalaBase;
    bool encima, pulsado, seleccionado, iniciado;
    float actual = 1f;

    void Awake()
    {
        if (!objetivo)
        {
            var hijo = transform.Find("Sprite");
            objetivo = hijo ? hijo : transform;
        }
        escalaBase = objetivo.localScale;
        iniciado = true;
    }

    void OnDisable()
    {
        if (iniciado && objetivo) objetivo.localScale = escalaBase;
        encima = pulsado = seleccionado = false;
        actual = 1f;
    }

    void Update()
    {
        float meta = pulsado ? escalaPulsado : (encima || seleccionado ? escalaEnfoque : 1f);
        actual = Mathf.Lerp(actual, meta, 1f - Mathf.Exp(-velocidad * Time.unscaledDeltaTime));
        float l = 1f;
        if (latido && !encima && !pulsado)
        {
            // pulso: sube r\u00e1pido y baja suave, como un latido
            float fase = Mathf.Repeat(Time.unscaledTime * frecuenciaLatido, 1f);
            l = 1f + amplitudLatido * Mathf.Pow(Mathf.Sin(fase * Mathf.PI), 3f);
        }
        objetivo.localScale = escalaBase * (actual * l);
    }

    public void OnPointerEnter(PointerEventData e) { encima = true; }
    public void OnPointerExit(PointerEventData e) { encima = false; pulsado = false; }
    public void OnPointerDown(PointerEventData e)
    {
        pulsado = true;
        if (pararLatidoAlPulsar) latido = false;
    }
    public void OnPointerUp(PointerEventData e) { pulsado = false; }
    public void OnSelect(BaseEventData e) { seleccionado = true; }
    public void OnDeselect(BaseEventData e) { seleccionado = false; }
}
