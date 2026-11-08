using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// Cinemática de la pastilla: un acercamiento lento de las imágenes durante unos segundos
/// (clic para saltar) y después pasa al nivel siguiente saltándose uno.
public class CinematicaPastilla : MonoBehaviour
{
    [Tooltip("Imágenes que hacen el acercamiento lento")]
    public RectTransform[] imagenes;
    public float duracion = 4f;
    [Tooltip("Cuánto se acercan las imágenes al final (1.08 = 8%)")]
    public float zoomFinal = 1.08f;
    public bool clicParaSaltar = true;

    Vector3[] escalas;
    bool saliendo;

    void Start()
    {
        escalas = new Vector3[imagenes.Length];
        for (int i = 0; i < imagenes.Length; i++) if (imagenes[i]) escalas[i] = imagenes[i].localScale;
        StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        float t = 0f;
        yield return null;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duracion);
            for (int i = 0; i < imagenes.Length; i++)
                if (imagenes[i]) imagenes[i].localScale = escalas[i] * Mathf.Lerp(1f, zoomFinal, k);
            if (clicParaSaltar && t > 0.5f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break;
            yield return null;
        }
        Salir();
    }

    void Salir()
    {
        if (saliendo) return;
        saliendo = true;
        FlujoPastilla.IrAlSiguienteNivel();
    }
}
