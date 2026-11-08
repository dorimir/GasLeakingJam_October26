using UnityEngine;

/// Datos que viajan entre escenas (resultado de la partida y progreso de niveles).
public static class ResultadoPartida
{
    public static float porcentajeCalvicie = 100f;
    public static int mechonesUsados;
    public static int mechonesTotales;
    /// Escena desde la que se llegó a Resultados ("Volver a probar" vuelve aquí).
    public static string escenaOrigen;
    /// Busto (con sus mechones) que se lleva a Resultados con DontDestroyOnLoad.
    public static GameObject busto;

    // ---------- Niveles ----------
    /// Niveles 1..5 se juegan en sus escenas (Nivel1..Nivel5); después va el nivel final (afeitado).
    public const int nivelesNormales = 5;
    public const string escenaNiveles = "PrototipoCabeza";   // escena de pruebas (todas las etapas)
    /// Cada nivel 1..5 tiene su propia escena: Nivel1 … Nivel5.
    public static string EscenaNivel(int n) { return "Nivel" + n; }
    public const string escenaFinal = "NivelFinal";
    /// Nivel que se está jugando (1..5).
    public static int nivel = 1;
    /// true en cuanto se pasa de nivel por el flujo del juego (para que el 'nivel de prueba' del editor no moleste).
    public static bool flujoIniciado;
}
