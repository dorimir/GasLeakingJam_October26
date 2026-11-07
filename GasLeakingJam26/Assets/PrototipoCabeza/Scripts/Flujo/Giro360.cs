using UnityEngine;

/// Cámara de Resultados: gira 360° sin parar alrededor del objetivo.
public class Giro360 : MonoBehaviour
{
    [Tooltip("Si está vacío, busca un objeto con este nombre (el busto que viene del prototipo)")]
    public Transform objetivo;
    public string nombreObjetivo = "PuntoEnfoque";

    public float distancia = 4.2f;
    [Tooltip("Ángulo vertical en grados (positivo = desde arriba)")]
    public float anguloVertical = 15f;
    [Tooltip("Grados por segundo")]
    public float velocidad = 35f;
    [Tooltip("180 = empieza de frente, 0 = desde la nuca")]
    public float anguloInicial = 180f;

    float yaw;

    void Start()
    {
        if (!objetivo)
        {
            var go = GameObject.Find(nombreObjetivo);
            if (go) objetivo = go.transform;
        }
        yaw = anguloInicial;
        Aplicar();
    }

    void LateUpdate()
    {
        yaw = Mathf.Repeat(yaw + velocidad * Time.deltaTime, 360f);
        Aplicar();
    }

    void Aplicar()
    {
        Vector3 centro = objetivo ? objetivo.position : new Vector3(0f, 1.15f, 0f);
        Quaternion rot = Quaternion.Euler(anguloVertical, yaw, 0f);
        transform.SetPositionAndRotation(centro + rot * new Vector3(0f, 0f, -distancia), rot);
    }
}
