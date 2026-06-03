using UnityEngine;
using TMPro;

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [SerializeField] TextMeshProUGUI timerText;
    float _elapsed;
    bool _running = true;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;
        int minutes = Mathf.FloorToInt(_elapsed / 60f);
        int seconds = Mathf.FloorToInt(_elapsed % 60f);
        int milliseconds = Mathf.FloorToInt((_elapsed * 100f) % 100f);
        timerText.text = $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    public void StopTimer()
    {
        _running = false;
    }

    public float GetTime()
    {
        return _elapsed;
    }
}