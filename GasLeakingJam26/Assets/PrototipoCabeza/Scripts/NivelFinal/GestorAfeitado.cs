using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Nivel final: aparecen unos pocos mechones al azar en la zona marcada con esferas
/// (parte trasera / entradas) y la única herramienta es la maquinilla: arrastra con clic
/// izquierdo sobre la cabeza para afeitarlos.
public class GestorAfeitado : MonoBehaviour
{
    [Header("Mechones aleatorios")]
    public Mechon prefabMechon;
    [Min(0)] public int numeroMechones = 6;
    public float escalaMin = 0.8f, escalaMax = 1.15f;
    [Tooltip("Distancia mínima entre mechones")]
    public float separacionMinima = 0.18f;
    public float separacionSuperficie = 0.01f;
    [Tooltip("Si está desactivado, usa 'semilla' para que salgan siempre en el mismo sitio")]
    public bool aleatorioCadaVez = true;
    public int semilla = 1;

    [Header("Cabeza")]
    public Collider colliderCabeza;
    [Tooltip("Centro aproximado de la cabeza (los mechones se proyectan desde aquí hacia fuera)")]
    public Transform centroCabeza;
    [Tooltip("Cada hijo es una esfera (escala X = diámetro) donde pueden salir mechones")]
    public Transform zonaMechones;
    [Tooltip("Los mechones se hacen hijos de este objeto (el busto) para viajar a Resultados")]
    public Transform busto;

    [Header("Maquinilla")]
    public Camera camara;
    [Tooltip("Radio de corte alrededor del cursor (metros)")]
    public float radioMaquinilla = 0.12f;
    public float duracionCorte = 0.35f;
    public RectTransform cursorMaquinilla;
    [Tooltip("Sprite 2D de la maquinilla: al pulsarlo la coges (y al volver a pulsarlo la sueltas)")]
    public Graphic spriteMaquinilla;
    [Tooltip("Segundos tras afeitar el último mechón antes de pasar solo a la cinemática")]
    public float esperaFinal = 3f;
    public bool Equipada { get; private set; }

    [Header("UI")]
    public Image rellenoBarra;
    public Text textoContador;
    public string textoCompleto = "¡Afeitado completo!";

    [Header("Final")]
    public string escenaResultados = "Resultados";

    public int Restantes => vivos.Count;
    public int Total { get; private set; }
    public float PorcentajeAfeitado => Total == 0 ? 100f : 100f * (Total - Restantes) / Total;

    readonly List<Mechon> vivos = new List<Mechon>();
    bool terminado;
    Coroutine autoFinal;
    bool afeitando;

    void Start()
    {
        if (!camara) camara = Camera.main;
        if (!busto) busto = transform;
        GenerarMechones();
        ActualizarUI();
        ActualizarMaquinilla();
    }

    // ---------- Generación ----------

