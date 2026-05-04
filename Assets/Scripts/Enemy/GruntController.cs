using System.Collections.Specialized;
using System.Security.Cryptography;
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
    private float attackTimer = 1f;
    public float attackDamage = 20f;
    public float attackRange = 2.5f;

    public float ragdollSpeed = 20f;

    bool canAttack = true;
    bool dead = false;

    private Rigidbody rb;
    public Transform pelvis;
    private Rigidbody[] ragdollRigidbodies;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = minDistance;
        healthSystem = GetComponent<HealthSystem>();
        rb = GetComponent<Rigidbody>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();

        SetRagdollState(false); // start with ragdoll disabled
    }

    // Update is called once per frame
    void Update()
    {
        if (!dead)
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
    public void SetRagdollState(bool state)
    {
        // Toggle animator so it doesn't fight physics
        if (animator != null) animator.enabled = !state;
        gameObject.GetComponent<CapsuleCollider>().enabled = !state;
        navMeshAgent.enabled = !state;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            // Set kinematics to false when state is true for all rigidbody comps
            rb.isKinematic = !state;
        }
    }

    public void DeathHandling()
    {
        animator.SetBool("isDead", true);
        dead = true;
        SetRagdollState(true);

        Rigidbody pelvisRb = pelvis.GetComponent<Rigidbody>();
        pelvisRb.AddForce(-transform.forward * ragdollSpeed, ForceMode.Impulse);
    }

    public void DeathHandling(Vector3 ragdollDirection)
    {
        animator.SetBool("isDead", true);
        dead = true;
        SetRagdollState(true);

        Vector3 forceDirection = ragdollDirection.sqrMagnitude > 0.001f
            ? ragdollDirection.normalized
            : transform.forward;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.linearVelocity = forceDirection * ragdollSpeed;
        }

        Rigidbody pelvisRb = pelvis.GetComponent<Rigidbody>();
        pelvisRb.AddForce(forceDirection * ragdollSpeed, ForceMode.VelocityChange);
    }
}
