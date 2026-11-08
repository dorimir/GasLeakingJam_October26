using UnityEngine;

/// Flujo de la pastilla:
///   nivel N -> (botón Pastillas) -> escena "Pastilla" -> (comprar) -> "CinemáticaPastilla"
///   -> salta un nivel: N+2 (si se pasa del último nivel normal, al nivel final).
public static class FlujoPastilla
{
    public const string escenaPastilla = "Pastilla";
    public const string escenaCinematica = "CinemáticaPastilla";
    public const float fundido = 0.9f;        // fundidos suaves de este flujo

    /// Nivel desde el que se tomó la pastilla.
    public static int nivelOrigen = 1;

    public static void TomarPastilla()
    {
        if (Transicion.Ocupado) return;
        nivelOrigen = Mathf.Max(1, ResultadoPartida.nivel);
        ResultadoPartida.flujoIniciado = true;
        Transicion.Ir(escenaPastilla, null, fundido);
    }

    public static void IrACinematica()
    {
        if (Transicion.Ocupado) return;
        Transicion.Ir(escenaCinematica, null, fundido);
    }

    /// Tras la cinemática: se salta un nivel hacia delante.
    public static void IrAlSiguienteNivel()
    {
        if (Transicion.Ocupado) return;
        int destino = nivelOrigen + 2;
        string escena;
        if (destino > ResultadoPartida.nivelesNormales)
        {
            ResultadoPartida.nivel = ResultadoPartida.nivelesNormales + 1;
            escena = ResultadoPartida.escenaFinal;
        }
        else
        {
            ResultadoPartida.nivel = destino;
            escena = ResultadoPartida.EscenaNivel(destino);
        }
        ResultadoPartida.flujoIniciado = true;
        Transicion.Ir(escena, null, 1.2f);
    }
}
