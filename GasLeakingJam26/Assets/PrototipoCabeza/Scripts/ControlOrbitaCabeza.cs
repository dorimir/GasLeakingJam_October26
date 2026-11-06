using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// Rota la cámara orbital alrededor de la cabeza con clic derecho y hace zoom con la rueda.
/// RestaurarVista() lleva la cámara a la nuca (vista trasera).
[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class ControlOrbitaCabeza : MonoBehaviour
{
    [Header("Rotación (clic derecho)")]
    public float sensibilidadHorizontal = 0.3f;   // grados por píxel
    public float sensibilidadVertical = 0.25f;
    public bool invertirHorizontal = false;
    public bool invertirVertical = false;

    [Header("Zoom (rueda)")]
    public float sensibilidadZoom = 0.001f;

    [Header("Vista restaurada (nuca)")]
    public float anguloHorizontalNuca = 0f;
    public float anguloVerticalNuca = 25f;
    public float zoomNuca = 1f;
    public float duracionRestaurar = 0.6f;

    CinemachineOrbitalFollow orbital;
    Coroutine animacion;

    void Awake()
    {
        orbital = GetComponent<CinemachineOrbitalFollow>();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            if (d.sqrMagnitude > 0f)
            {
                DetenerAnimacion();
                float dx = (invertirHorizontal ? -d.x : d.x) * sensibilidadHorizontal;
                float dy = (invertirVertical ? d.y : -d.y) * sensibilidadVertical;

                var h = orbital.HorizontalAxis;
                h.Value = Envolver(h.Value + dx, h.Range);
                orbital.HorizontalAxis = h;

                var v = orbital.VerticalAxis;
                v.Value = Mathf.Clamp(v.Value + dy, v.Range.x, v.Range.y);
                orbital.VerticalAxis = v;
            }
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            DetenerAnimacion();
            var r = orbital.RadialAxis;
            r.Value = Mathf.Clamp(r.Value - scroll * sensibilidadZoom, r.Range.x, r.Range.y);
            orbital.RadialAxis = r;
        }
    }

    /// Llamado por el botón "Restaurar vista".
    public void RestaurarVista()
    {
        DetenerAnimacion();
        animacion = StartCoroutine(AnimarHacia(anguloHorizontalNuca, anguloVerticalNuca, zoomNuca));
    }

    IEnumerator AnimarHacia(float hFin, float vFin, float rFin)
    {
        float hIni = orbital.HorizontalAxis.Value;
        float vIni = orbital.VerticalAxis.Value;
        float rIni = orbital.RadialAxis.Value;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionRestaurar);
            float k = Mathf.SmoothStep(0f, 1f, t);

            var h = orbital.HorizontalAxis;
            h.Value = Envolver(Mathf.LerpAngle(hIni, hFin, k), h.Range);
            orbital.HorizontalAxis = h;

            var v = orbital.VerticalAxis;
            v.Value = Mathf.Lerp(vIni, vFin, k);
            orbital.VerticalAxis = v;

            var r = orbital.RadialAxis;
            r.Value = Mathf.Lerp(rIni, rFin, k);
            orbital.RadialAxis = r;

            yield return null;
        }
        animacion = null;
    }

    void DetenerAnimacion()
    {
        if (animacion != null) { StopCoroutine(animacion); animacion = null; }
    }

    static float Envolver(float valor, Vector2 rango)
    {
        float ancho = rango.y - rango.x;
        return Mathf.Repeat(valor - rango.x, ancho) + rango.x;
    }
}
