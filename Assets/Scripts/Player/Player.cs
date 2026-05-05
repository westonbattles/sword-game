using KinematicCharacterController;
using System;
using System.Collections;
using Sword;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class Player : MonoBehaviour, ICharacterController
{
    
    AudioSource audioSource;
   
    [SerializeField] Animator playerAnimator;
    public Animator PlayerAnimator => playerAnimator;
    
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

    [Header("Crouch / Slide")]
    [SerializeField] float crouchHeight = 1.0f;
    [SerializeField] float standingHeight = 1.75f;    
    [SerializeField] float crouchSpeedMax = 4f;
    [SerializeField] float slideSpeed = 20f;
    [SerializeField] float slideDuration = 0.8f;
    [SerializeField] float slideCooldown = 1f;
    [SerializeField] float slideFriction = 5f;

    private bool _isCrouching;
    private bool _isSliding;
    private bool _slideJustStarted;
    private float _slideTimer;
    private float _slideCooldownTimer;
    private Vector3 _slideDirection;
    private float _slideCurrentSpeed;

    [Header("Dash")]
    [SerializeField] public float dashSpeed = 10f;
    [SerializeField] private HealthSystem HealthSystem;
    public event Action OnJump;
    public event Action OnLand;


    [Header("Attacking")] 
    public LayerMask enemyLayerMask;
    public AudioClip swordSwing;
    public AudioClip hitSound;
    public bool attacking { get; private set; } = false;
    bool readyToAttack = true;
    
    [Header("Dash Attack")]
    public float regularAttackRange = 2f;
    public float dashAttackDuration = 0.25f;
    public float dashAttackFloatTime = 0.25f;
    public float dashAttackPlowDuration = 0.5f;
    [Range(0f, 1f)] public float dashAttackBrakeFactor = 0.15f;
    public float dashAttackInputRampTime = 0.12f;
    public float dashAttackScreenRadius = 0.12f;
    public float dashAttackMomentumScale = 0.2f;
    public float dashAttackMaxMomentumBonus = 4f;
    public int dashAttackDamage = 5;
    

    [Header("Debug")]
    public bool depleteStamina = true;
    public bool depleteHealth = true;
    public bool drawDebugTargetGui = true;
    
    Vector3 _dashVelocity;
    bool _shouldDash;
    PlayerDashAttack _dashAttack;

    public static Player Instance { get; private set; }
    
    public KinematicCharacterMotor Motor => _motor;
    
    public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;
    public bool IsCrouching => _isCrouching;
    public bool IsSliding => _isSliding;
    public bool IsJumpHeld { get; private set; }

    KinematicCharacterMotor _motor;
    Quaternion _inputRot;
    Vector2 _moveInput;
    bool _jumpInput;
    bool _attackInput;
    bool _dashAttackInput;
    bool _isJumpingThisFrame;
    bool _shouldBrakeDashAttack;
    float _dashAttackInputRampTimer;
    float _pendingDashAttackBrakeFactor;
    float _jumpBufferCounter;
    [Header("Map Triggers")]
    public GameObject Bars;

    void Awake()
    {
        Instance = this;
        _motor = GetComponent<KinematicCharacterMotor>();
        _dashAttack = new PlayerDashAttack(this);
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
        
        // reset attack when swing animation stops
        bool swingAnimationNotPlaying = !PlayerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Armature|SwordSwing");
        if (attacking && swingAnimationNotPlaying) { ResetAttack(); }

        HandleAttack();
        _dashAttack.Tick();
    }

    void UpdateInput()
    {
        _moveInput = InputSystem.actions["Move"].ReadValue<Vector2>();
        IsJumpHeld = InputSystem.actions["Jump"].IsPressed();
        _jumpInput = autoBhop ? InputSystem.actions["Jump"].IsPressed() : InputSystem.actions["Jump"].WasPressedThisFrame();
        _attackInput = InputSystem.actions["Attack"].IsPressed();
        _dashAttackInput = InputSystem.actions["Dash Attack"].WasPressedThisFrame();
        _inputRot = mainCamera.transform.rotation;

        if (_jumpInput)
            _jumpBufferCounter = jumpBufferTime;
        else
            _jumpBufferCounter -= Time.deltaTime;

        bool crouchHeld = InputSystem.actions["Crouch"].IsPressed();
        bool slidePressed = InputSystem.actions["Slide"].WasPressedThisFrame();

        //tick slide cooldown
        if (_slideCooldownTimer > 0f)
            _slideCooldownTimer -= Time.deltaTime;

        //start slide
        if (slidePressed && IsGrounded && !_isSliding && _slideCooldownTimer <= 0f)
        {
            _isSliding = true;
            _slideJustStarted = true;
            _slideTimer = slideDuration;
            _slideCurrentSpeed = slideSpeed;
        }

        _isCrouching = crouchHeld || _isSliding;

        if (InputSystem.actions["Reset"].IsPressed())
        {
            Die();
        }

        //update player height
        if (_isCrouching)
        {
            _motor.SetCapsuleDimensions(0.5f, crouchHeight, crouchHeight * 0.5f);
        }
        else
        {
            TryStandUp();
        }
    }

    void HandleAttack()
    {
        if ((!_attackInput && !_dashAttackInput)|| !readyToAttack) return;
        if (SwordController.Instance.IsHeld == false) return; // cant swing / attack
        
        //bool dashAttack = _dashAttack.TryDashAttack();
        if (_dashAttackInput)
        {
            _dashAttack.TryDashAttack();
        }
        if (_attackInput)
        {
            Attack();
            PlayerAnimator.SetTrigger("SwingTrigger"); 
            StartCoroutine(ClearTriggerNextFrame());
        }
    }

    IEnumerator ClearTriggerNextFrame()         
    {                                                                                                           
        yield return null; // wait one frame
        playerAnimator.ResetTrigger("SwingTrigger");                                                            
    }

    //stands unless there is a ceiling
    void TryStandUp()
    {
        //check for clearance before standing
        Collider[] overlapBuffer = new Collider[8];
        int overlaps = _motor.CharacterOverlap(
            _motor.TransientPosition + _motor.CharacterUp * (standingHeight - crouchHeight),
            _motor.TransientRotation,
            overlapBuffer,
            _motor.CollidableLayers,
            QueryTriggerInteraction.Ignore
        );

        if (overlaps == 0)
        {
            _motor.SetCapsuleDimensions(0.5f, standingHeight, standingHeight * 0.5f);
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
    public void Dash(Vector3 directionNormalized, float speed, bool dashAttack)
    {
        _dashVelocity = directionNormalized * speed;
        _shouldDash = true;
        
        // if the attack is a dash attack, enable enemy plowing
        if (dashAttack)
        {
            _dashAttack.BeginDashAttack();
        }
        
        _motor.ForceUnground(.25f); // lets you dash along objects without insta stopping you
        if (depleteStamina) HealthSystem.Instance.UseMana(20); // consumes 20 mana on dash
    }

    public void RequestDashAttackBrake(float brakeFactor)
    {
        _shouldBrakeDashAttack = true;
        _pendingDashAttackBrakeFactor = Mathf.Clamp01(brakeFactor);
        _dashAttackInputRampTimer = Mathf.Max(0f, dashAttackInputRampTime);
    }
    
    void VelocitySet(ref Vector3 currentVelocity, float dt)
    {
        if (_shouldDash)
        {
            currentVelocity = _dashVelocity;
            _shouldDash = false;
            return;
        }

        if (_shouldBrakeDashAttack)
        {
            _shouldBrakeDashAttack = false;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);
            Vector3 verticalVelocity = currentVelocity - horizontalVelocity;
            currentVelocity = verticalVelocity + horizontalVelocity * _pendingDashAttackBrakeFactor;
        }

        float inputScale = 1f;
        if (_dashAttackInputRampTimer > 0f && dashAttackInputRampTime > 0f)
        {
            _dashAttackInputRampTimer = Mathf.Max(0f, _dashAttackInputRampTimer - dt);
            inputScale = 1f - (_dashAttackInputRampTimer / dashAttackInputRampTime);
        }

        Vector2 effectiveMoveInput = _moveInput * inputScale;
        Vector3 inputDir = new Vector3(effectiveMoveInput.x, 0f, effectiveMoveInput.y);
        inputDir = Quaternion.Euler(0, _inputRot.eulerAngles.y, 0) * inputDir;
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        //slide
        if (_isSliding)
        {
            // Capture momentum direction on the first physics tick of the slide
            // This is done here (not UpdateInput) so we have access to currentVelocity
            if (_slideJustStarted)
            {
                _slideJustStarted = false;
                Vector3 horizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                _slideDirection = horizontal.magnitude > 0.1f
                    ? horizontal.normalized  // slide along current momentum
                    : transform.forward;     // fallback if standing still
            }

            // Bleed speed with friction over the motor's timestep
            _slideCurrentSpeed = Mathf.Max(0f, _slideCurrentSpeed - slideFriction * dt);

            // Tick slide timer on the motor's timestep (not Update) for consistency
            _slideTimer -= dt;

            bool slideExpired = _slideTimer <= 0f;
            bool leftGround = !IsGrounded;

            if (slideExpired || leftGround)
            {
                _isSliding = false;
                _slideCooldownTimer = slideCooldown;
                // Don't change _isCrouching here - UpdateInput owns that next frame
            }
            else if (_jumpBufferCounter > 0f)
            {
                // Jump out of slide
                _isSliding = false;
                _isCrouching = false;
                _slideCooldownTimer = slideCooldown;
                _motor.SetCapsuleDimensions(0.5f, standingHeight, standingHeight * 0.5f);
                OnJump?.Invoke();
                _motor.ForceUnground(0.2f);
                _jumpBufferCounter = 0f;
                currentVelocity = currentVelocity - Vector3.Project(currentVelocity, _motor.CharacterUp);
                currentVelocity += _motor.CharacterUp * jumpHeight;
            }
            else
            {
                currentVelocity = _slideDirection * _slideCurrentSpeed;
            }
            return; // skip normal movement while sliding
        }

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
        // Accelerate first, THEN cap - so the cap actually holds
        horivel = Accelerate(target, horivel, groundAccel, dt);

        if (_isCrouching)
            horivel = Vector2.ClampMagnitude(horivel, crouchSpeedMax);

        return horivel;
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
    public bool IsColliderValidForCollisions(Collider coll)
    {
        GruntController gruntController = coll.GetComponentInParent<GruntController>();
        if (gruntController != null && gruntController.IsRagdolling)
        {
            return false;
        }

        return _dashAttack == null || _dashAttack.IsColliderValidForCollisions(coll);
    }

    void OnGUI()
    {
        if (drawDebugTargetGui) _dashAttack?.DrawDebugGUI();
    }
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

        SwordController.Instance.StartSwingHitbox();

        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(swordSwing);
        Debug.Log("Attack function completed.");
    }

    public void PlaySwordHitSound()
    {
        if (hitSound == null) return;
        
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(hitSound);
    }

    public void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
        SwordController.Instance.StopSwingHitbox();
    }

    public void ResetAttackAnimation()
    {
        PlayerAnimator.Play("Armature|SwordHold");
        PlayerAnimator.ResetTrigger("SwingTrigger");
    }

    public void Die()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        GetComponent<KinematicCharacterMotor>().enabled = false;

        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Suspend()
    {
        // Nullify inputs and unlock mouse for menu pressing
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void Unsuspend()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }
}

