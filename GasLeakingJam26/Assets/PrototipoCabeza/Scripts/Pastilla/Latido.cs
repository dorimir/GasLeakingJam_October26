using UnityEngine;

/// Pulso de escala continuo para llamar la atención (sprites que no son botones).
public class Latido : MonoBehaviour
{
    [Tooltip("Lo que se escala (vacío = este objeto)")]
    public Transform objetivo;
    [Tooltip("Cuánto crece en cada latido (0.1 = 10%)")]
    public float amplitud = 0.1f;
    [Tooltip("Latidos por segundo")]
    public float frecuencia = 1.2f;

    Vector3 escalaBase;

    void Awake()
    {
        if (!objetivo) objetivo = transform;
        escalaBase = objetivo.localScale;
    }

    void OnDisable() { if (objetivo) objetivo.localScale = escalaBase; }

    void Update()
    {
        float fase = Mathf.Repeat(Time.unscaledTime * frecuencia, 1f);
        objetivo.localScale = escalaBase * (1f + amplitud * Mathf.Pow(Mathf.Sin(fase * Mathf.PI), 3f));
    }
}
