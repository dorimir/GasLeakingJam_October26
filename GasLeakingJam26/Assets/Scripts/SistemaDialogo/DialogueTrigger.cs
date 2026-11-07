using UnityEngine;

/// <summary>
/// Lanza un diálogo desde un evento del juego. Ponlo en cualquier objeto.
/// - Botón:        enlaza el OnClick del botón a DialogueTrigger.Trigger()
/// - Escena:       marca "Trigger On Scene Start"
/// - Objeto/zona:  marca "Trigger On Player Enter" (el objeto necesita un Collider con Is Trigger)
/// - Usar objeto:  llama a Trigger() desde el script del objeto o desde un UnityEvent
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("Diálogo")]
    public Dialogue dialogue;

    [Tooltip("Si se marca, usa estas líneas en lugar de las del Dialogue.")]
    public bool useCustomLines = false;
    [TextArea(2, 5)] public string[] customLines;

    [Header("Cuándo se lanza")]
    public bool triggerOnSceneStart = false;
    public bool triggerOnPlayerEnter = false;
    public string playerTag = "Player";

    [Header("Opciones")]
    [Tooltip("Si está marcado, solo se puede lanzar una vez.")]
    public bool playOnce = true;

    private bool alreadyPlayed;

    void Start()
    {
        if (triggerOnSceneStart)
            Trigger();
    }

    /// <summary>Lanza el diálogo. Enlázalo al OnClick de un botón o llámalo desde otros scripts.</summary>
    public void Trigger()
    {
        if (dialogue == null) return;
        if (playOnce && alreadyPlayed) return;
        if (dialogue.IsActive) return;

        alreadyPlayed = true;

        if (useCustomLines && customLines != null && customLines.Length > 0)
            dialogue.StartDialogue(customLines);
        else
            dialogue.StartDialogue();
    }

    // Juegos 3D
    void OnTriggerEnter(Collider other)
    {
        if (triggerOnPlayerEnter && other.CompareTag(playerTag))
            Trigger();
    }

    // Juegos 2D
    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnPlayerEnter && other.CompareTag(playerTag))
            Trigger();
    }
}
