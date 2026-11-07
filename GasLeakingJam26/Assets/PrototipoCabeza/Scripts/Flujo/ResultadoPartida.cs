using UnityEngine;

/// Datos que viajan del prototipo a la escena de Resultados.
public static class ResultadoPartida
{
    public static float porcentajeCalvicie = 100f;
    public static int mechonesUsados;
    public static int mechonesTotales;
    /// Escena desde la que se llegó a Resultados ("Volver a probar" vuelve aquí).
    public static string escenaOrigen;
    /// Busto (con sus mechones) que se lleva a Resultados con DontDestroyOnLoad.
    public static GameObject busto;
}
