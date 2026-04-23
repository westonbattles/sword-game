using UnityEngine;
using UnityEngine.AI;

public class GruntController : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent navMeshAgent;
    public float minDistance = 2f;
    public float maxDistance = 15f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = minDistance;

    }

    // Update is called once per frame
    void Update()
    {
        {
            if (target != null)
            {
                // basic player chasing
                if (IgnorePlayerCheck() == 1)
                {
                    navMeshAgent.SetDestination(target.position);
                }

                // keeps the grunt facing player if it's within stop distance
                if (Vector3.Distance(transform.position, target.position) < minDistance)
                {
                    KeepRotation();
                }
            }
        }
    }

    // without this, the grunt wont rotate towards the player if its within the stop distance
    void KeepRotation()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // disables grunt ai when player is far enough away so its not needlessly pathfinding
    int IgnorePlayerCheck()
    {
        return Vector3.Distance(transform.position, target.position) <= maxDistance ? 1 : 0;
    }
}
