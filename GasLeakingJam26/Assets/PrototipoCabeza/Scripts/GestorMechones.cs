using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Gestiona los mechones: se sacan arrastrando desde el contador (panel de UI), se pegan en la
/// cabeza con clic izquierdo y se pueden recolocar. Si se sueltan fuera de la cabeza vuelven al
/// contador. Calcula el % de calvicie.
/// Los mechones se crean al sacarlos (no hay N mechones en la escena desde el principio).
[DefaultExecutionOrder(100)]
public class GestorMechones : MonoBehaviour
{
    [Header("Mechones")]
    [Min(0)] public int numeroMechones = 8;
    public Mechon prefabMechon;
    [Tooltip("Radio (en metros) de calva que tapa cada mechón")]
    public float radioCobertura = 0.13f;
    public float separacionSuperficie = 0.01f;

    [Header("Cabeza")]
    public Collider colliderCabeza;
    [Tooltip("Malla de la cabeza. Si cambias el modelo, activa Read/Write en su importación.")]
    public MeshFilter mallaCabeza;
    [Tooltip("Índice de la submalla (material) que es la zona calva")]
    public int submallaCalva = 1;

    [Header("Arrastre")]
    public Camera camara;
    [Tooltip("Distancia a la cámara del mechón mientras lo arrastras fuera de la cabeza")]
    public float distanciaArrastre = 2.5f;

    [Header("UI")]
    public Image rellenoBarra;
    public Text textoCalvicie;
    public Text textoMechones;
    [Tooltip("Imagen del contador: se atenúa cuando no quedan mechones")]
    public Graphic iconoContador;
    public Color colorCubierto = new Color(0.3f, 0.8f, 0.35f);
    public Color colorCalvo = new Color(0.9f, 0.3f, 0.25f);

    public float PorcentajeCalvicie { get; private set; } = 100f;
    public int MechonesDisponibles { get; private set; }
    /// Mechones sacados (colocados en la cabeza o arrastrándose).
    public IReadOnlyList<Mechon> Mechones => mechones;
    /// Con el peine activo no se cogen mechones con el clic izquierdo.
    [HideInInspector] public bool modoPeine;

    readonly List<Mechon> mechones = new List<Mechon>();
    Vector3[] muestrasLocales = new Vector3[0];
    Mechon arrastrado;
    Color colorIcono = Color.white;

    void Start()
    {
        if (!camara) camara = Camera.main;
        if (iconoContador) colorIcono = iconoContador.color;
        PrepararMuestras();
        Recalcular();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || camara == null) return;
        Ray rayo = EfectoPixelado.RayoDesdePantalla(camara, mouse.position.ReadValue());

        // Recolocar un mechón ya pegado
        if (arrastrado == null && !modoPeine && mouse.leftButton.wasPressedThisFrame && !SobreUI())
        {
            var m = BuscarMechon(rayo);
            if (m) EmpezarArrastre(m);
        }

