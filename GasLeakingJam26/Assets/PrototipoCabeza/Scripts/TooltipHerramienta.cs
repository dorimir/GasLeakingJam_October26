using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// Al pasar el ratón por encima de una herramienta muestra su tooltip (caja con imagen) al lado:
/// aparece con un fundido suave y desaparece de golpe al salir.
public class TooltipHerramienta : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Caja del tooltip (con CanvasGroup). Pon dentro tu imagen.")]
    public CanvasGroup tooltip;
    public float duracionAparicion = 0.25f;

    Coroutine fundido;

    void Awake() { Ocultar(); }
    void OnDisable() { Ocultar(); }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!tooltip) return;
        if (fundido != null) StopCoroutine(fundido);
        tooltip.gameObject.SetActive(true);
        tooltip.alpha = 0f;
        fundido = StartCoroutine(Aparecer());
    }

    public void OnPointerExit(PointerEventData eventData) { Ocultar(); }

    IEnumerator Aparecer()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionAparicion);
            tooltip.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        tooltip.alpha = 1f;
        fundido = null;
    }

    void Ocultar()
    {
        if (fundido != null) { StopCoroutine(fundido); fundido = null; }
        if (!tooltip) return;
        tooltip.alpha = 0f;                       // corte seco
        tooltip.blocksRaycasts = false;
        tooltip.interactable = false;
        tooltip.gameObject.SetActive(false);
    }
}
