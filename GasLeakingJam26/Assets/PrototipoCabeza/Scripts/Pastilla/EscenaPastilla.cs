using UnityEngine;

/// Escena "Pastilla": el botón Download muestra el antivirus; el botón de comprar del antivirus
/// lleva a la cinemática de la pastilla.
public class EscenaPastilla : MonoBehaviour
{
    [Tooltip("Panel del antivirus (oculto al empezar)")]
    public GameObject antivirus;
    [Tooltip("Botón Download (se oculta al pulsarlo)")]
    public GameObject botonDownload;

    void Start()
    {
        if (antivirus) antivirus.SetActive(false);
    }

    /// Botón Download.
    public void Descargar()
    {
        if (antivirus) antivirus.SetActive(true);
        if (botonDownload) botonDownload.SetActive(false);
    }

    /// Botón Comprar del antivirus.
    public void Comprar()
    {
        FlujoPastilla.IrACinematica();
    }
}
