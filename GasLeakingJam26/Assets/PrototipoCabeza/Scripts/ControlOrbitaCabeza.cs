using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// Cámara orbital propia (va en la Main Camera): rota alrededor del objetivo con clic derecho,
/// zoom limitado con la rueda y RestaurarVista() la lleva a la nuca.
[DefaultExecutionOrder(-100)]
public class ControlOrbitaCabeza : MonoBehaviour
{
    public Transform objetivo;

    [Header("Rotación (clic derecho)")]
    public float sensibilidadHorizontal = 0.3f;   // grados por píxel
    public float sensibilidadVertical = 0.25f;
    public bool invertirHorizontal = false;
    public bool invertirVertical = false;
    public float anguloVerticalMin = -15f;
    public float anguloVerticalMax = 80f;

    [Header("Zoom (rueda)")]
    public float distancia = 4.2f;
    public float distanciaMin = 2.5f;
    public float distanciaMax = 6.3f;
    public float sensibilidadZoom = 0.004f;

    [Header("Vista inicial")]
    public float anguloHorizontalInicial = 180f;   // 180 = de frente, 0 = nuca
    public float anguloVerticalInicial = 10f;

    [Header("Vista restaurada (nuca)")]
    public float anguloHorizontalNuca = 0f;
    public float anguloVerticalNuca = 25f;
    public float distanciaNuca = 4.2f;
    public float duracionRestaurar = 0.6f;

    float yaw, pitch;
    Coroutine animacion;

    void Start()
    {
        yaw = anguloHorizontalInicial;
        pitch = anguloVerticalInicial;
        Aplicar();
    }

    void LateUpdate()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                if (d.sqrMagnitude > 0f)
                {
                    DetenerAnimacion();
                    yaw += (invertirHorizontal ? -d.x : d.x) * sensibilidadHorizontal;
                    pitch += (invertirVertical ? d.y : -d.y) * sensibilidadVertical;
                    pitch = Mathf.Clamp(pitch, anguloVerticalMin, anguloVerticalMax);
                    yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;
                }
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                DetenerAnimacion();
                distancia = Mathf.Clamp(distancia - scroll * sensibilidadZoom, distanciaMin, distanciaMax);
            }
        }
        Aplicar();
    }

    void Aplicar()
    {
        if (objetivo == null) return;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(objetivo.position + rot * new Vector3(0f, 0f, -distancia), rot);
    }

    /// Llamado por el botón "Restaurar vista".
    public void RestaurarVista()
    {
        DetenerAnimacion();
        animacion = StartCoroutine(AnimarHacia(anguloHorizontalNuca, anguloVerticalNuca, distanciaNuca));
    }

    IEnumerator AnimarHacia(float yawFin, float pitchFin, float distFin)
    {
        float yawIni = yaw, pitchIni = pitch, distIni = distancia;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionRestaurar);
            float k = Mathf.SmoothStep(0f, 1f, t);
            yaw = Mathf.LerpAngle(yawIni, yawFin, k);
            pitch = Mathf.Lerp(pitchIni, pitchFin, k);
            distancia = Mathf.Lerp(distIni, distFin, k);
            yield return null;
        }
        animacion = null;
    }

    void DetenerAnimacion()
    {
        if (animacion != null) { StopCoroutine(animacion); animacion = null; }
    }
}
