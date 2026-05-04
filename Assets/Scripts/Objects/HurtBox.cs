using UnityEngine;

public class HurtBox : MonoBehaviour
{
    public float damage = 100f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UnityEngine.Debug.Log(other.gameObject.name + " entered the hurtbox trigger!");
            HealthSystem.Instance.TakeDamage(damage);
        }
    }
}
