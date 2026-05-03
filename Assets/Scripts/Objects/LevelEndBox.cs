using UnityEngine;

public class LevelEndBox : MonoBehaviour
{
    public GameObject endPanel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UnityEngine.Debug.Log(other.gameObject.name + " entered the trigger!");
            endPanel.GetComponent<LevelEndScreen>().levelEnd();
        }
    }
}
