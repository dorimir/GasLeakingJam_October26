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
    [Tooltip("Multiplica lo que tapa cada mechón (parche y pelos) y cada mancha. Más = el % baja más con cada uno.")]
    public float multiplicadorCobertura = 2f;
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
    [Tooltip("Solo se pueden pegar mechones en el cuero cabelludo (la submalla calva)")]
    public bool soloEnCueroCabelludo = true;
    [Tooltip("Estricto: no deja pegar si hay pelo cerca o inclinado alrededor. Desactivado: solo si hay pelo justo encima.")]
    public bool bloqueoEstricto = false;
    [Tooltip("Margen (metros) alrededor del cuero cabelludo donde también se pueden pegar mechones")]
    public float margenZona = 0.08f;
    [Tooltip("Zona objetivo del nivel: cada hijo es una esfera (escala X = diámetro). Solo cuenta para el % lo que hay dentro, y solo ahí se pueden pegar mechones.")]
    public Transform zonaObjetivo;
    [Tooltip("Dentro de la zona objetivo, los mechones se pueden pegar ENCIMA del pelo del peinado (y tapan la calva de debajo)")]
    public bool pegarSobrePeloEnZona = true;
    [Tooltip("Hay que tapar TODA la zona objetivo: el % de partida baja según la parte de la zona que tapes con mechones (aunque haya pelo debajo)")]
    public bool zonaEnteraCuenta = false;
    [Tooltip("Con 'zona entera': % de alopecia al empezar el nivel. -1 = calcularlo solo.")]
    public float porcentajeInicialZona = -1f;
    [Tooltip("Con 'zona entera': qué parte de la zona hay que tapar para llegar a 0% (0.85 = el 85%)")]
    [Range(0.3f, 1f)] public float fraccionParaCompletar = 0.85f;
    float porcentajeBaseZona;
    [Tooltip("Hasta qué altura sobre la piel se busca pelo del peinado (metros)")]
    public float alcancePeinado = 0.5f;
    [Tooltip("Apertura (grados) de los rayos inclinados: más = el pelo tapa más alrededor")]
    public float aperturaPeinado = 20f;

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
    bool[] cubiertoPorPeinado;
    int triInicioCalva, triFinCalva;
    /// % del cuero cabelludo que tapa el peinado (informativo)
    public float PeinadoCubre { get; private set; }
    Vector3[] muestrasLocales = new Vector3[0];
    Vector3[] normalesLocales = new Vector3[0];
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
        if (pegarSobrePeloEnZona && HayZonaObjetivo() && PegarSobrePelo(rayo)) return;
        if (colliderCabeza && colliderCabeza.Raycast(rayo, out RaycastHit hit, 100f))
        {
            // no se puede pegar fuera del cuero cabelludo (cara, orejas, cuerpo) ni sobre el peinado
            bool fueraDeZona = HayZonaObjetivo()
                ? !DentroZonaObjetivo(hit.point, margenZona)          // solo dentro de la zona objetivo del nivel (con margen)
                : (soloEnCueroCabelludo && hit.triangleIndex >= 0 && triFinCalva > 0 &&
                   (hit.triangleIndex < triInicioCalva || hit.triangleIndex >= triFinCalva) &&
                   !CercaDelCueroCabelludo(hit.point));
            bool sobrePelo = bloqueoEstricto
                ? (HayPeloCerca(hit.point) || PeinadoEncima(hit.point, hit.normal))
                : TocaPeinado(hit.point + hit.normal * 0.003f, hit.normal);   // solo si hay pelo justo encima
            bool bloqueado = fueraDeZona || sobrePelo;
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

    /// Si el ratón apunta al pelo del peinado y debajo está la zona objetivo, pega el mechón encima del pelo.
    bool PegarSobrePelo(Ray rayo)
    {
        // pelo más cercano bajo el ratón
        var hits = Physics.RaycastAll(rayo, 100f, ~0, QueryTriggerInteraction.Collide);
        RaycastHit pelo = default(RaycastHit); bool hayPelo = false;
        foreach (var h in hits)
            if (h.collider && h.collider.GetComponentInParent<PeloBloqueo>() && (!hayPelo || h.distance < pelo.distance)) { pelo = h; hayPelo = true; }
        if (!hayPelo) return false;
        // si la cabeza está delante del pelo, no es este caso
        if (colliderCabeza && colliderCabeza.Raycast(rayo, out RaycastHit cab, 100f) && cab.distance < pelo.distance) return false;
        // piel de debajo del pelo
        Vector3 piel = pelo.point; Vector3 normal = pelo.normal;
        Vector3 haciaDentro = mallaCabeza ? (mallaCabeza.transform.position + Vector3.up * 0.5f - pelo.point).normalized : -pelo.normal;
        if (colliderCabeza && colliderCabeza.Raycast(new Ray(pelo.point - haciaDentro * 0.02f, haciaDentro), out RaycastHit pielHit, 1f))
        { piel = pielHit.point; normal = pielHit.normal; }
        if (!DentroZonaObjetivo(piel, margenZona) && !DentroZonaObjetivo(pelo.point, margenZona)) return false;

        arrastrado.sobreCabeza = true;
        arrastrado.puntoSuperficie = piel;
        arrastrado.transform.SetPositionAndRotation(pelo.point + normal * separacionSuperficie, RotacionSobreSuperficie(normal));
        arrastrado.MarcarBloqueado(false);
        return true;
    }

    /// ¿Está el punto en el cuero cabelludo o a menos de 'margenZona' de él?
    bool CercaDelCueroCabelludo(Vector3 punto)
    {
        if (!mallaCabeza) return true;
        var tr = mallaCabeza.transform;
        float m2 = margenZona * margenZona;
        for (int i = 0; i < muestrasLocales.Length; i += 2)
            if ((tr.TransformPoint(muestrasLocales[i]) - punto).sqrMagnitude < m2) return true;
        return false;
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
        var normalesLista = new List<Vector3>();
        var malla = mallaCabeza ? mallaCabeza.sharedMesh : null;
        if (malla != null && submallaCalva < malla.subMeshCount)
        {
            var v = malla.vertices;
            var nv = malla.normals;
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
                    normalesLista.Add((nv[t[i * 3]] + nv[t[i * 3 + 1]] + nv[t[i * 3 + 2]]).normalized);
                }
            }
        }
        muestrasLocales = lista.ToArray();
        normalesLocales = normalesLista.ToArray();
        CalcularPeinado();
        CalcularZona();
        if (malla != null && submallaCalva < malla.subMeshCount)
        {
            var sm = malla.GetSubMesh(submallaCalva);
            triInicioCalva = sm.indexStart / 3;
            triFinCalva = triInicioCalva + sm.indexCount / 3;
        }
        if (muestrasLocales.Length == 0)
            Debug.LogWarning("[GestorMechones] No se encontró la zona calva (revisa mallaCabeza y submallaCalva).");
    }

    /// Recalcula el % de calvicie (lo llaman también el peine y el grafitti).
    public void RecalcularCobertura() { Recalcular(); }

    /// Marca qué puntos del cuero cabelludo tapa el peinado activo (objetos con PeloBloqueo).
    /// Llámalo si cambias de peinado en mitad de la partida.
    public void CalcularPeinado()
    {
        cubiertoPorPeinado = new bool[muestrasLocales.Length];
        if (!mallaCabeza) return;
        Physics.SyncTransforms();
        var tr = mallaCabeza.transform;
        int n = 0;
        for (int i = 0; i < muestrasLocales.Length; i++)
        {
            Vector3 w = tr.TransformPoint(muestrasLocales[i]);
            Vector3 nrm = i < normalesLocales.Length ? tr.TransformDirection(normalesLocales[i]).normalized : (w - tr.position).normalized;
            // un rayo recto hacia fuera y 4 inclinados: si alguno toca pelo, ese punto está tapado
            Vector3 t1 = Vector3.Cross(nrm, Vector3.up); if (t1.sqrMagnitude < 1e-4f) t1 = Vector3.Cross(nrm, Vector3.right);
            t1.Normalize(); Vector3 t2 = Vector3.Cross(nrm, t1);
            float inc = Mathf.Tan(aperturaPeinado * Mathf.Deg2Rad);
            Vector3 origen = w + nrm * 0.003f;
            if (TocaPeinado(origen, nrm) || TocaPeinado(origen, nrm + t1 * inc) || TocaPeinado(origen, nrm - t1 * inc) ||
                TocaPeinado(origen, nrm + t2 * inc) || TocaPeinado(origen, nrm - t2 * inc))
            { cubiertoPorPeinado[i] = true; n++; }
        }
        PeinadoCubre = muestrasLocales.Length > 0 ? 100f * n / muestrasLocales.Length : 0f;
    }

    readonly RaycastHit[] golpes = new RaycastHit[8];
    /// ¿Hay pelo del peinado encima de este punto de la piel? (mismo criterio que el contador)
    bool PeinadoEncima(Vector3 punto, Vector3 normal)
    {
        Vector3 n = normal.normalized;
        Vector3 t1 = Vector3.Cross(n, Vector3.up); if (t1.sqrMagnitude < 1e-4f) t1 = Vector3.Cross(n, Vector3.right);
        t1.Normalize(); Vector3 t2 = Vector3.Cross(n, t1);
        float inc = Mathf.Tan(aperturaPeinado * Mathf.Deg2Rad);
        Vector3 o = punto + n * 0.003f;
        return TocaPeinado(o, n) || TocaPeinado(o, n + t1 * inc) || TocaPeinado(o, n - t1 * inc) ||
               TocaPeinado(o, n + t2 * inc) || TocaPeinado(o, n - t2 * inc);
    }

    bool TocaPeinado(Vector3 origen, Vector3 dir)
    {
        int k = Physics.RaycastNonAlloc(origen, dir.normalized, golpes, alcancePeinado, ~0, QueryTriggerInteraction.Collide);
        for (int j = 0; j < k; j++)
            if (golpes[j].collider && golpes[j].collider.GetComponentInParent<PeloBloqueo>()) return true;
        return false;
    }

    /// Registra una mancha de pintura que tapa la calva (grafitti).
    public void RegistrarMancha(Vector3 punto, float radio)
    {
        // se guarda relativa a la cabeza, para que siga tapando aunque el gato gire
        Vector3 local = mallaCabeza ? mallaCabeza.transform.InverseTransformPoint(punto) : punto;
        manchas.Add(new Vector4(local.x, local.y, local.z, radio));
        Recalcular();
    }

    // ---------- Zona objetivo (esferas hijas de 'zonaObjetivo') ----------
    bool[] enZona;
    int totalEnZona;

    bool HayZonaObjetivo()
    {
        if (!zonaObjetivo) return false;
        foreach (Transform e in zonaObjetivo) if (e.gameObject.activeInHierarchy) return true;
        return false;
    }

    bool DentroZonaObjetivo(Vector3 p, float margen = 0f)
    {
        foreach (Transform e in zonaObjetivo)
        {
            if (!e.gameObject.activeInHierarchy) continue;
            float r = Mathf.Abs(e.lossyScale.x) * 0.5f + margen;
            if ((p - e.position).sqrMagnitude < r * r) return true;
        }
        return false;
    }

    /// Marca qué puntos del cuero cabelludo están dentro de la zona objetivo (lo único que cuenta).
    public void CalcularZona()
    {
        enZona = null; totalEnZona = 0;
        if (!HayZonaObjetivo() || !mallaCabeza) return;
        var tr = mallaCabeza.transform;
        enZona = new bool[muestrasLocales.Length];
        for (int i = 0; i < muestrasLocales.Length; i++)
            if (DentroZonaObjetivo(tr.TransformPoint(muestrasLocales[i]))) { enZona[i] = true; totalEnZona++; }
        // % de partida (solo con el peinado) para el modo 'zona entera'
        int calvas = 0;
        for (int i = 0; i < muestrasLocales.Length; i++)
            if (enZona[i] && !(cubiertoPorPeinado != null && i < cubiertoPorPeinado.Length && cubiertoPorPeinado[i])) calvas++;
        porcentajeBaseZona = porcentajeInicialZona >= 0f ? porcentajeInicialZona : (totalEnZona > 0 ? 100f * calvas / totalEnZona : 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (!zonaObjetivo) return;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        foreach (Transform e in zonaObjetivo)
            if (e.gameObject.activeInHierarchy) Gizmos.DrawWireSphere(e.position, Mathf.Abs(e.lossyScale.x) * 0.5f);
    }

    void Recalcular()
    {
        int cubiertas = 0;
        Transform tc = mallaCabeza ? mallaCabeza.transform : null;
        if (tc != null)
        {
            for (int i = 0; i < muestrasLocales.Length; i++)
            {
                if (enZona != null && i < enZona.Length && !enZona[i]) continue;   // fuera de la zona objetivo: no cuenta
                if (cubiertoPorPeinado != null && i < cubiertoPorPeinado.Length && cubiertoPorPeinado[i]) { cubiertas++; continue; }
                Vector3 w = tc.TransformPoint(muestrasLocales[i]);
                if (Cubierto(w)) cubiertas++;
            }
        }
        int total = totalEnZona > 0 ? totalEnZona : muestrasLocales.Length;
        PorcentajeCalvicie = total > 0 ? 100f * (1f - cubiertas / (float)total) : 0f;
        if (zonaEnteraCuenta && enZona != null && totalEnZona > 0 && tc != null)
        {
            // el % de partida baja según cuánta zona objetivo tapas (con mechones/manchas, pelo incluido debajo)
            int tapadas = 0;
            for (int i = 0; i < muestrasLocales.Length; i++)
                if (enZona[i] && Cubierto(tc.TransformPoint(muestrasLocales[i]))) tapadas++;
            float frac = tapadas / (float)totalEnZona / Mathf.Max(0.01f, fraccionParaCompletar);   // tapando esa fracción ya es 0%
            PorcentajeCalvicie = porcentajeBaseZona * Mathf.Clamp01(1f - frac);
        }
        MechonesDisponibles = Mathf.Max(0, numeroMechones - mechones.Count);
        ActualizarUI();
    }

    bool Cubierto(Vector3 w)
    {
        foreach (var mc in manchas)
        {
            Vector3 p = mallaCabeza ? mallaCabeza.transform.TransformPoint(new Vector3(mc.x, mc.y, mc.z)) : new Vector3(mc.x, mc.y, mc.z);
            float rm = mc.w * multiplicadorCobertura;
            if ((w - p).sqrMagnitude < rm * rm) return true;
        }
        foreach (var m in mechones)
        {
            bool cuenta = m.estado == Mechon.Estado.Colocado ||
                          (m.estado == Mechon.Estado.Arrastrando && m.sobreCabeza);
            if (cuenta && m.Cubre(w, radioCobertura * multiplicadorCobertura, multiplicadorCobertura)) return true;
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
        if (textoCalvicie) textoCalvicie.text = $"{Mathf.RoundToInt(PorcentajeCalvicie)}%";   // solo el número
        if (textoMechones) textoMechones.text = $"x {MechonesDisponibles}";
        if (iconoContador)
        {
            var c = colorIcono;
            if (MechonesDisponibles == 0) c.a *= 0.3f;
            iconoContador.color = c;
        }
    }
}
