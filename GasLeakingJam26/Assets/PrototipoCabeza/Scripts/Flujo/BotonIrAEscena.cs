using UnityEngine;

/// Botón genérico: va a otra escena con el fundido de Transicion.
public class BotonIrAEscena : MonoBehaviour
{
    public string escena = "NivelFinal";

    public void Ir()
    {
        if (!string.IsNullOrEmpty(escena)) Transicion.Ir(escena);
    }
}
