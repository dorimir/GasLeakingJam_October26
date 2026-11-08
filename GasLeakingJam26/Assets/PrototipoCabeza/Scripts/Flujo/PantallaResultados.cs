using UnityEngine;
using UnityEngine.UI;

/// UI de Resultados / cinemática.
/// - Continuar(): tras la barra gacha, si NO te pillan pasas al siguiente nivel;
///   si te pillan (o era el nivel 4) saltas al nivel final.
/// - VolverAEmpezar(): vuelve al nivel 1 (para la cinemática final).
public class PantallaResultados : MonoBehaviour
{
    public Text textoPorcentaje;
    public Text textoValoracion;
    public Text textoMechones;
    public string escenaJuego = "PrototipoCabeza";

    [Header("Continuar (Resultados)")]
    public BarraGacha gacha;
    [Tooltip("Botón que aparece cuando la barra gacha termina")]
    public Button botonContinuar;
    public Text textoBotonContinuar;
    public string textoSiguienteNivel = "Siguiente nivel";
    public string textoNivelFinal = "Nivel final";

    bool botonMostrado;

    void Start()
    {
        float p = ResultadoPartida.porcentajeCalvicie;
        if (textoPorcentaje) textoPorcentaje.text = $"Calvicie: {Mathf.RoundToInt(p)}%";
        if (textoValoracion) textoValoracion.text = Valoracion(p);
        if (textoMechones) textoMechones.text = $"Mechones usados: {ResultadoPartida.mechonesUsados}/{ResultadoPartida.mechonesTotales}";
        if (botonContinuar && gacha) botonContinuar.gameObject.SetActive(false);
        if (ResultadoPartida.busto == null)
            Debug.LogWarning("[Resultados] No hay busto: entra en esta escena desde un nivel.");
    }

    void Update()
    {
        if (botonMostrado || !botonContinuar || !gacha || !gacha.Terminado) return;
        botonMostrado = true;
        bool alFinal = gacha.Pillado || ResultadoPartida.nivel >= ResultadoPartida.nivelesNormales;
        if (textoBotonContinuar) textoBotonContinuar.text = alFinal ? textoNivelFinal : textoSiguienteNivel;
        botonContinuar.gameObject.SetActive(true);
    }

    /// Botón de Resultados: siguiente nivel si no te pillaron; si te pillaron, nivel final.
    public void Continuar()
    {
        bool pillado = gacha != null && gacha.Pillado;
        string destino;
        if (pillado || ResultadoPartida.nivel >= ResultadoPartida.nivelesNormales)
        {
            ResultadoPartida.nivel = ResultadoPartida.nivelesNormales + 1;
            destino = ResultadoPartida.escenaFinal;
        }
        else
        {
            ResultadoPartida.nivel++;
            destino = ResultadoPartida.EscenaNivel(ResultadoPartida.nivel);
        }
        Ir(destino);
    }

    /// Repite el mismo nivel.
    public void VolverAProbar()
    {
        string destino = string.IsNullOrEmpty(ResultadoPartida.escenaOrigen) ? escenaJuego : ResultadoPartida.escenaOrigen;
        Ir(destino);
    }

    /// Empieza otra vez desde el nivel 1 (botón de la cinemática final).
    public void VolverAEmpezar()
    {
        ResultadoPartida.nivel = 1;
        Ir(ResultadoPartida.EscenaNivel(1));
    }

    void Ir(string escena)
    {
        ResultadoPartida.flujoIniciado = true;
        Transicion.Ir(escena, () =>
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