    void GenerarMechones()
    {
        var esferas = new List<Transform>();
        if (zonaMechones) foreach (Transform e in zonaMechones) if (e.gameObject.activeInHierarchy) esferas.Add(e);
        if (esferas.Count == 0 || !colliderCabeza || !prefabMechon) { Debug.LogWarning("[Afeitado] Falta zona, collider o prefab."); return; }

        var estado = Random.state;
        if (!aleatorioCadaVez) Random.InitState(semilla);
        Vector3 centro = centroCabeza ? centroCabeza.position : colliderCabeza.bounds.center;

        for (int intento = 0; intento < numeroMechones * 60 && vivos.Count < numeroMechones; intento++)
        {
            var e = esferas[Random.Range(0, esferas.Count)];
            float r = Mathf.Abs(e.lossyScale.x) * 0.5f;
            Vector3 q = e.position + Random.insideUnitSphere * r;
            Vector3 dir = (q - centro).normalized;
            if (dir.sqrMagnitude < 1e-6f) continue;
            if (!colliderCabeza.Raycast(new Ray(centro + dir * 5f, -dir), out RaycastHit hit, 10f)) continue;
            if (!DentroDeZona(hit.point, esferas)) continue;
            bool lejos = true;
            foreach (var m in vivos) if ((m.puntoSuperficie - hit.point).sqrMagnitude < separacionMinima * separacionMinima) { lejos = false; break; }
            if (!lejos) continue;

            var nuevo = Instantiate(prefabMechon, busto);
            nuevo.name = "MechonFinal_" + (vivos.Count + 1);
            nuevo.estado = Mechon.Estado.Colocado;
            nuevo.sobreCabeza = true;
            nuevo.puntoSuperficie = hit.point;
            // el pelo cae hacia abajo siguiendo la superficie, con algo de variación
            Vector3 abajo = Vector3.ProjectOnPlane(Vector3.down, hit.normal);
            if (abajo.sqrMagnitude < 1e-4f) abajo = Vector3.ProjectOnPlane(Vector3.forward, hit.normal);
            Quaternion rot = Quaternion.LookRotation(abajo.normalized, hit.normal) * Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f);
            nuevo.transform.SetPositionAndRotation(hit.point + hit.normal * separacionSuperficie, rot);
            nuevo.transform.localScale *= Random.Range(escalaMin, escalaMax);
            var col = nuevo.GetComponent<Collider>();
            if (col) col.enabled = false;
            vivos.Add(nuevo);
        }
        if (!aleatorioCadaVez) Random.state = estado;
        Total = vivos.Count;
    }

    static bool DentroDeZona(Vector3 p, List<Transform> esferas)
    {
        foreach (var e in esferas)
        {
            float r = Mathf.Abs(e.lossyScale.x) * 0.5f + 0.05f;
            if ((p - e.position).sqrMagnitude < r * r) return true;
        }
        return false;
    }

    // ---------- Maquinilla ----------

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || camara == null) return;
        Vector2 pos = mouse.position.ReadValue();
        if (cursorMaquinilla && Equipada) cursorMaquinilla.position = pos;

        if (mouse.leftButton.wasPressedThisFrame)
            afeitando = !UtilUI.PunteroSobreUIInteractiva(pos);
        if (!mouse.leftButton.isPressed) { afeitando = false; return; }
        if (!afeitando || !Equipada || vivos.Count == 0) return;

        Ray rayo = EfectoPixelado.RayoDesdePantalla(camara, pos);
        if (!colliderCabeza.Raycast(rayo, out RaycastHit hit, 100f)) return;

        float r2 = radioMaquinilla * radioMaquinilla;
        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            if ((vivos[i].transform.position - hit.point).sqrMagnitude > r2) continue;
            var m = vivos[i];
            vivos.RemoveAt(i);
            StartCoroutine(Cortar(m, hit.normal));
        }
        ActualizarUI();
        if (vivos.Count == 0 && autoFinal == null) autoFinal = StartCoroutine(FinalAutomatico());
    }

    IEnumerator Cortar(Mechon m, Vector3 normal)
    {
        Vector3 escala = m.transform.localScale;
        Vector3 inicio = m.transform.position;
        Vector3 caida = normal * 0.15f + Vector3.down * 0.6f;
        Quaternion giro = Quaternion.Euler(Random.Range(-90f, 90f), Random.Range(-90f, 90f), Random.Range(-90f, 90f));
        Quaternion rot0 = m.transform.rotation;
        float t = 0f;
        while (t < 1f && m)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duracionCorte);
            float k = Mathf.Clamp01(t);
            m.transform.position = inicio + caida * (k * k);
            m.transform.rotation = Quaternion.Slerp(rot0, rot0 * giro, k);
            m.transform.localScale = escala * (1f - k);
            yield return null;
        }
        if (m) Destroy(m.gameObject);
    }

    void ActualizarUI()
    {
        float quedan = Total == 0 ? 0f : Restantes / (float)Total;
        if (rellenoBarra) rellenoBarra.fillAmount = quedan;
        if (textoContador) textoContador.text = Restantes == 0 ? textoCompleto : $"Mechones: {Restantes}";
    }

    // ---------- Final ----------

    /// Botón "Listo!": lleva el busto (afeitado) a Resultados.
    /// Llamado al pulsar el sprite de la maquinilla: la coges o la sueltas.
    public void AlternarMaquinilla()
    {
        if (terminado) return;
        Equipada = !Equipada;
        ActualizarMaquinilla();
    }

    void ActualizarMaquinilla()
    {
        if (cursorMaquinilla) cursorMaquinilla.gameObject.SetActive(Equipada);
        if (spriteMaquinilla)
        {
            var c = spriteMaquinilla.color;
            c.a = Equipada ? 0.35f : 1f;   // en la mano: el sprite del estante se atenúa
            spriteMaquinilla.color = c;
        }
    }

    IEnumerator FinalAutomatico()
    {
        float t = 0f;
        while (t < esperaFinal) { t += Time.unscaledDeltaTime; yield return null; }
        Terminar();   // si ya se pulsó "Listo!", no hace nada
    }

    public void Terminar()
    {
        if (terminado || Transicion.Ocupado || busto == null) return;
        terminado = true;
        // En Resultados, el % es la probabilidad de que te pillen: aquí son los mechones que quedan sin afeitar.
        ResultadoPartida.porcentajeCalvicie = 100f - PorcentajeAfeitado;
        ResultadoPartida.mechonesUsados = Total - Restantes;
        ResultadoPartida.mechonesTotales = Total;
        ResultadoPartida.escenaOrigen = gameObject.scene.name;
        Transicion.Ir(escenaResultados, () =>
        {
            if (busto.parent != null) busto.SetParent(null, true);
            DontDestroyOnLoad(busto.gameObject);
            ResultadoPartida.busto = busto.gameObject;
        });
    }

    void OnDrawGizmos()
    {
        if (!zonaMechones) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        foreach (Transform e in zonaMechones)
            if (e.gameObject.activeInHierarchy) Gizmos.DrawWireSphere(e.position, Mathf.Abs(e.lossyScale.x) * 0.5f);
    }
}
