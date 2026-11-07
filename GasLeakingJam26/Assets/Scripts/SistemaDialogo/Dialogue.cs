using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Sistema de diálogo con efecto de escritura, que se puede lanzar desde cualquier evento del juego.
/// El panel NO se desactiva con SetActive: se oculta con un CanvasGroup para que el script
/// siga activo y pueda volver a mostrarse cuando se le llame.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Dialogue : MonoBehaviour, IPointerClickHandler
{
    [Header("Referencias")]
    public TextMeshProUGUI textComponent;

    [Header("Contenido")]
    [TextArea(2, 5)] public string[] lines;
    public float textSpeed = 0.03f;

    [Header("Opciones")]
    [Tooltip("Si está marcado, el diálogo arranca solo al empezar la escena.")]
    public bool playOnStart = false;

    [Header("Eventos")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;

    public bool IsActive { get; private set; }

    private CanvasGroup canvasGroup;
    private int index;
    private bool isTyping;
    private Coroutine typingCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    void Start()
    {
        if (playOnStart)
            StartDialogue();
    }

    // ---------- API pública: llámala desde botones, triggers, objetos, otros scripts... ----------

    /// <summary>Inicia el diálogo con las líneas que ya están en el Inspector.
    /// Se puede enlazar directamente al OnClick de un Button.</summary>
    public void StartDialogue()
    {
        if (IsActive || lines == null || lines.Length == 0) return;

        IsActive = true;
        index = 0;
        SetVisible(true);
        onDialogueStart?.Invoke();
        typingCoroutine = StartCoroutine(TypeLine());
    }

    /// <summary>Inicia el diálogo con unas líneas distintas (por ejemplo, según el objeto usado).</summary>
    public void StartDialogue(string[] newLines)
    {
        if (IsActive) return;
        lines = newLines;
        StartDialogue();
    }

    /// <summary>Avanza el diálogo. Se llama al hacer clic en el panel,
    /// pero también se puede enlazar a un botón.</summary>
    public void Advance()
    {
        if (!IsActive) return;

        if (isTyping)
            CompleteLine();
        else
            NextLine();
    }

    /// <summary>Cierra el diálogo inmediatamente.</summary>
    public void EndDialogue()
    {
        if (!IsActive) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        IsActive = false;
        textComponent.text = string.Empty;
        SetVisible(false);
        onDialogueEnd?.Invoke();
    }

    // ---------- Interno ----------

    // Clic/tap sobre el panel (o sus hijos) gracias al EventSystem de la UI.
    public void OnPointerClick(PointerEventData eventData)
    {
        Advance();
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;

        foreach (char c in lines[index])
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    void CompleteLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        textComponent.text = lines[index];
        isTyping = false;
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            typingCoroutine = StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}