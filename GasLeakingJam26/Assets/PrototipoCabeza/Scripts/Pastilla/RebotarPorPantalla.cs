using UnityEngine;

/// Mueve el objeto por la pantalla rebotando en los bordes, como el logo del DVD de las teles antiguas.
/// 'tamanoVisible' y 'centroVisible' describen la parte que se ve (en unidades del Canvas, relativas a este objeto),
/// para que rebote con los bordes de lo que se ve y no con el rectángulo entero.
public class RebotarPorPantalla : MonoBehaviour
{
    [Tooltip("Velocidad en unidades del Canvas por segundo (muy lento = 20-40)")]
    public float velocidad = 30f;
    [Tooltip("Tamaño de lo que se ve (ancho, alto) en unidades del Canvas")]
    public Vector2 tamanoVisible = new Vector2(300f, 200f);
    [Tooltip("Centro de lo que se ve respecto a la posición de este objeto")]
    public Vector2 centroVisible = Vector2.zero;
    [Tooltip("Rectángulo del Canvas dentro del que rebota (vacío = el Canvas)")]
    public RectTransform zona;

    Vector2 dir;

    void Start()
    {
        if (!zona) { var c = GetComponentInParent<Canvas>(); if (c) zona = c.transform as RectTransform; }
        float ang = Random.Range(25f, 65f) * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Cos(ang) * (Random.value < 0.5f ? -1f : 1f), Mathf.Sin(ang) * (Random.value < 0.5f ? -1f : 1f));
    }

    void Update()
    {
        if (!zona) return;
        Vector3 p = transform.localPosition;
        p += (Vector3)(dir * velocidad * Time.unscaledDeltaTime);
        Vector2 media = zona.rect.size * 0.5f;
        Vector2 h = tamanoVisible * 0.5f;
        float minX = -media.x + h.x - centroVisible.x, maxX = media.x - h.x - centroVisible.x;
        float minY = -media.y + h.y - centroVisible.y, maxY = media.y - h.y - centroVisible.y;
        if (p.x < minX) { p.x = minX; dir.x = Mathf.Abs(dir.x); }
        if (p.x > maxX) { p.x = maxX; dir.x = -Mathf.Abs(dir.x); }
        if (p.y < minY) { p.y = minY; dir.y = Mathf.Abs(dir.y); }
        if (p.y > maxY) { p.y = maxY; dir.y = -Mathf.Abs(dir.y); }
        transform.localPosition = p;
    }
}
