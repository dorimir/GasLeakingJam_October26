using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CinematicSlide
{
    [Tooltip("Imagen que se muestra en esta diapositiva.")]
    public Sprite image;

    [Tooltip("Textos que se escriben uno tras otro debajo de la imagen. Clic para pasar al siguiente.")]
    [TextArea(2, 5)] public string[] texts;
}

/// <summary>
/// Escena de introducción / lore:
///   1) Muestra una serie de imágenes, cada una con varios textos que se escriben debajo.
///   2) Al terminar la última, reproduce una animación final (el GIF convertido en frames).
///   3) Carga la siguiente escena (el juego).
///
/// Ponlo en un panel que ocupe toda la pantalla con un Image (con Raycast Target activado).
/// Usa el EventSystem de la UI, así que funciona con el nuevo Input System.
/// </summary>
public class CinematicSequence : MonoBehaviour, IPointerClickHandler
{
    [Header("Referencias UI")]
    [Tooltip("Image donde se muestran las imágenes y la animación final.")]
    [SerializeField] private Image imageDisplay;
    [SerializeField] private TextMeshProUGUI textComponent;
    [Tooltip("Opcional: Image negra a pantalla completa para fundir a negro antes de cambiar de escena.")]
    [SerializeField] private Image fadeOverlay;

    [Header("Secuencia")]
    [SerializeField] private CinematicSlide[] slides;
    [SerializeField] private float textSpeed = 0.03f;
    [Tooltip("Duración del fundido entre imágenes (segundos). 0 = corte directo.")]
    [SerializeField] private float imageFadeTime = 0.4f;

    [Header("Animación final (el GIF)")]
    [Tooltip("Frames del GIF, en orden. Unity no importa GIFs: conviértelo a frames/sprite sheet.")]
    [SerializeField] private Sprite[] finalFrames;
    [SerializeField] private float finalFps = 12f;
    [Tooltip("Escala de la animación final. 1 = tamaño normal, 0.5 = la mitad, 0.75 = un 75%.")]
    [SerializeField, Range(0.1f, 2f)] private float finalScale = 0.7f;
    [Tooltip("Cuántas veces se reproduce la animación.")]
    [SerializeField] private int finalLoops = 1;
    [Tooltip("Segundos que se queda el último frame antes de pasar a la escena.")]
    [SerializeField] private float finalHoldTime = 0.5f;
    [Tooltip("Permite saltar la animación final con un clic.")]
    [SerializeField] private bool allowSkipFinal = true;

    [Header("Siguiente escena")]
    [Tooltip("Nombre exacto de la escena del juego (debe estar en Build Settings).")]
    [SerializeField] private string nextScene;
    [SerializeField] private float fadeToBlackTime = 0.6f;

    private bool _clicked;
    private bool _leaving;

    private void Start()
    {
        textComponent.text = string.Empty;

        if (fadeOverlay != null)
            SetOverlayAlpha(0f);

        StartCoroutine(RunSequence());
    }

    // Clic/tap en el panel (o en sus hijos).
    public void OnPointerClick(PointerEventData eventData)
    {
        _clicked = true;
    }

    /// <summary>Salta toda la cinemática y va directo a la siguiente escena (enlázalo a un botón "Saltar").</summary>
    public void Skip()
    {
        if (_leaving) return;
        _leaving = true;
        StopAllCoroutines();
        SceneManager.LoadScene(nextScene);
    }

    // ------------------------------------------------------------------

    private IEnumerator RunSequence()
    {
        // Imágenes con sus textos
        for (int s = 0; s < slides.Length; s++)
        {
            CinematicSlide slide = slides[s];

            textComponent.text = string.Empty;
            yield return ChangeImage(slide.image, s == 0);

            if (slide.texts != null)
            {
                foreach (string t in slide.texts)
                {
                    yield return TypeText(t);
                    yield return WaitForClick();
                }
            }
        }

        // Animación final
        textComponent.text = string.Empty;
        yield return PlayFinalAnimation();

        // Cambio de escena
        yield return LeaveToNextScene();
    }

    private IEnumerator ChangeImage(Sprite sprite, bool first)
    {
        // Fundido de salida de la imagen anterior (no en la primera)
        if (!first && imageFadeTime > 0f)
            yield return FadeImage(1f, 0f, imageFadeTime);

        imageDisplay.sprite = sprite;
        imageDisplay.preserveAspect = true;

        if (imageFadeTime > 0f)
            yield return FadeImage(0f, 1f, imageFadeTime);
        else
            SetImageAlpha(1f);
    }

    private IEnumerator TypeText(string text)
    {
        textComponent.text = text;
        textComponent.maxVisibleCharacters = 0;
        textComponent.ForceMeshUpdate();

        int total = textComponent.textInfo.characterCount;
        _clicked = false;

        for (int i = 0; i <= total; i++)
        {
            textComponent.maxVisibleCharacters = i;

            // Un clic mientras escribe completa el texto al instante.
            if (_clicked) break;

            yield return new WaitForSecondsRealtime(textSpeed);
        }

        textComponent.maxVisibleCharacters = int.MaxValue;
        _clicked = false; // el clic ya se ha usado para completar el texto
    }

    private IEnumerator WaitForClick()
    {
        _clicked = false;
        while (!_clicked)
            yield return null;
        _clicked = false;
    }

    private IEnumerator PlayFinalAnimation()
    {
        if (finalFrames == null || finalFrames.Length == 0)
            yield break;

        if (imageFadeTime > 0f)
            yield return FadeImage(1f, 0f, imageFadeTime);

        // Se aplica con la imagen ya invisible, para que no se vea el salto de tamaño.
        imageDisplay.rectTransform.localScale = Vector3.one * finalScale;
        imageDisplay.sprite = finalFrames[0];
        imageDisplay.preserveAspect = true;

        if (imageFadeTime > 0f)
            yield return FadeImage(0f, 1f, imageFadeTime);
        else
            SetImageAlpha(1f);

        float frameTime = 1f / Mathf.Max(1f, finalFps);
        _clicked = false;

        for (int loop = 0; loop < Mathf.Max(1, finalLoops); loop++)
        {
            for (int f = 0; f < finalFrames.Length; f++)
            {
                imageDisplay.sprite = finalFrames[f];
                yield return new WaitForSecondsRealtime(frameTime);

                if (allowSkipFinal && _clicked)
                {
                    _clicked = false;
                    yield break;
                }
            }
        }

        if (finalHoldTime > 0f)
            yield return new WaitForSecondsRealtime(finalHoldTime);
    }

    private IEnumerator LeaveToNextScene()
    {
        if (_leaving) yield break;
        _leaving = true;

        if (fadeOverlay != null && fadeToBlackTime > 0f)
        {
            float t = 0f;
            while (t < fadeToBlackTime)
            {
                t += Time.unscaledDeltaTime;
                SetOverlayAlpha(Mathf.Clamp01(t / fadeToBlackTime));
                yield return null;
            }
            SetOverlayAlpha(1f);
        }

        SceneManager.LoadScene(nextScene);
    }

    // ---------- Utilidades de fundido ----------

    private IEnumerator FadeImage(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetImageAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetImageAlpha(to);
    }

    private void SetImageAlpha(float a)
    {
        Color c = imageDisplay.color;
        c.a = a;
        imageDisplay.color = c;
    }

    private void SetOverlayAlpha(float a)
    {
        Color c = fadeOverlay.color;
        c.a = a;
        fadeOverlay.color = c;
    }
}