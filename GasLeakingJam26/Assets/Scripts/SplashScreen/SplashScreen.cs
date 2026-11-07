using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Calvo
{
    /// <summary>
    /// Splash screen: reproduce una animacion + musica, muestra el titulo de la empresa
    /// con fade in, espera a que termine y carga la siguiente escena.
    /// El cursor permanece oculto y bloqueado, y no se puede saltar.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        [Header("Animacion")]
        [Tooltip("Animator que reproduce la animacion del splash (normalmente el del Logo).")]
        [SerializeField] private Animator animator;
        [Tooltip("Nombre del estado a reproducir. Vacio = estado por defecto del Animator.")]
        [SerializeField] private string stateName = "";
        [SerializeField] private int layer = 0;
        [Tooltip("Pausa extra cuando termina la animacion, antes de cambiar de escena.")]
        [SerializeField] private float extraWaitAfterEnd = 0.5f;
        [Tooltip("Seguro: si la animacion no termina en este tiempo, se continua igualmente.")]
        [SerializeField] private float maxWaitTime = 20f;

        [Header("Titulo (Gas Leaking)")]
        [Tooltip("CanvasGroup del objeto con el texto del titulo. Opcional.")]
        [SerializeField] private CanvasGroup titleGroup;
        [Tooltip("Segundos desde que empieza el splash hasta que empieza a aparecer el titulo.")]
        [SerializeField] private float titleDelay = 1f;
        [SerializeField] private float titleFadeTime = 1f;

        [Header("Musica")]
        [Tooltip("AudioSource con el clip del splash. Play On Awake desactivado. Opcional.")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Segundos desde que empieza el splash hasta que suena la musica.")]
        [SerializeField] private float musicDelay = 0f;
        [Tooltip("Si esta activo, no se cambia de escena hasta que termine el audio.")]
        [SerializeField] private bool waitForAudio = true;

        [Header("Escena")]
        [SerializeField] private string nextScene = "MainMenu";

        [Header("Opciones")]
        [SerializeField] private bool clearPrefsInEditor = false;

        private bool _animationDone;
        private bool _splashActive;

        private void Awake()
        {
            HideCursor();
            _splashActive = true;

            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);

            if (animator == null)
            {
                Debug.LogError("SplashScreen: no encuentro ningun Animator. Asignalo en el Inspector.", this);
                enabled = false;
                return;
            }

            if (titleGroup != null) titleGroup.alpha = 0f;
            if (audioSource != null) audioSource.playOnAwake = false;
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (clearPrefsInEditor) PlayerPrefs.DeleteAll();
#endif
            HideCursor();

            if (!Application.CanStreamedLevelBeLoaded(nextScene))
            {
                Debug.LogError($"SplashScreen: la escena '{nextScene}' no esta en Build Profiles > Scene List.", this);
                return;
            }

            StartCoroutine(SplashRoutine());
        }

        // Si la ventana pierde y recupera el foco, vuelve a ocultar el cursor
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _splashActive) HideCursor();
        }

        /// <summary>Opcional: llamar desde un Animation Event en el ultimo frame.</summary>
        public void EndAnimationLogo()
        {
            _animationDone = true;
        }

        private IEnumerator SplashRoutine()
        {
            // Carga la escena en segundo plano sin activarla
            AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
            op.allowSceneActivation = false;

            // Deja que el Animator se inicialice y arranca la animacion
            yield return null;
            if (!string.IsNullOrEmpty(stateName))
                animator.Play(stateName, layer, 0f);
            yield return null;

            // Musica y titulo en paralelo con la animacion
            StartCoroutine(PlayMusic());
            StartCoroutine(FadeTitle());

            // Espera a que la animacion termine (o salte el seguro)
            float elapsed = 0f;
            while (!_animationDone && elapsed < maxWaitTime)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
                if (info.normalizedTime >= 1f && !animator.IsInTransition(layer))
                {
                    _animationDone = true;
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Congela el ultimo frame para que no se reinicie si el Animator vuelve al estado inicial
            animator.speed = 0f;

            // Pausa extra
            yield return new WaitForSecondsRealtime(extraWaitAfterEnd);

            // Espera a que termine la musica (con seguro)
            if (waitForAudio && audioSource != null)
            {
                float audioWait = 0f;
                while (audioSource.isPlaying && audioWait < maxWaitTime)
                {
                    audioWait += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // Espera a que la escena este lista (Unity la deja en 0.9)
            while (op.progress < 0.9f) yield return null;

            // El menu necesita el cursor libre
            _splashActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            op.allowSceneActivation = true;
        }

        private IEnumerator PlayMusic()
        {
            if (audioSource == null) yield break;

            if (musicDelay > 0f) yield return new WaitForSecondsRealtime(musicDelay);

            if (audioSource.clip == null)
            {
                Debug.LogWarning("SplashScreen: el AudioSource no tiene ningun clip asignado.", this);
                yield break;
            }

            audioSource.Play();
        }

        private IEnumerator FadeTitle()
        {
            if (titleGroup == null) yield break;

            if (titleDelay > 0f) yield return new WaitForSecondsRealtime(titleDelay);

            float t = 0f;
            while (t < titleFadeTime)
            {
                t += Time.unscaledDeltaTime;
                titleGroup.alpha = Mathf.Clamp01(t / titleFadeTime);
                yield return null;
            }

            titleGroup.alpha = 1f;
        }

        private static void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}