using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject EnemyPrefab;

    [Tooltip("One enemy will be spawned per spawn point per wave.")]
    public List<Transform> SpawnPoints = new List<Transform>();

    [Header("Wave Settings")]
    [Tooltip("Total number of waves to run.")]
    public int TotalWaves = 3;

    [Tooltip("Delay in seconds before the first wave begins after the player enters the trigger.")]
    public float InitialDelay = 1.0f;

    [Tooltip("Delay in seconds between waves once all enemies are cleared.")]
    public float WaveCooldown = 3.0f;

    [Header("Player Detection")]
    [Tooltip("Tag used to identify the player GameObject.")]
    public string PlayerTag = "Player";

    private readonly List<GameObject> _activeEnemies = new List<GameObject>();

    private int _currentWave = 0;
    private bool _spawnerActive = false;
    private bool _waveRunning = false;

    public GameObject[] DoorUnlock;


    private void OnTriggerEnter(Collider other)
    {
        // Only react to the player, and only once.
        if (_spawnerActive || !other.CompareTag(PlayerTag))
            return;

        _spawnerActive = true;
        Debug.Log("[EnemySpawner] Player detected – starting wave sequence.");
        StartCoroutine(RunWaveSequence());
    }

    // -------------------------------------------------------------------------
    // Wave logic
    // -------------------------------------------------------------------------

    private IEnumerator RunWaveSequence()
    {
        yield return new WaitForSeconds(InitialDelay);

        while (_currentWave < TotalWaves)
        {
            _currentWave++;
            Debug.Log($"[EnemySpawner] Starting wave {_currentWave} / {TotalWaves}.");

            SpawnWave();

            // Wait until every enemy in this wave is destroyed.
            yield return new WaitUntil(AllEnemiesCleared);

            Debug.Log($"[EnemySpawner] Wave {_currentWave} cleared.");

            if (_currentWave < TotalWaves)
            {
                Debug.Log($"[EnemySpawner] Next wave in {WaveCooldown}s…");
                yield return new WaitForSeconds(WaveCooldown);
            }
        }

        Debug.Log("[EnemySpawner] All waves completed!");
        OnAllWavesComplete();
    }

    private void SpawnWave()
    {
        // Remove any stale references before spawning.
        _activeEnemies.Clear();

        if (SpawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No spawn points assigned!");
            return;
        }

        if (EnemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] EnemyPrefab is not assigned!");
            return;
        }

        foreach (Transform spawnPoint in SpawnPoints)
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning("[EnemySpawner] A spawn point reference is null – skipping.");
                continue;
            }

            GameObject enemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
            _activeEnemies.Add(enemy);
        }

        Debug.Log($"[EnemySpawner] Spawned {_activeEnemies.Count} enemies.");
    }


    /// Returns true once every tracked enemy has been destroyed.
    /// Null entries are treated as destroyed.
    private bool AllEnemiesCleared()
    {
        foreach (GameObject enemy in _activeEnemies)
        {
            if (enemy != null)
                return false;
        }
        return true;
    }

    // -------------------------------------------------------------------------
    // Extensibility hook
    // -------------------------------------------------------------------------

    
    protected virtual void OnAllWavesComplete()
    {
        // Example: disable the trigger collider so it can't be re-activated.
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        foreach (GameObject wall in DoorUnlock)
        {
            wall.SetActive(false);
        }
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        // Visualise spawn points in the Scene view.
        if (SpawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform sp in SpawnPoints)
        {
            if (sp == null) continue;
            Gizmos.DrawWireSphere(sp.position, 0.4f);
            Gizmos.DrawLine(transform.position, sp.position);
        }
    }
}