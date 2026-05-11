using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    public int keyType = 0; // 0 for blue, 1 for red, 2 for green, 3 for yellow, 4 for purple (we probably never need this many keys)
    public int rotationSpeed = 100;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UnityEngine.Debug.Log(other.gameObject.name + " entered the hurtbox trigger!");
            Player.Instance.SetKey(keyType, true);
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }
}
