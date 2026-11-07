using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Sonido de hover y de clic para botones de UI.
/// Ponlo en cada botón (o en un prefab de botón). No hace falta tocar MainMenu.
/// Usa el EventSystem, así que funciona con el nuevo Input System.
/// </summary>
public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
{
    [Header("Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Volumen")]
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    [Header("Salida de audio (opcional)")]
    [Tooltip("AudioSource compartido (por ejemplo, uno en un objeto que nunca se desactive). " +
             "Si se deja vacío, el sonido se reproduce con un AudioSource temporal en la posición de la cámara.")]
    [SerializeField] private AudioSource sharedAudioSource;

    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    // Ratón encima del botón
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanPlay()) return;
        Play(hoverClip, hoverVolume);
    }

    // Clic con el ratón o tap
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanPlay()) return;
        Play(clickClip, clickVolume);
    }

    // Teclado / mando (Enter, botón A...)
    public void OnSubmit(BaseEventData eventData)
    {
        if (!CanPlay()) return;
        Play(clickClip, clickVolume);
    }

    private bool CanPlay()
    {
        // No suena si el botón está desactivado (Interactable = false)
        return _selectable == null || _selectable.IsInteractable();
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;

        if (sharedAudioSource != null)
        {
            sharedAudioSource.PlayOneShot(clip, volume);
        }
        else
        {
            // Se reproduce aunque el botón se desactive justo después de pulsarlo
            // (por ejemplo, el botón de volver de los créditos).
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, pos, volume);
        }
    }
}
