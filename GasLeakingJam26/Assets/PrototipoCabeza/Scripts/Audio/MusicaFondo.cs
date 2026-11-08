using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Música de fondo de todo el juego, elegida por escena. Se crea sola al arrancar (Resources/MusicaFondo),
/// no se corta al cambiar de escena:
///  - si la escena nueva usa la MISMA canción, sigue sonando y solo cambia el volumen (suave);
///  - si usa OTRA canción, hace un fundido cruzado;
///  - si la escena no está en la lista, sigue lo que estaba sonando;
///  - si la entrada no tiene canción (vacía), la música se apaga con fundido.
public class MusicaFondo : MonoBehaviour
{
    [System.Serializable]
    public class Entrada
    {
        [Tooltip("Nombres de escena separados por coma")]
        public string escenas;
        [Tooltip("Vacío = silencio en estas escenas")]
        public AudioClip cancion;
        [Range(0f, 1f)] public float volumen = 0.08f;
    }

    public Entrada[] porEscena;
    [Tooltip("Segundos del fundido al cambiar de canción o de volumen")]
    public float fundido = 1.2f;

    static MusicaFondo instancia;
    AudioSource a, b;            // dos fuentes para el fundido cruzado
    Coroutine rutina;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia) return;
        var prefab = Resources.Load<GameObject>("MusicaFondo");
        if (!prefab) return;
        var go = Instantiate(prefab);
        go.name = "MusicaFondo";
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instancia && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        a = GetComponent<AudioSource>(); if (!a) a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var f in new[] { a, b }) { f.loop = true; f.playOnAwake = false; f.spatialBlend = 0f; f.Stop(); f.volume = 0f; }
        SceneManager.sceneLoaded += AlCargar;
    }

    void Start()
    {
        // la primera escena del juego no siempre avisa con sceneLoaded
        Aplicar(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (instancia == this) { SceneManager.sceneLoaded -= AlCargar; instancia = null; }
    }

    void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Single) Aplicar(escena.name);
    }

    Entrada Buscar(string escena)
    {
        if (porEscena == null) return null;
        foreach (var e in porEscena)
        {
            if (e == null || string.IsNullOrEmpty(e.escenas)) continue;
            foreach (var n in e.escenas.Split(','))
                if (n.Trim() == escena) return e;
        }
        return null;
    }

    void Aplicar(string escena)
    {
        var e = Buscar(escena);
        if (e == null) return;   // escena sin entrada: sigue lo que sonaba
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Cambiar(e.cancion, e.cancion ? e.volumen : 0f, fundido));
    }

    IEnumerator Cambiar(AudioClip cancion, float volumen, float dur)
    {
        AudioSource actual = a.isPlaying && (!b.isPlaying || a.volume >= b.volume) ? a : (b.isPlaying ? b : a);
        AudioSource otra = actual == a ? b : a;
        // ya suena esa canción -> solo cambia el volumen, sin cortar
        if (cancion != null && actual.isPlaying && actual.clip == cancion)
        {
            float v0 = actual.volume, v1 = otra.volume, t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, dur);
                actual.volume = Mathf.Lerp(v0, volumen, t);
                otra.volume = Mathf.Lerp(v1, 0f, t);
                yield return null;
            }
            otra.Stop();
            yield break;
        }
        // otra canción (o silencio): fundido cruzado
        if (cancion != null) { otra.clip = cancion; otra.volume = 0f; otra.Play(); }
        float va = actual.volume, tt = 0f;
        while (tt < 1f)
        {
            tt += Time.unscaledDeltaTime / Mathf.Max(0.01f, dur);
            actual.volume = Mathf.Lerp(va, 0f, tt);
            if (cancion != null) otra.volume = Mathf.Lerp(0f, volumen, tt);
            yield return null;
        }
        actual.Stop();
    }
}
