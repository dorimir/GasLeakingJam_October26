using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Herramienta peine: con el modo activo, arrastrar con clic izquierdo sobre la cabeza
/// orienta los pelos de los mechones colocados en la dirección del peinado.
[DefaultExecutionOrder(50)]
public class Peine : MonoBehaviour
{
    public GestorMechones gestor;

    [Header("Peinado")]
    [Tooltip("Radio (metros) alrededor del cursor que afecta a los pelos")]
    public float radioPeine = 0.12f;
    [Range(0f, 1f)] public float fuerza = 0.35f;
    [Tooltip("Movimiento mínimo (metros) para considerar una pasada")]
    public float pasoMinimo = 0.004f;

    [Header("UI")]
    public Image fondoBoton;
    public Text textoBoton;
    public RectTransform cursorPeine;
    public Color colorActivo = new Color(0.95f, 0.8f, 0.3f);
    public Color colorInactivo = Color.white;

    public bool Activo { get; private set; }

    bool peinando, tienePunto;
    Vector3 ultimoPunto;

    void Start() { ActualizarUI(); }

    /// Llamado por el botón "Peine".
    public void Alternar()
    {
        Activo = !Activo;
        if (Activo)
        {
            var g = FindFirstObjectByType<Grafitti>();
            if (g && g.Activo) g.Alternar();   // solo una herramienta a la vez
        }
        if (gestor) gestor.modoPeine = Activo;
        ActualizarUI();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || gestor == null || gestor.camara == null) return;
        Vector2 pos = mouse.position.ReadValue();

        if (cursorPeine)
        {
            cursorPeine.gameObject.SetActive(Activo);
            if (Activo) cursorPeine.position = pos;
        }
        if (!Activo) { peinando = false; return; }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            peinando = !UtilUI.PunteroSobreUIInteractiva(pos);
            tienePunto = false;
        }
        if (!mouse.leftButton.isPressed || !peinando) { peinando = false; return; }

        Ray rayo = EfectoPixelado.RayoDesdePantalla(gestor.camara, pos);
        if (!gestor.colliderCabeza || !gestor.colliderCabeza.Raycast(rayo, out RaycastHit hit, 100f))
        {
            tienePunto = false;
            return;
        }
        if (!tienePunto) { ultimoPunto = hit.point; tienePunto = true; return; }

        Vector3 delta = hit.point - ultimoPunto;
        if (delta.magnitude < pasoMinimo) return;
        foreach (var m in gestor.Mechones)
            if (m.estado == Mechon.Estado.Colocado)
                m.Peinar(hit.point, delta, radioPeine, fuerza);
        ultimoPunto = hit.point;
        gestor.RecalcularCobertura();   // los pelos peinados encima de la calva también cuentan
    }

    void ActualizarUI()
    {
        if (fondoBoton) fondoBoton.color = Activo ? colorActivo : colorInactivo;
        if (textoBoton) textoBoton.text = Activo ? "Peine (activo)" : "Peine";
        if (cursorPeine) cursorPeine.gameObject.SetActive(Activo);
    }
}
