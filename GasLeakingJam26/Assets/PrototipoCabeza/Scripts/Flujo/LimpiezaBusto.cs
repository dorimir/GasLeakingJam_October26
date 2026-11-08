using UnityEngine;
using UnityEngine.SceneManagement;

/// El busto (gato + mechones) viaja con DontDestroyOnLoad a Resultados y a la cinemática final.
/// Al cargar cualquier otra escena (menú, niveles, créditos...) se borra, para que no se quede en pantalla.
public static class LimpiezaBusto
{
    static readonly string[] escenasConBusto = { "Resultados", "CinematicaFinal" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Additive) return;
        foreach (var n in escenasConBusto) if (escena.name == n) return;
        if (ResultadoPartida.busto)
        {
            Object.Destroy(ResultadoPartida.busto);
            ResultadoPartida.busto = null;
        }
    }
}
