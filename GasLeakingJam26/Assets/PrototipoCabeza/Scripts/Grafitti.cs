using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Herramienta bote de grafitti: con el modo activo, cada clic izquierdo sobre la cabeza
/// pinta una mancha que tapa la calva (cuenta en el %). Hay un número limitado de manchas.
[DefaultExecutionOrder(50)]
public class Grafitti : MonoBehaviour
{
    public GestorMechones gestor;
    [Tooltip("Las manchas se hacen hijas de este objeto (el busto) para viajar a Resultados")]
    public Transform busto;

    [Header("Manchas")]
    [Min(0)] public int manchasMaximas = 5;
    public Material materialMancha;
    [Tooltip("Tamaño (metros) de cada mancha")]
    public float tamanoMancha = 0.2f;
    [Range(0f, 0.5f)] public float variacionTamano = 0.2f;
    [Tooltip("Radio (metros) de calva que tapa cada mancha")]
    public float radioCobertura = 0.08f;
    public float separacionSuperficie = 0.004f;

    [Header("UI")]
    public Image fondoBoton;
    public Text textoContador;
    public RectTransform cursorGrafitti;
    public Color colorActivo = new Color(0.95f, 0.8f, 0.3f);
    public Color colorInactivo = Color.white;

    public bool Activo { get; private set; }
    public int ManchasRestantes => Mathf.Max(0, manchasMaximas - usadas);

    int usadas;

    void Start()
    {
        if (!busto && gestor && gestor.colliderCabeza) busto = gestor.colliderCabeza.transform.root;
        ActualizarUI();
    }

    /// Llamado por el botón del bote de grafitti.
    public void Alternar()
    {
        Activo = !Activo;
        if (Activo)
        {
            var p = FindFirstObjectByType<Peine>();
            if (p && p.Activo) p.Alternar();   // solo una herramienta a la vez
        }
        if (gestor) gestor.modoPeine = Activo;   // bloquea coger mechones mientras pintas
        ActualizarUI();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || gestor == null || gestor.camara == null) return;
        Vector2 pos = mouse.position.ReadValue();
        if (cursorGrafitti && Activo) cursorGrafitti.position = pos;
        if (!Activo || !mouse.leftButton.wasPressedThisFrame) return;
        if (ManchasRestantes <= 0 || UtilUI.PunteroSobreUIInteractiva(pos)) return;

        Ray rayo = EfectoPixelado.RayoDesdePantalla(gestor.camara, pos);
        if (!gestor.colliderCabeza || !gestor.colliderCabeza.Raycast(rayo, out RaycastHit hit, 100f)) return;
        Pintar(hit.point, hit.normal);
    }

    void Pintar(Vector3 punto, Vector3 normal)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Mancha_" + (usadas + 1);
        Destroy(go.GetComponent<Collider>());
        if (materialMancha) go.GetComponent<MeshRenderer>().sharedMaterial = materialMancha;
        go.transform.SetParent(busto, true);
        // El Quad se ve por su cara -Z: la orientamos hacia fuera de la cabeza, con giro aleatorio
        Quaternion rot = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * Quaternion.LookRotation(-normal);
        go.transform.SetPositionAndRotation(punto + normal * separacionSuperficie, rot);
        float k = 1f + Random.Range(-variacionTamano, variacionTamano);
        go.transform.localScale = Vector3.one * tamanoMancha * k;

        usadas++;
        gestor.RegistrarMancha(punto, radioCobertura * k);
        ActualizarUI();
    }

    void ActualizarUI()
    {
        if (fondoBoton) fondoBoton.color = Activo ? colorActivo : colorInactivo;
        if (textoContador) textoContador.text = $"x {ManchasRestantes}";
        if (cursorGrafitti) cursorGrafitti.gameObject.SetActive(Activo);
    }
}
