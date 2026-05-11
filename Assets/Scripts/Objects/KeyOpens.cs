using UnityEngine;

public class KeyOpens : MonoBehaviour
{
    public int keysNeeded = 2;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UnityEngine.Debug.Log(other.gameObject.name + " entered the hurtbox trigger!");
            if (Player.Instance.hasKeys(keysNeeded)) Open();
        }
    }

    void Open()
    {
        for (int i = 0; i < keysNeeded; i++)
        {
            Player.Instance.SetKey(i, false);
        }
        gameObject.SetActive(false);
    }
}
