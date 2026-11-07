using UnityEngine;

/// Cámara de Resultados: la cámara queda fija y el GATO gira 360° sin parar.
public class Giro360 : MonoBehaviour
{
    [Tooltip("Si está vacío, busca un objeto con este nombre (el busto que viene del prototipo)")]
    public Transform objetivo;
    public string nombreObjetivo = "PuntoEnfoque";

    public float distancia = 4.2f;
    [Tooltip("Ángulo vertical de la cámara en grados (positivo = desde arriba)")]
    public float anguloVertical = 15f;
    [Tooltip("Grados por segundo que gira el gato")]
    public float velocidad = 35f;
    [Tooltip("Posición de la cámara: 180 = de frente, 0 = desde la nuca")]
    public float anguloInicial = 180f;

    Transform gato;
    Vector3 pivote;

    void Start()
    {
        if (!objetivo)
        {
            var go = GameObject.Find(nombreObjetivo);
            if (go) objetivo = go.transform;
        }
        pivote = objetivo ? objetivo.position : new Vector3(0f, 1.15f, 0f);
        if (objetivo) gato = objetivo.root;
        if (gato)
        {
            // si llega inclinado del juego, lo enderezamos (solo conserva el giro horizontal)
            Quaternion enderezar = Quaternion.FromToRotation(gato.up, Vector3.up);
            gato.SetPositionAndRotation(pivote + enderezar * (gato.position - pivote), enderezar * gato.rotation);
        }
        Quaternion rot = Quaternion.Euler(anguloVertical, anguloInicial, 0f);
        transform.SetPositionAndRotation(pivote + rot * new Vector3(0f, 0f, -distancia), rot);
    }

    void LateUpdate()
    {
        if (gato) gato.RotateAround(pivote, Vector3.up, velocidad * Time.deltaTime);
    }
}
