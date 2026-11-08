using UnityEngine;
using UnityEngine.UI;

/// Configura la escena de niveles (1..4) según ResultadoPartida.nivel:
/// activa el peinado de esa etapa y pone el sprite de su botón "Listo".
/// Se ejecuta antes que el resto para que el % de calvicie se calcule ya con el peinado correcto.
[DefaultExecutionOrder(-500)]
public class ConfiguradorNivel : MonoBehaviour
{
    [System.Serializable]
    public class Nivel
    {
        public string nombre;
        public GameObject peinado;
        public Sprite spriteListo;
    }

    public Nivel[] niveles = new Nivel[4];
    public Image imagenBotonListo;
    [Tooltip("Solo editor: si das a Play directamente en esta escena, empieza en este nivel (1..4). 0 = el que toque.")]
    public int nivelDePrueba = 0;

    public int NivelActual { get; private set; }

    void Awake()
    {
        if (Application.isEditor && !ResultadoPartida.flujoIniciado && nivelDePrueba > 0)
            ResultadoPartida.nivel = nivelDePrueba;

        int idx = Mathf.Clamp(ResultadoPartida.nivel - 1, 0, niveles.Length - 1);
        ResultadoPartida.nivel = idx + 1;
        NivelActual = idx + 1;

        for (int i = 0; i < niveles.Length; i++)
            if (niveles[i] != null && niveles[i].peinado) niveles[i].peinado.SetActive(i == idx);

        if (imagenBotonListo && niveles[idx] != null && niveles[idx].spriteListo)
            imagenBotonListo.sprite = niveles[idx].spriteListo;
    }
}
