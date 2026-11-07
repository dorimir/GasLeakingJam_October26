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
    [Tooltip("Puntos de muestra sobre la zona calva para calcular el % (más = más preciso)")]
    public int muestrasZonaCalva = 800;

    [Header("Arrastre")]
    public Camera camara;
    [Tooltip("Distancia a la cámara del mechón mientras lo arrastras fuera de la cabeza")]
    public float distanciaArrastre = 2.5f;
    [Tooltip("Distancia (a escala 1 del mechón) a un pelo medio calvo dentro de la cual no se puede pegar")]
    public float radioBloqueo = 0.09f;

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
    readonly List<Vector4> manchas = new List<Vector4>();   // xyz = punto, w = radio
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
            m.MarcarBloqueado(false);
            if (colliderCabeza) m.transform.SetParent(colliderCabeza.transform, true);   // gira con el gato
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
            bool bloqueado = HayPeloCerca(hit.point);
            arrastrado.sobreCabeza = !bloqueado;          // sobre un pelo: no se pega (al soltar vuelve al contador)
            arrastrado.puntoSuperficie = hit.point;
            arrastrado.transform.SetPositionAndRotation(
                hit.point + hit.normal * separacionSuperficie,
                RotacionSobreSuperficie(hit.normal));
            arrastrado.MarcarBloqueado(bloqueado);
        }
        else
        {
            arrastrado.sobreCabeza = false;
            arrastrado.transform.SetPositionAndRotation(rayo.GetPoint(distanciaArrastre), RotacionHaciaCamara());
            arrastrado.MarcarBloqueado(false);
        }
    }

    readonly Collider[] cercanos = new Collider[16];

    /// ¿Hay algún pelo medio calvo (PeloBloqueo) cerca de este punto?
    bool HayPeloCerca(Vector3 punto)
    {
        float r = radioBloqueo * (arrastrado ? arrastrado.transform.lossyScale.x : 1f);
        int n = Physics.OverlapSphereNonAlloc(punto, r, cercanos, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (cercanos[i] && cercanos[i].GetComponentInParent<PeloBloqueo>()) return true;
        return false;
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
        return Mouse.current != null && UtilUI.PunteroSobreUIInteractiva(Mouse.current.position.ReadValue());
    }

    // ---------- Calvicie ----------

    void PrepararMuestras()
    {
        // Puntos repartidos por área sobre los triángulos de la zona calva (sirve también para mallas low-poly).
        var lista = new List<Vector3>();
        var malla = mallaCabeza ? mallaCabeza.sharedMesh : null;
        if (malla != null && submallaCalva < malla.subMeshCount)
        {
            var v = malla.vertices;
            var t = malla.GetTriangles(submallaCalva);
            var tr = mallaCabeza.transform;
            int nTri = t.Length / 3;
            var areas = new float[nTri];
            float total = 0f;
            for (int i = 0; i < nTri; i++)
            {
                Vector3 a = tr.TransformPoint(v[t[i * 3]]), b = tr.TransformPoint(v[t[i * 3 + 1]]), c = tr.TransformPoint(v[t[i * 3 + 2]]);
                areas[i] = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                total += areas[i];
            }
            var rnd = new System.Random(12345);
            for (int i = 0; i < nTri && total > 0f; i++)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(muestrasZonaCalva * areas[i] / total));
                Vector3 a = v[t[i * 3]], b = v[t[i * 3 + 1]], c = v[t[i * 3 + 2]];
                for (int k = 0; k < n; k++)
                {
                    float r1 = Mathf.Sqrt((float)rnd.NextDouble()), r2 = (float)rnd.NextDouble();
                    lista.Add((1f - r1) * a + r1 * (1f - r2) * b + r1 * r2 * c);
                }
            }
        }
        muestrasLocales = lista.ToArray();
        if (muestrasLocales.Length == 0)
            Debug.LogWarning("[GestorMechones] No se encontró la zona calva (revisa mallaCabeza y submallaCalva).");
    }

    /// Recalcula el % de calvicie (lo llaman también el peine y el grafitti).
    public void RecalcularCobertura() { Recalcular(); }

    /// Registra una mancha de pintura que tapa la calva (grafitti).
    public void RegistrarMancha(Vector3 punto, float radio)
    {
        // se guarda relativa a la cabeza, para que siga tapando aunque el gato gire
        Vector3 local = mallaCabeza ? mallaCabeza.transform.InverseTransformPoint(punto) : punto;
        manchas.Add(new Vector4(local.x, local.y, local.z, radio));
        Recalcular();
    }

    void Recalcular()
    {
        int cubiertas = 0;
        Transform tc = mallaCabeza ? mallaCabeza.transform : null;
        if (tc != null)
        {
            foreach (var s in muestrasLocales)
            {
                Vector3 w = tc.TransformPoint(s);
                if (Cubierto(w)) cubiertas++;
            }
        }
        int total = muestrasLocales.Length;
        PorcentajeCalvicie = total > 0 ? 100f * (1f - cubiertas / (float)total) : 0f;
        MechonesDisponibles = Mathf.Max(0, numeroMechones - mechones.Count);
        ActualizarUI();
    }

    bool Cubierto(Vector3 w)
    {
        foreach (var mc in manchas)
        {
            Vector3 p = mallaCabeza ? mallaCabeza.transform.TransformPoint(new Vector3(mc.x, mc.y, mc.z)) : new Vector3(mc.x, mc.y, mc.z);
            if ((w - p).sqrMagnitude < mc.w * mc.w) return true;
        }
        foreach (var m in mechones)
        {
            bool cuenta = m.estado == Mechon.Estado.Colocado ||
                          (m.estado == Mechon.Estado.Arrastrando && m.sobreCabeza);
            if (cuenta && m.Cubre(w, radioCobertura)) return true;
        }
        return false;
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
