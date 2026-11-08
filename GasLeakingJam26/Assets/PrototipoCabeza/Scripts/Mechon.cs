using System.Collections.Generic;
using UnityEngine;

/// Un mechón arrastrable. Necesita un Collider (en la raíz) para poder cogerlo con el ratón.
/// Estructura:
///   Mechon (este script + collider)
///     Parche  -> lo visual del parche (sprite)
///     Pelos   -> cada hijo es un pelo con el pivote en la raíz y creciendo hacia +Z
/// Tapa la calva con el parche y también con sus pelos (si los peinas encima de la calva, cuentan).
public class Mechon : MonoBehaviour
{
    public enum Estado { EnBandeja, Arrastrando, Colocado }

    [Tooltip("Contenedor de los pelos. Si está vacío busca un hijo llamado 'Pelos'.")]
    public Transform contenedorPelos;
    [Tooltip("Grosor (radio, en metros a escala 1) de lo que tapa cada pelo")]
    public float radioPelo = 0.03f;

    [HideInInspector] public Estado estado = Estado.EnBandeja;
    [HideInInspector] public Vector3 puntoSuperficie;   // punto de la cabeza donde está (mundo)
    [HideInInspector] public bool sobreCabeza;
    [HideInInspector] public int indiceBandeja;
    [HideInInspector] public Vector3 escalaBase;

    readonly List<Transform> pelos = new List<Transform>();
    float[] yaw = new float[0];
    float[] elevacion = new float[0];
    float[] largo = new float[0];

    void Awake()
    {
        escalaBase = transform.localScale;
        if (!contenedorPelos) contenedorPelos = transform.Find("Pelos");
        if (contenedorPelos) foreach (Transform t in contenedorPelos) pelos.Add(t);
        yaw = new float[pelos.Count];
        elevacion = new float[pelos.Count];
        largo = new float[pelos.Count];
        for (int i = 0; i < pelos.Count; i++)
        {
            Vector3 e = pelos[i].localEulerAngles;
            yaw[i] = e.y;
            elevacion[i] = -Mathf.DeltaAngle(0f, e.x);
            // largo del pelo: hasta la punta de su hijo visual (o 0.15 si no tiene)
            float l = 0.15f;
            if (pelos[i].childCount > 0)
            {
                var h = pelos[i].GetChild(0);
                l = Mathf.Max(0.02f, h.localPosition.z * 2f);
            }
            largo[i] = l;
        }
    }

    /// Orienta los pelos cercanos a 'punto' hacia la dirección del peinado.
    public void Peinar(Vector3 punto, Vector3 direccion, float radio, float fuerza)
    {
        if (!contenedorPelos) return;
        Vector3 dl = contenedorPelos.InverseTransformDirection(direccion);
        dl.y = 0f;
        if (dl.sqrMagnitude < 1e-8f) return;
        float objetivo = Mathf.Atan2(dl.x, dl.z) * Mathf.Rad2Deg;
        for (int i = 0; i < pelos.Count; i++)
        {
            float d = Vector3.Distance(pelos[i].position, punto);
            if (d > radio) continue;
            float w = Mathf.Clamp01(fuerza * (1f - d / radio));
            yaw[i] = Mathf.LerpAngle(yaw[i], objetivo, w);
            pelos[i].localRotation = Quaternion.Euler(-elevacion[i], yaw[i], 0f);
        }
    }

    /// ¿Tapa este mechón el punto 'w' de la cabeza? (parche + pelos)
    public bool Cubre(Vector3 w, float radioParche, float factorPelo = 1f)
    {
        float escala = transform.lossyScale.x;
        float rp = radioParche * escala;
        // distancia medida "en planta" (sin la altura): si el mechón está encima del pelo, sigue tapando la piel de debajo
        Vector3 d = w - transform.position;
        float alto = Vector3.Dot(d, transform.up);
        Vector3 plano = d - transform.up * alto;
        if (Mathf.Abs(alto) < 0.3f && plano.sqrMagnitude < rp * rp) return true;
        float rpelo = radioPelo * escala * factorPelo;
        float r2 = rpelo * rpelo;
        for (int i = 0; i < pelos.Count; i++)
        {
            Vector3 a = pelos[i].position;
            Vector3 b = pelos[i].TransformPoint(new Vector3(0f, 0f, largo[i]));
            if (DistanciaSegmento2(w, a, b) < r2) return true;
        }
        return false;
    }

    MaterialPropertyBlock bloque;
    Renderer[] renderers;
    bool marcadoBloqueado;

    /// Tinte rojo mientras está sobre una zona donde no se puede pegar.
    public void MarcarBloqueado(bool bloqueado)
    {
        if (bloqueado == marcadoBloqueado) return;
        marcadoBloqueado = bloqueado;
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>();
        if (bloque == null) bloque = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (bloqueado)
            {
                r.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f));
                r.SetPropertyBlock(bloque);
            }
            else r.SetPropertyBlock(null);
        }
    }

    static float DistanciaSegmento2(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-8f, ab.sqrMagnitude));
        return (a + ab * t - p).sqrMagnitude;
    }
}
