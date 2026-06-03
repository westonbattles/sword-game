using System;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour
{
    public static IReadOnlyList<Actor> ActiveActors => activeActors;
    static readonly List<Actor> activeActors = new List<Actor>();
    public Renderer[] Renderers { get; private set; }
    public Collider[] Colliders { get; private set; }

    int currentHealth;
    public int maxHealth;
    bool dead = false;
    bool deathHandled = false;
    public float bodyTimer = 5f;

    [Header("Audio")]
    [SerializeField] AudioClip[] deathSounds = Array.Empty<AudioClip>();
    [SerializeField, Range(0f, 1f)] float deathVolume = 1f;
    [SerializeField, Range(0.5f, 1.5f)] float deathPitchMin = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] float deathPitchMax = 1.05f;

    [Header("Illusory Walls")]
    public bool illusoryWall = false;
    public float fadeRate = 0.5f;

    Material[] materials;
    Color[] originalColors;
    Renderer rend;
    float fadeTimer = 0f;
    bool hasDashAttackRagdollDirection;
    Vector3 dashAttackRagdollDirection;

    void OnEnable()
    {
        if (!activeActors.Contains(this)) activeActors.Add(this);
    }

    void OnDisable()
    {
        activeActors.Remove(this);
    }

    void Awake()
    {
        // Save list of renderers/collideres for each actor so we effiently loop
        // over them (specifically for dash attack enemy handeling)
        Renderers = GetComponentsInChildren<Renderer>();
        Colliders = GetComponentsInChildren<Collider>();
        LoadDefaultDeathSounds();
        
        currentHealth = maxHealth;
        if (illusoryWall) // Only handled for the updating of transparency on illusory walls
        {
            rend = GetComponent<Renderer>();
            materials = rend.materials;  // Per-instance copies of all materials
            originalColors = new Color[materials.Length];

            
        }
        
    }

    void Update()
    {
        if (illusoryWall && dead)
        {
            fadeTimer += Time.deltaTime * fadeRate;

            for (int i = 0; i < materials.Length; i++)
            {
                Color c = originalColors[i];
                c.a = Mathf.Lerp(originalColors[i].a, 0f, fadeTimer);
                materials[i].color = c;
            }

            // Push the modified array back to the renderer
            rend.materials = materials;

            if (fadeTimer >= 1f)
            {
                Delete();
            }
        }
        else if (dead && !deathHandled)
        {
            Death();
        }
    }

    public void TakeDamage(int amount)
    {
        hasDashAttackRagdollDirection = false;
        ApplyDamage(amount);
    }

    public void TakeDamage(int amount, Vector3 dashAttackDirection)
    {
        hasDashAttackRagdollDirection = true;
        dashAttackRagdollDirection = dashAttackDirection.normalized;
        ApplyDamage(amount);
    }

    void ApplyDamage(int amount)
    {
        UnityEngine.Debug.Log("Damage taken");
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            dead = true;
            if (illusoryWall)
            {
                IllusoryWall();
            }
            else
            {
                Death();
            }
        }
    }

    void Death()
    {
        if (deathHandled) return;
        deathHandled = true;

        PlayDeathSound();

        // TEMPORARY: Destroy upon death
        // Later we want to add animations and likely some splatter effects too to make it feel more satisfying
        GruntController gruntController = gameObject.GetComponent<GruntController>();
        if (gruntController == null)
        {
            Invoke(nameof(Delete), bodyTimer);
            return;
        }

        if (hasDashAttackRagdollDirection)
        {
            gruntController.DeathHandling(dashAttackRagdollDirection);
        }
        else
        {
            gruntController.DeathHandling();
        }
        Invoke(nameof(Delete), bodyTimer);
    }

    void LoadDefaultDeathSounds()
    {
        if (deathSounds != null && deathSounds.Length > 0) return;

        deathSounds = Resources.LoadAll<AudioClip>("Audio/SFX/Death");
    }

    void PlayDeathSound()
    {
        if (deathSounds == null || deathSounds.Length == 0) return;

        AudioClip clip = deathSounds[UnityEngine.Random.Range(0, deathSounds.Length)];
        if (clip == null) return;

        GameObject soundObject = new GameObject("EnemyDeathSound");
        soundObject.transform.position = transform.position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = deathVolume;
        source.pitch = UnityEngine.Random.Range(deathPitchMin, deathPitchMax);
        source.spatialBlend = 1f;
        source.Play();

        Destroy(soundObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch)));
    }

    void Delete()
    {
        Destroy(gameObject);
    }

    void IllusoryWall()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            SetMaterialTransparent(materials[i]);

            Color c = materials[i].color;
            c.a = 1f;
            materials[i].color = c;
            originalColors[i] = c;
        }
        rend.materials = materials;
    }

    void SetMaterialTransparent(Material mat)
    {
        // URP uses the "Universal Render Pipeline/Lit" shader
        // Surface type is controlled via these properties:
        mat.SetFloat("_Surface", 1);                            // 1 = Transparent, 0 = Opaque
        mat.SetFloat("_Blend", 0);                             // 0 = Alpha blend
        mat.SetFloat("_AlphaClip", 0);                        // Disable alpha clipping
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
