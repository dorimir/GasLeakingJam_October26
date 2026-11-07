using UnityEngine;

/// Define la zona calva de la cabeza con esferas (hijos de 'contenedorEsferas').
/// Mueve / escala las esferas en la escena y pulsa "Aplicar a la malla" en el inspector:
/// los triángulos dentro de las esferas pasan a la submalla calva (material brillante)
/// y el GestorMechones usa esa misma submalla para calcular el % de calvicie.
public class ZonaCalva : MonoBehaviour
{
    [Tooltip("Malla de la cabeza a la que se aplica la zona")]
    public MeshFilter malla;
    [Tooltip("Cada hijo es una esfera. Su tamaño = escala X (diámetro).")]
    public Transform contenedorEsferas;
    [Tooltip("Índice de la submalla calva. Si no existe se añade al final.")]
    public int submallaCalva = 1;
    [Tooltip("Material para la zona calva (si se añade una submalla nueva)")]
    public Material materialCalva;

    [Header("Gizmos")]
    public bool mostrarEsferas = true;
    public bool mostrarPuntos = true;

    public bool Contiene(Vector3 puntoMundo)
    {
        if (!contenedorEsferas) return false;
        foreach (Transform e in contenedorEsferas)
        {
            if (!e.gameObject.activeInHierarchy) continue;
            float r = Mathf.Abs(e.lossyScale.x) * 0.5f;
            if ((puntoMundo - e.position).sqrMagnitude < r * r) return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (!contenedorEsferas) return;
        if (mostrarEsferas)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            foreach (Transform e in contenedorEsferas)
                if (e.gameObject.activeInHierarchy)
                    Gizmos.DrawWireSphere(e.position, Mathf.Abs(e.lossyScale.x) * 0.5f);
        }
        if (mostrarPuntos && malla && malla.sharedMesh)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 1f);
            var v = malla.sharedMesh.vertices;
            var t = malla.transform;
            int dibujados = 0;
            for (int i = 0; i < v.Length && dibujados < 3000; i++)
            {
                Vector3 w = t.TransformPoint(v[i]);
                if (Contiene(w)) { Gizmos.DrawCube(w, Vector3.one * 0.008f); dibujados++; }
            }
        }
    }
}
