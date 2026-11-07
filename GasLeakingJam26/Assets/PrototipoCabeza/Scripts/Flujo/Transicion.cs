using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Fundido a negro entre escenas. Se crea solo la primera vez que se usa.
/// Uso: Transicion.Ir("Resultados", () => { /* se ejecuta con la pantalla en negro */ });
public class Transicion : MonoBehaviour
{
    public static float duracion = 0.6f;
    public static Color color = Color.black;

    static Transicion instancia;
    Image cortina;
    bool ocupado;

    public static bool Ocupado => instancia != null && instancia.ocupado;

    public static void Ir(string escena, Action antesDeCargar = null)
    {
        Asegurar();
        if (instancia.ocupado) return;
        instancia.StartCoroutine(instancia.Rutina(escena, antesDeCargar));
    }

    static void Asegurar()
    {
        if (instancia != null) return;
        var go = new GameObject("Transicion");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<Transicion>();
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        go.AddComponent<GraphicRaycaster>();

        var img = new GameObject("Cortina", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(go.transform, false);
        var rt = (RectTransform)img.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        instancia.cortina = img.GetComponent<Image>();
        instancia.cortina.color = new Color(color.r, color.g, color.b, 0f);
        instancia.cortina.raycastTarget = false;
    }

    IEnumerator Rutina(string escena, Action antesDeCargar)
    {
        ocupado = true;
        cortina.raycastTarget = true;           // bloquea clics durante la transición
        yield return Fundido(0f, 1f);

        antesDeCargar?.Invoke();
        var op = SceneManager.LoadSceneAsync(escena);
        while (!op.isDone) yield return null;
        yield return null;                      // un frame para que la escena nueva se coloque

        yield return Fundido(1f, 0f);
        cortina.raycastTarget = false;
        ocupado = false;
    }

    IEnumerator Fundido(float desde, float hasta)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracion);
            float a = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
            cortina.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }
}
