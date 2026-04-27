using UnityEngine;
using UnityEngine.AI;

public class GruntController : MonoBehaviour
{
    public Transform target;
    public HealthSystem healthSystem;
    private NavMeshAgent navMeshAgent;
    public float minDistance = 2f;
    public float maxDistance = 15f;
    private float distanceToPlayer;
    public float attackInterval = 1f;
    private float attackTimer = 0f;
    public float attackDamage = 20f;
    public float attackRange = 2.5f;

    bool canAttack = true;

    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = minDistance;
        healthSystem = GetComponent<HealthSystem>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, target.position);
            // basic player chasing
            if (IgnorePlayerCheck() == 1)
            {
                navMeshAgent.SetDestination(target.position);
            }

            // keeps the grunt facing player if it's within stop distance
            if (distanceToPlayer < minDistance)
            {
                KeepRotation();
            }

            // attack logic
            if (distanceToPlayer <= attackRange)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackInterval)
                {
                    // Perform attack
                    Attack();
                    attackTimer = 0f;
                }
            }
        }

        // Animation update
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);

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
        return distanceToPlayer <= maxDistance ? 1 : 0;
    }

    void Attack()
    {
        if (canAttack && distanceToPlayer <= attackRange)
        {
            HealthSystem.Instance.TakeDamage(attackDamage);
            canAttack = false;
            animator.SetTrigger("Attack");
            Invoke(nameof(RefreshAttack), attackInterval);
        }
    }

    void RefreshAttack()
    {
        animator.ResetTrigger("Attack");
        canAttack = true;
    }
}
