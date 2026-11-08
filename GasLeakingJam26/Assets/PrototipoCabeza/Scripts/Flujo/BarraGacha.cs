using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Barra "gacha" de Resultados.
/// - La zona roja mide el % de calvicie (= probabilidad de que la gente se dé cuenta) y se coloca
///   en una posición aleatoria de la barra. El resto es verde (no se dan cuenta).
/// - Un marcador recorre la barra de lado a lado, va frenando y se para al azar.
///   Si se para en rojo, te han pillado.
public class BarraGacha : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Zona roja (hija de la barra). Se ajusta con sus anclas X.")]
    public RectTransform zonaRoja;
    [Tooltip("Marcador (hijo de la barra). Se mueve con sus anclas X.")]
    public RectTransform marcador;
    public Text textoPorcentaje;
    public Text textoVeredicto;

    [Header("Animación")]
    public float retrasoInicial = 0.9f;     // espera a que termine el fundido
    public float duracion = 3.5f;
    [Tooltip("Cuántas idas y vueltas completas hace antes de parar")]
    public int vueltas = 3;

    [Header("Veredicto")]
    public string textoPillado = "¡TE HAN PILLADO!";
    public string textoLibre = "¡NO SE HAN DADO CUENTA!";
    public Color colorPillado = new Color(0.9f, 0.3f, 0.25f);
    public Color colorLibre = new Color(0.05f, 0.97f, 0.33f);

    public bool Terminado { get; private set; }

    [Header("Sprite de resultado")]
    [Tooltip("Imagen que aparece al pararse la barra (en su sitio ya colocado)")]
    public Image imagenResultado;
    public Sprite spriteGanaste;
    public Sprite spritePerdiste;
    public float duracionAparicion = 0.4f;

    IEnumerator MostrarResultado()
    {
        imagenResultado.sprite = Pillado ? spritePerdiste : spriteGanaste;
        var rt = imagenResultado.rectTransform;
        Vector3 escalaFinal = rt.localScale;
        Color c = imagenResultado.color; float alfa = c.a;
        imagenResultado.gameObject.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionAparicion);
            float k = Mathf.Clamp01(t);
            // crece con un pequeño rebote (sobrepasa y vuelve) y aparece con fundido
            float s = 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f);
            rt.localScale = escalaFinal * s;
            imagenResultado.color = new Color(c.r, c.g, c.b, alfa * Mathf.Clamp01(k * 2f));
            yield return null;
        }
        rt.localScale = escalaFinal;
        imagenResultado.color = new Color(c.r, c.g, c.b, alfa);
    }
    public bool Pillado { get; private set; }

    void Start()
    {
        float calvicie = Mathf.Clamp(ResultadoPartida.porcentajeCalvicie, 0f, 100f);
        if (textoPorcentaje) textoPorcentaje.text = $"{Mathf.RoundToInt(calvicie)}%";
        if (textoVeredicto) textoVeredicto.text = "";
        if (imagenResultado) imagenResultado.gameObject.SetActive(false);   // aparece al pararse la barra

        // Zona roja: ancho = probabilidad de que se note, en posición aleatoria
        float ancho = calvicie / 100f;
        float inicioRojo = Random.Range(0f, 1f - ancho);
        if (zonaRoja)
        {
            zonaRoja.anchorMin = new Vector2(inicioRojo, 0f);
            zonaRoja.anchorMax = new Vector2(inicioRojo + ancho, 1f);
            zonaRoja.offsetMin = zonaRoja.offsetMax = Vector2.zero;
        }

        // Resultado: punto al azar sobre la barra
        float destino = Random.value;
        Pillado = ancho > 0f && destino >= inicioRojo && destino <= inicioRojo + ancho;

        PonerMarcador(0f);
        StartCoroutine(Animar(destino));
    }

    IEnumerator Animar(float destino)
    {
        float espera = 0f;
        while (espera < retrasoInicial) { espera += Time.unscaledDeltaTime; yield return null; }

        // Recorre una distancia total (en "anchos de barra") que acaba justo en el destino.
        // PingPong con periodo 2: en 2n + x vale x.
        float distancia = 2f * Mathf.Max(0, vueltas) + destino;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracion);
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);   // frena al final
            PonerMarcador(Mathf.PingPong(distancia * k, 1f));
            yield return null;
        }
        PonerMarcador(destino);
        Terminado = true;
        if (imagenResultado) StartCoroutine(MostrarResultado());

        if (textoVeredicto)
        {
            textoVeredicto.text = Pillado ? textoPillado : textoLibre;
            textoVeredicto.color = Pillado ? colorPillado : colorLibre;
        }

        // pequeño "pop" del marcador
        float p = 0f;
        Vector3 escala = marcador ? marcador.localScale : Vector3.one;
        while (p < 1f && marcador)
        {
            p += Time.unscaledDeltaTime / 0.25f;
            marcador.localScale = escala * (1f + 0.4f * Mathf.Sin(Mathf.Clamp01(p) * Mathf.PI));
            yield return null;
        }
        if (marcador) marcador.localScale = escala;
    }

    void PonerMarcador(float x)
    {
        if (!marcador) return;
        marcador.anchorMin = new Vector2(x, marcador.anchorMin.y);
        marcador.anchorMax = new Vector2(x, marcador.anchorMax.y);
        marcador.anchoredPosition = new Vector2(0f, marcador.anchoredPosition.y);
    }
}
