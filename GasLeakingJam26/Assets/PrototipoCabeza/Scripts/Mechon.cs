using UnityEngine;

/// Un mechón arrastrable. Necesita un Collider para poder cogerlo con el ratón.
public class Mechon : MonoBehaviour
{
    public enum Estado { EnBandeja, Arrastrando, Colocado }

    [HideInInspector] public Estado estado = Estado.EnBandeja;
    [HideInInspector] public Vector3 puntoSuperficie;   // punto de la cabeza donde está (mundo)
    [HideInInspector] public bool sobreCabeza;
    [HideInInspector] public int indiceBandeja;
    [HideInInspector] public Vector3 escalaBase;

    void Awake() { escalaBase = transform.localScale; }
}
