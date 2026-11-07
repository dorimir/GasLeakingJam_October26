using System.Collections.Generic;
using UnityEngine;

/// Un mechón arrastrable. Necesita un Collider (en la raíz) para poder cogerlo con el ratón.
/// Estructura recomendada:
///   Mechon (este script + collider, escala 1)
///     Parche  -> lo visual del parche (lo puedes redibujar libremente)
///     Pelos   -> cada hijo es un pelo con el pivote en la raíz y creciendo hacia +Z
public class Mechon : MonoBehaviour
{
    public enum Estado { EnBandeja, Arrastrando, Colocado }

    [Tooltip("Contenedor de los pelos. Si está vacío busca un hijo llamado 'Pelos'.")]
    public Transform contenedorPelos;

    [HideInInspector] public Estado estado = Estado.EnBandeja;
    [HideInInspector] public Vector3 puntoSuperficie;   // punto de la cabeza donde está (mundo)
    [HideInInspector] public bool sobreCabeza;
    [HideInInspector] public int indiceBandeja;
    [HideInInspector] public Vector3 escalaBase;

    readonly List<Transform> pelos = new List<Transform>();
    float[] yaw = new float[0];
    float[] elevacion = new float[0];

    void Awake()
    {
        escalaBase = transform.localScale;
        if (!contenedorPelos) contenedorPelos = transform.Find("Pelos");
        if (contenedorPelos) foreach (Transform t in contenedorPelos) pelos.Add(t);
        yaw = new float[pelos.Count];
        elevacion = new float[pelos.Count];
        for (int i = 0; i < pelos.Count; i++)
        {
            Vector3 e = pelos[i].localEulerAngles;
            yaw[i] = e.y;
            elevacion[i] = -Mathf.DeltaAngle(0f, e.x);
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
}
