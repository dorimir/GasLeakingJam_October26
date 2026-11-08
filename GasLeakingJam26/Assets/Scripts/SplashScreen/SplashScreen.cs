using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Calvo
{
    /// <summary>Un audio del splash con su propio delay, volumen y fade in.</summary>
    [System.Serializable]
    public class AudioCue
    {
        [Tooltip("Solo para identificarlo en el Inspector.")]
        public string name = "Audio";
        [Tooltip("AudioSource con el clip asignado y Play On Awake desactivado.")]
        public AudioSource source;
        [Tooltip("Segundos desde que empieza el splash hasta que suena este audio.")]
        public float delay = 0f;
        [Range(0f, 1f)] public float volume = 0.5f;
        [Tooltip("Segundos de fade in. 0 = empieza directamente al volumen indicado.")]
        public float fadeInTime = 0f;
    }

    /// <summary>
    /// Splash screen: reproduce una animacion + varios audios mezclados, muestra el titulo de la empresa
    /// con fade in, espera a que termine todo y carga la siguiente escena.
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
        [Tooltip("Seguro: si la animacion o los audios no terminan en este tiempo, se continua igualmente.")]
        [SerializeField] private float maxWaitTime = 20f;

        [Header("Titulo (Gas Leaking)")]
        [Tooltip("CanvasGroup del objeto con el texto del titulo. Opcional.")]
        [SerializeField] private CanvasGroup titleGroup;
        [Tooltip("Segundos desde que empieza el splash hasta que empieza a aparecer el titulo.")]
        [SerializeField] private float titleDelay = 1f;
        [SerializeField] private float titleFadeTime = 1f;

        [Header("Audios (se mezclan; empieza antes el que tenga menos delay)")]
        [SerializeField] private AudioCue[] audioCues;
        [Tooltip("Si esta activo, no se cambia de escena hasta que terminen todos los audios.")]
        [SerializeField] private bool waitForAudio = true;

        [Header("Escena")]
        [SerializeField] private string nextScene = "MainMenu";

        [Header("Opciones")]
        [SerializeField] private bool clearPrefsInEditor = false;

        private bool _animationDone;
        private bool _splashActive;
        private int _cuesRunning;

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

            if (audioCues != null)
            {
                foreach (AudioCue cue in audioCues)
                    if (cue != null && cue.source != null) cue.source.playOnAwake = false;
            }
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

            // Audios y titulo en paralelo con la animacion
            StartAudioCues();
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

            // Espera a que terminen todos los audios (incluidos los que aun no han empezado)
            if (waitForAudio)
            {
                float audioWait = 0f;
                while (_cuesRunning > 0 && audioWait < maxWaitTime)
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

        private void StartAudioCues()
        {
            if (audioCues == null) return;

            // Primero se cuentan los validos, para que la espera final sepa cuantos faltan
            foreach (AudioCue cue in audioCues)
            {
                if (cue == null || cue.source == null) continue;

                if (cue.source.clip == null)
                {
                    Debug.LogWarning($"SplashScreen: el audio '{cue.name}' no tiene clip asignado.", this);
                    continue;
                }

                _cuesRunning++;
                StartCoroutine(PlayCue(cue));
            }
        }

        private IEnumerator PlayCue(AudioCue cue)
        {
            if (cue.delay > 0f) yield return new WaitForSecondsRealtime(cue.delay);

            cue.source.volume = cue.fadeInTime > 0f ? 0f : cue.volume;
            cue.source.Play();

            // Fade in
            if (cue.fadeInTime > 0f)
            {
                float t = 0f;
                while (t < cue.fadeInTime)
                {
                    t += Time.unscaledDeltaTime;
                    cue.source.volume = Mathf.Lerp(0f, cue.volume, Mathf.Clamp01(t / cue.fadeInTime));
                    yield return null;
                }

                cue.source.volume = cue.volume*0.3f;
            }

            // Espera a que termine este audio
            while (cue.source.isPlaying) yield return null;

            _cuesRunning--;
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