        if (arrastrado != null)
        {
            MoverArrastrado(rayo);
            if (!mouse.leftButton.isPressed) Soltar();
            Recalcular();
        }
    }

    /// Lo llama el contador (panel de UI) al pulsar sobre él: crea un mechón y empieza a arrastrarlo.
    public bool EmpezarArrastreDesdeUI()
    {
        if (arrastrado != null || modoPeine || prefabMechon == null) return false;
        if (numeroMechones - mechones.Count <= 0) return false;

        var m = Instantiate(prefabMechon, transform);
        m.name = "Mechon_" + (mechones.Count + 1);
        mechones.Add(m);
        EmpezarArrastre(m);
        var mouse = Mouse.current;
        if (mouse != null) MoverArrastrado(EfectoPixelado.RayoDesdePantalla(camara, mouse.position.ReadValue()));
        Recalcular();
        return true;
    }

    // ---------- Arrastre ----------

    void EmpezarArrastre(Mechon m)
    {
        arrastrado = m;
        m.transform.localScale = m.escalaBase;
        m.estado = Mechon.Estado.Arrastrando;
    }

    void Soltar()
    {
        var m = arrastrado;
        arrastrado = null;
        if (m.sobreCabeza)
        {
            m.estado = Mechon.Estado.Colocado;
        }
        else
        {
            // Fuera de la cabeza: vuelve al contador
            mechones.Remove(m);
            Destroy(m.gameObject);
        }
    }

    Mechon BuscarMechon(Ray rayo)
    {
        var hits = Physics.RaycastAll(rayo, 100f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var m = h.collider.GetComponentInParent<Mechon>();
            if (m && mechones.Contains(m)) return m;
        }
        return null;
    }

    void MoverArrastrado(Ray rayo)
    {
        if (colliderCabeza && colliderCabeza.Raycast(rayo, out RaycastHit hit, 100f))
        {
            arrastrado.sobreCabeza = true;
            arrastrado.puntoSuperficie = hit.point;
            arrastrado.transform.SetPositionAndRotation(
                hit.point + hit.normal * separacionSuperficie,
                RotacionSobreSuperficie(hit.normal));
        }
        else
        {
            arrastrado.sobreCabeza = false;
            arrastrado.transform.SetPositionAndRotation(rayo.GetPoint(distanciaArrastre), RotacionHaciaCamara());
        }
    }

    Quaternion RotacionSobreSuperficie(Vector3 normal)
    {
        Vector3 adelante = Vector3.ProjectOnPlane(camara.transform.up, normal);
        if (adelante.sqrMagnitude < 1e-4f) adelante = Vector3.ProjectOnPlane(camara.transform.forward, normal);
        return Quaternion.LookRotation(adelante.normalized, normal);
    }

    Quaternion RotacionHaciaCamara()
    {
        return Quaternion.LookRotation(camara.transform.up, -camara.transform.forward);
    }

    bool SobreUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // ---------- Calvicie ----------

    void PrepararMuestras()
    {
        var puntos = new HashSet<Vector3>();
        var malla = mallaCabeza ? mallaCabeza.sharedMesh : null;
        if (malla != null && submallaCalva < malla.subMeshCount)
        {
            var v = malla.vertices;
            var t = malla.GetTriangles(submallaCalva);
            for (int i = 0; i < t.Length; i += 3)
            {
                puntos.Add(v[t[i]]); puntos.Add(v[t[i + 1]]); puntos.Add(v[t[i + 2]]);
                puntos.Add((v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f);
            }
        }
        muestrasLocales = new Vector3[puntos.Count];
        puntos.CopyTo(muestrasLocales);
        if (muestrasLocales.Length == 0)
            Debug.LogWarning("[GestorMechones] No se encontró la zona calva (revisa mallaCabeza y submallaCalva).");
    }

    void Recalcular()
    {
        int cubiertas = 0;
        float r2 = radioCobertura * radioCobertura;
        Transform tc = mallaCabeza ? mallaCabeza.transform : null;
        if (tc != null)
        {
            foreach (var s in muestrasLocales)
            {
                Vector3 w = tc.TransformPoint(s);
                foreach (var m in mechones)
                {
                    bool cuenta = m.estado == Mechon.Estado.Colocado ||
                                  (m.estado == Mechon.Estado.Arrastrando && m.sobreCabeza);
                    if (cuenta && (w - m.puntoSuperficie).sqrMagnitude < r2) { cubiertas++; break; }
                }
            }
        }
        int total = muestrasLocales.Length;
        PorcentajeCalvicie = total > 0 ? 100f * (1f - cubiertas / (float)total) : 0f;
        MechonesDisponibles = Mathf.Max(0, numeroMechones - mechones.Count);
        ActualizarUI();
    }

    void ActualizarUI()
    {
        float f = PorcentajeCalvicie / 100f;
        if (rellenoBarra)
        {
            rellenoBarra.fillAmount = f;
            rellenoBarra.color = Color.Lerp(colorCubierto, colorCalvo, f);
        }
        if (textoCalvicie) textoCalvicie.text = $"Calvicie: {Mathf.RoundToInt(PorcentajeCalvicie)}%";
        if (textoMechones) textoMechones.text = $"x {MechonesDisponibles}";
        if (iconoContador)
        {
            var c = colorIcono;
            if (MechonesDisponibles == 0) c.a *= 0.3f;
            iconoContador.color = c;
        }
    }
}
