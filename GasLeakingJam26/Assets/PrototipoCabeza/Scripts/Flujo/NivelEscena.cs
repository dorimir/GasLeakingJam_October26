using UnityEngine;

/// Va en cada escena de nivel (Nivel1..Nivel4): indica qué nivel es.
[DefaultExecutionOrder(-600)]
public class NivelEscena : MonoBehaviour
{
    [Range(1, 4)] public int nivel = 1;

    void Awake()
    {
        ResultadoPartida.nivel = nivel;
    }
}
