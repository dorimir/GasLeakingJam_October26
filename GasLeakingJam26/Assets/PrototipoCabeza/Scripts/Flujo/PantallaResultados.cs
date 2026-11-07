using UnityEngine;
using UnityEngine.UI;

/// UI de la escena de Resultados y botón "Volver a probar".
public class PantallaResultados : MonoBehaviour
{
    public Text textoPorcentaje;
    public Text textoValoracion;
    public Text textoMechones;
    public string escenaJuego = "PrototipoCabeza";

    void Start()
    {
        float p = ResultadoPartida.porcentajeCalvicie;
        if (textoPorcentaje) textoPorcentaje.text = $"Calvicie: {Mathf.RoundToInt(p)}%";
        if (textoValoracion) textoValoracion.text = Valoracion(p);
        if (textoMechones) textoMechones.text = $"Mechones usados: {ResultadoPartida.mechonesUsados}/{ResultadoPartida.mechonesTotales}";
        if (ResultadoPartida.busto == null)
            Debug.LogWarning("[Resultados] No hay busto: entra en esta escena desde PrototipoCabeza.");
    }

    /// Llamado por el botón "Volver a probar".
    public void VolverAProbar()
    {
        Transicion.Ir(escenaJuego, () =>
        {
            if (ResultadoPartida.busto) Destroy(ResultadoPartida.busto);
            ResultadoPartida.busto = null;
        });
    }

    /// Cambia aquí los textos de valoración.
    public static string Valoracion(float porcentaje)
    {
        if (porcentaje <= 5f) return "¡Melena de anuncio!";
        if (porcentaje <= 20f) return "Ni se nota. Bien disimulado.";
        if (porcentaje <= 45f) return "Desde lejos cuela...";
        if (porcentaje <= 75f) return "Eso se ve desde el avión.";
        return "Calvo con dignidad.";
    }
}
