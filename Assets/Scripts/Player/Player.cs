using KinematicCharacterController;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class Player : MonoBehaviour, ICharacterController
{
    AudioSource audioSource;
    
    [Header("Gameplay")]
    [SerializeField] bool autoBhop;

    [Header("Movement")]
    [SerializeField] public Camera mainCamera;
    [SerializeField] public float gravity = 40f;
    [SerializeField] public float gravityMultiplier = 1.025f;
    [SerializeField] float groundAccel = 130f;
    [SerializeField] float airAccel = 50f;
    [SerializeField] float airSpeed = 1f;
    [SerializeField] float friction = 12f;
    [SerializeField] float jumpHeight = 12f;
    [SerializeField] float jumpBufferTime = 0.2f;

    [Header("Dash")]
    [SerializeField] public float dashSpeed = 10f;
    [SerializeField] private HealthSystem HealthSystem;
    public event Action OnJump;
    public event Action OnLand;

    [Header("Attacking")]
    public float attackDistance = 3f;
    public float attackDelay = 0.4f;
    public float attackSpeed = 1f;
    public int attackDamage = 5;
    public LayerMask attackLayer;

    public GameObject hitEffect;
    public AudioClip swordSwing;
    public AudioClip hitSound;
    bool attacking = false;
    bool readyToAttack = true;
    int attackCount;


    Vector3 _dashVelocity;
    bool _shouldDash;

    public static Player Instance { get; private set; }
    public KinematicCharacterMotor Motor => _motor;
    public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;

    KinematicCharacterMotor _motor;
    Quaternion _inputRot;
    Vector2 _moveInput;
    bool _jumpInput;
    bool _isJumpingThisFrame;
    float _jumpBufferCounter;
    [Header("Map Triggers")]
	public GameObject Bars;

    void Awake()
    {
        Instance = this;
        _motor = GetComponent<KinematicCharacterMotor>();
        if (autoBhop) jumpBufferTime = 0.01f; // jump buffer with auto bhop feels bad
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        _motor.CharacterController = this;
        HealthSystem = GetComponent<HealthSystem>();
    }

    void Update()
    {
        UpdateInput();
    }

    void UpdateInput()
    {
        _moveInput = InputSystem.actions["Move"].ReadValue<Vector2>();
        _jumpInput = autoBhop ? InputSystem.actions["Jump"].IsPressed() : InputSystem.actions["Jump"].WasPressedThisFrame();
        _inputRot = mainCamera.transform.rotation;
        if (_jumpInput)
            _jumpBufferCounter = jumpBufferTime;
        else
            _jumpBufferCounter -= Time.deltaTime;
        if (InputSystem.actions["Attack"].IsPressed())
        {
            Attack();
        }
    }

    // Called from KinematicCharacterMotor 
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        VelocitySet(ref currentVelocity, deltaTime);
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        Vector3 forward = Vector3.ProjectOnPlane(_inputRot * Vector3.forward, _motor.CharacterUp);
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, _motor.CharacterUp);
    }

    /// Performs dash in a given direction with given speed
    public void Dash(Vector3 directionNormalized, float speed)
    {
        _dashVelocity = directionNormalized * speed;
        _shouldDash = true;
        _motor.ForceUnground(.25f); // lets you dash along objects without insta stopping you
        HealthSystem.Instance.UseMana(20); // consumes 20 mana on dash
    }

    void VelocitySet(ref Vector3 currentVelocity, float dt)
    {
        if (_shouldDash)
        {
            currentVelocity = _dashVelocity;
            _shouldDash = false;
            return;  // skip normal movement for this one frame
        }

        // inputdir is just which global direction the player is trying to move in
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
        inputDir = Quaternion.Euler(0, _inputRot.eulerAngles.y, 0) * inputDir;
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        if (_motor.GroundingStatus.IsStableOnGround)
        {
            bool hasSupportBelow = _motor.GroundingStatus.GroundNormal != Vector3.zero;

            if (hasSupportBelow)
            {
                currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            }

            Vector3 reorientedInput = _motor.GetDirectionTangentToSurface(inputDir, _motor.GroundingStatus.GroundNormal);
            Vector2 target = new Vector2(reorientedInput.x, reorientedInput.z);
            Vector2 horivel = new Vector2(currentVelocity.x, currentVelocity.z);
            if (_jumpBufferCounter > 0f)
            {
                OnJump?.Invoke();
                _motor.ForceUnground(0.2f);
                _jumpBufferCounter = 0f;
                currentVelocity = currentVelocity - Vector3.Project(currentVelocity, _motor.CharacterUp);
                currentVelocity += _motor.CharacterUp * jumpHeight;
                horivel = MoveAir(target, horivel, dt);
            }
            else
            {
                horivel = MoveGround(target, horivel, dt);
            }

            currentVelocity = new Vector3(horivel.x, currentVelocity.y, horivel.y);
        }
        else
        {
            if (!_isJumpingThisFrame && _motor.GroundingStatus.FoundAnyGround)
            {
                Vector3 perpendicular = Vector3.Cross(Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal), _motor.CharacterUp).normalized;
                inputDir = Vector3.ProjectOnPlane(inputDir, perpendicular);
            }
            Vector2 horivel = new Vector2(currentVelocity.x, currentVelocity.z);

            Vector2 target = new Vector2(inputDir.x, inputDir.z);
            horivel = MoveAir(target, horivel, dt);

            currentVelocity.y -= (gravity * gravityMultiplier) * dt;
            currentVelocity = new Vector3(horivel.x, currentVelocity.y, horivel.y);
        }
    }

    Vector2 MoveGround(Vector2 target, Vector2 horivel, float dt)
    {
        var speed = horivel.magnitude;
        if (speed != 0f)
        {
            float drop = speed * friction * dt;
            horivel *= Mathf.Max(speed - drop, 0f) / speed;
        }
        return Accelerate(target, horivel, groundAccel, dt);
    }

    Vector2 MoveAir(Vector2 target, Vector2 horivel, float dt)
    {
        return Accelerate(target, horivel, airAccel, dt);
    }

    Vector2 Accelerate(Vector2 target, Vector2 horivel, float acceleration, float dt)
    {
        float accelVelocity = acceleration * dt;
        float projectedVelocity = Vector2.Dot(target, horivel);
        if (!_motor.GroundingStatus.IsStableOnGround && projectedVelocity >= airSpeed)
        {
            return horivel;
        }
        else
        {
            return horivel + (target * accelVelocity);
        }
    }

    public void BeforeCharacterUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!_motor.LastGroundingStatus.IsStableOnGround && _motor.GroundingStatus.IsStableOnGround)
        {
            OnLand?.Invoke();
            _motor.StepHandling = StepHandlingMethod.Standard;
            _isJumpingThisFrame = false;
        }

        else if (!_motor.GroundingStatus.IsStableOnGround && _motor.LastGroundingStatus.IsStableOnGround)
        {
            _motor.StepHandling = StepHandlingMethod.None;
            _isJumpingThisFrame = true;
        }
    }
    public bool IsColliderValidForCollisions(Collider coll) { return true; }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log("Player entered trigger with tag " + other.tag);
        if (other.gameObject.CompareTag("HealthZone"))
        {
            HealthSystem.Instance.HealDamage(HealthSystem.Instance.maxHitPoint);
        }
        else if (other.gameObject.CompareTag("ManaZone"))
        {
            HealthSystem.Instance.RestoreMana(HealthSystem.Instance.maxManaPoint);
        }
		if (other.gameObject.CompareTag("ShowBars"))
		{
			Bars.SetActive(true);
		}
    }

    public void Attack()
    {
        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(swordSwing);
        UnityEngine.Debug.Log("Attack function completed.");
    }

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
        UnityEngine.Debug.Log("Attack reset");
    }

    void AttackRaycast()
    {
        UnityEngine.Debug.Log("AttackRaycast called");
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
        {
            HitTarget(hit.point);

            if (hit.transform.TryGetComponent<Actor>(out Actor T))
            {
                UnityEngine.Debug.Log("Attempting to deal damage via player script");
                T.TakeDamage(attackDamage);
            }
        }
    }

    void HitTarget(Vector3 pos)
    {
        UnityEngine.Debug.Log("HitTarget called");
        audioSource.pitch = 1;
        audioSource.PlayOneShot(hitSound);

        GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(GO, 20);

        
    }
}
