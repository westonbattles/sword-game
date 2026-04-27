using UnityEngine;

public class Actor : MonoBehaviour
{
    int currentHealth;
    public int maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        UnityEngine.Debug.Log("Damage taken");
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Death();
        }
    }

    void Death()
    {
        // TEMPORARY: Destroy upon death
        // Later we want to add animations and likely some splatter effects too to make it feel more satisfying
        Destroy(gameObject);
    }
}
