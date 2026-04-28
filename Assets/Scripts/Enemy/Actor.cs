using System;
using UnityEngine;

public class Actor : MonoBehaviour
{
    int currentHealth;
    public int maxHealth;
    bool dead = false;

    [Header("Illusory Walls")]
    public bool illusoryWall = false;
    public float fadeRate = 0.5f;

    Material[] materials;
    Color[] originalColors;
    Renderer rend;
    float fadeTimer = 0f;

    void Awake()
    {
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
                Death();
            }
        }
        else if (dead)
        {
            Death();
        }
    }

    public void TakeDamage(int amount)
    {
        UnityEngine.Debug.Log("Damage taken");
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            dead = true;
            if (illusoryWall) { IllusoryWall(); }
        }
    }

    void Death()
    {
        // TEMPORARY: Destroy upon death
        // Later we want to add animations and likely some splatter effects too to make it feel more satisfying
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
