using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicPlayer : MonoBehaviour
{
    const string MusicRootResourceFolder = "Audio/Music";
    const string MainMenuSceneName = "StartScreen";

    static BackgroundMusicPlayer _instance;
    static float _musicVolume = 0.6f;

    AudioSource _audioSource;
    AudioListener _fallbackListener;
    string _currentMusicFolder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateRuntimeMusicPlayer()
    {
        if (_instance != null) return;

        GameObject musicPlayerObject = new GameObject("BackgroundMusicPlayer");
        DontDestroyOnLoad(musicPlayerObject);
        _instance = musicPlayerObject.AddComponent<BackgroundMusicPlayer>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.volume = _musicVolume;

        _fallbackListener = gameObject.AddComponent<AudioListener>();
        _fallbackListener.enabled = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateMusicForScene(SceneManager.GetActiveScene());
        EnsureSingleAudioListener();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateMusicForScene(scene);
        EnsureSingleAudioListener();
    }

    void UpdateMusicForScene(Scene scene)
    {
        string musicFolder = GetMusicFolderForScene(scene.name);
        if (string.IsNullOrWhiteSpace(musicFolder))
        {
            _currentMusicFolder = null;
            _audioSource.Stop();
            _audioSource.clip = null;
            return;
        }

        if (_currentMusicFolder == musicFolder && _audioSource.clip != null)
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }

            return;
        }

        AudioClip clip = LoadMusicClip(musicFolder);
        if (clip == null)
        {
            _currentMusicFolder = null;
            _audioSource.Stop();
            _audioSource.clip = null;
            Debug.LogWarning($"No background music found for {scene.name}. Add an AudioClip to Assets/Resources/{musicFolder}, preferably named MainTheme.");
            return;
        }

        _currentMusicFolder = musicFolder;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    string GetMusicFolderForScene(string sceneName)
    {
        if (sceneName == MainMenuSceneName)
        {
            return null;
        }

        if (sceneName.Contains("Town"))
        {
            return $"{MusicRootResourceFolder}/Town";
        }

        if (sceneName.Contains("Castle"))
        {
            return $"{MusicRootResourceFolder}/Castle";
        }

        if (sceneName.Contains("Dungeon") || sceneName.StartsWith("1-"))
        {
            return $"{MusicRootResourceFolder}/Dungeon";
        }

        return null;
    }

    AudioClip LoadMusicClip(string musicFolder)
    {
        AudioClip preferredClip = Resources.Load<AudioClip>($"{musicFolder}/MainTheme");
        if (preferredClip != null)
        {
            return preferredClip;
        }

        AudioClip[] musicClips = Resources.LoadAll<AudioClip>(musicFolder);
        return musicClips.Length > 0 ? musicClips[0] : null;
    }

    public static float MusicVolume => _musicVolume;

    public static void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (_instance != null && _instance._audioSource != null)
        {
            _instance._audioSource.volume = _musicVolume;
        }
    }

    void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
        AudioListener preferredListener = null;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            preferredListener = mainCamera.GetComponent<AudioListener>();
        }

        if (preferredListener == null)
        {
            foreach (AudioListener listener in listeners)
            {
                if (listener != _fallbackListener)
                {
                    preferredListener = listener;
                    break;
                }
            }
        }

        if (preferredListener == null)
        {
            preferredListener = _fallbackListener;
        }

        foreach (AudioListener listener in listeners)
        {
            listener.enabled = listener == preferredListener;
        }

        if (preferredListener == _fallbackListener)
        {
            _fallbackListener.enabled = true;
        }
    }
}
