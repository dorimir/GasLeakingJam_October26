using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Escena y creditos")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private Image creditsImage;
    [SerializeField] private Image backButton;

    [Header("Musica")]
    [Tooltip("AudioSource con la musica del menu. Play On Awake desactivado.")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] private float fadeInTime = 1f;
    [Tooltip("Fade out de la musica antes de jugar o salir.")]
    [SerializeField] private float fadeOutTime = 0.8f;

    private bool _leaving;
    private Coroutine _fadeInRoutine;

    private void Start()
    {
        // El SplashScreen deja el cursor bloqueado; aqui se libera
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            _fadeInRoutine = StartCoroutine(FadeInMusic());
        }
    }

    // La musica NO se toca aqui: sigue sonando mientras se ven los creditos
    public void ShowCredits()
    {
        creditsImage.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
    }

    // Tampoco se toca al volver de los creditos
    public void HideCredits()
    {
        creditsImage.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
    }

    // Jugar: apaga la musica con fade out y luego carga la escena
    public void PlayGame()
    {
        if (_leaving) return;
        StartCoroutine(LeaveRoutine(() => SceneManager.LoadScene(sceneToLoad)));
    }

    // Salir: apaga la musica con fade out y luego cierra el juego
    public void QuitGame()
    {
        if (_leaving) return;
        StartCoroutine(LeaveRoutine(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }));
    }

    private IEnumerator LeaveRoutine(Action onDone)
    {
        _leaving = true;

        if (_fadeInRoutine != null) StopCoroutine(_fadeInRoutine);
        yield return FadeOutMusic();

        onDone?.Invoke();
    }

    private IEnumerator FadeInMusic()
    {
        musicSource.volume = fadeInTime > 0f ? 0f : musicVolume;
        musicSource.Play();

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, Mathf.Clamp01(t / fadeInTime));
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    private IEnumerator FadeOutMusic()
    {
        if (musicSource == null || !musicSource.isPlaying) yield break;

        float startVolume = musicSource.volume;
        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(t / fadeOutTime));
            yield return null;
        }

        musicSource.Stop();
    }
}