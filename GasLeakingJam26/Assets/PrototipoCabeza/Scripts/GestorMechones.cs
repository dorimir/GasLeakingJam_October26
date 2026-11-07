using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Gestiona los mechones: bandeja 3D en un lateral de la pantalla, panel lateral de UI desde el que
/// también se arrastran, arrastre con clic izquierdo hasta la cabeza y cálculo del % de calvicie.
[DefaultExecutionOrder(100)]   // después de la cámara, para que la bandeja no tiemble al orbitar
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

    [Header("Bandeja 3D (lateral de la pantalla)")]
    public Camera camara;
    public bool mostrarBandeja3D = true;
    [Tooltip("true = derecha, false = izquierda")]
    public bool bandejaDerecha = true;
    public float distanciaBandeja = 2.5f;
    [Range(0f, 0.5f)] public float margenBandeja = 0.1f;
    public float velocidadBandeja = 12f;
    [Tooltip("Tamaño de los mechones mientras esperan en la bandeja (1 = tamaño real)")]
    public float escalaEnBandeja = 0.5f;

    [Header("UI")]
    public Image rellenoBarra;
    public Text textoCalvicie;
    public Text textoMechones;
    public Color colorCubierto = new Color(0.3f, 0.8f, 0.35f);
    public Color colorCalvo = new Color(0.9f, 0.3f, 0.25f);

    public float PorcentajeCalvicie { get; private set; } = 100f;
    public int MechonesDisponibles { get; private set; }
    public IReadOnlyList<Mechon> Mechones => mechones;
    /// Con el peine activo no se cogen mechones con el clic izquierdo.
    [HideInInspector] public bool modoPeine;

    readonly List<Mechon> mechones = new List<Mechon>();
    Vector3[] muestrasLocales = new Vector3[0];
    Mechon arrastrado;
    bool primeraBandeja = true;

    void Start()
    {
        if (!camara) camara = Camera.main;
        PrepararMuestras();
        for (int i = 0; i < numeroMechones; i++)
        {
            var m = Instantiate(prefabMechon, transform);
            m.name = "Mechon_" + (i + 1);
            m.indiceBandeja = i;
            mechones.Add(m);
        }
        Recalcular();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || camara == null) return;
        Ray rayo = EfectoPixelado.RayoDesdePantalla(camara, mouse.position.ReadValue());

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

    void LateUpdate()
    {
        ColocarBandeja(primeraBandeja);
        primeraBandeja = false;
    }

    /// Lo llama el panel lateral de UI al pulsar sobre él: coge un mechón libre y empieza a arrastrarlo.
    public bool EmpezarArrastreDesdeUI()
    {
        if (arrastrado != null || modoPeine) return false;
        foreach (var m in mechones)
        {
            if (m.estado != Mechon.Estado.EnBandeja) continue;
            m.gameObject.SetActive(true);
            EmpezarArrastre(m);
            var mouse = Mouse.current;
            if (mouse != null) MoverArrastrado(EfectoPixelado.RayoDesdePantalla(camara, mouse.position.ReadValue()));
            Recalcular();
            return true;
        }
        return false;
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
        arrastrado.estado = arrastrado.sobreCabeza ? Mechon.Estado.Colocado : Mechon.Estado.EnBandeja;
        arrastrado = null;
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
            arrastrado.transform.SetPositionAndRotation(rayo.GetPoint(distanciaBandeja), RotacionHaciaCamara());
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

    void ColocarBandeja(bool inmediato)
    {
        if (camara == null) return;
        int n = Mathf.Max(1, numeroMechones);
        float k = inmediato ? 1f : 1f - Mathf.Exp(-velocidadBandeja * Time.deltaTime);
        Quaternion rot = RotacionHaciaCamara();
        float x = bandejaDerecha ? 1f - margenBandeja : margenBandeja;
        foreach (var m in mechones)
        {
            if (m.estado != Mechon.Estado.EnBandeja) continue;
            // Sin bandeja 3D, los mechones libres se esconden y salen desde el panel de UI.
            if (m.gameObject.activeSelf != mostrarBandeja3D) m.gameObject.SetActive(mostrarBandeja3D);
            if (!mostrarBandeja3D) continue;
            float y = n == 1 ? 0.5f : Mathf.Lerp(0.85f, 0.15f, m.indiceBandeja / (float)(n - 1));
            Vector3 objetivo = camara.ViewportToWorldPoint(new Vector3(x, y, distanciaBandeja));
            Vector3 p = Vector3.Lerp(m.transform.position, objetivo, k);
            if ((p - objetivo).sqrMagnitude < 0.0004f) p = objetivo;
            m.transform.SetPositionAndRotation(p, Quaternion.Slerp(m.transform.rotation, rot, k));
            m.transform.localScale = Vector3.Lerp(m.transform.localScale, m.escalaBase * escalaEnBandeja, k);
        }
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

        int disponibles = 0;
        foreach (var m in mechones) if (m.estado == Mechon.Estado.EnBandeja) disponibles++;
        MechonesDisponibles = disponibles;

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
    }
}
