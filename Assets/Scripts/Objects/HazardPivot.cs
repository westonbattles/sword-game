using System.Security.Cryptography;
using UnityEngine;

public class HazardPivot : MonoBehaviour
{
    public float speed = 2.0f;
    public float maxRotation = 30.0f; // Rotates 30 degrees left and 30 degrees right
    public float direction = 1f; // negative to switch direction
    public float offset = 0f;

    void Update()
    {
        float angle = Mathf.Sin(Time.time * speed) * maxRotation;
        float adjustedAngle = (angle + offset) * direction;
        transform.rotation = Quaternion.Euler(adjustedAngle, 0, 0);
    }
}
