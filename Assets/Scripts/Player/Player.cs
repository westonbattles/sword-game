using UnityEngine;
using KinematicCharacterController;
using System;
using UnityEngine.InputSystem;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class Player : MonoBehaviour, ICharacterController
{
	[Header("Gameplay")] 
	[SerializeField] bool autoBhop;
	
    [Header("Movement")]
    [SerializeField] public Camera mainCamera;
    [SerializeField] public float gravity = 40f;
	[SerializeField] public float gravityMultiplier = 1.025f;
	[SerializeField] float groundAccel  = 130f;
	[SerializeField] float airAccel = 50f;
	[SerializeField] float airSpeed = 1f;
	[SerializeField] float friction = 12f;
	[SerializeField] float jumpHeight = 12f;
    [SerializeField] float jumpBufferTime = 0.2f;
    
    [Header("Dash")]
    [SerializeField] public float dashSpeed = 10f;
    
    public event Action OnJump;
    public event Action OnLand;
    
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

    void Awake()
    {
	    Instance = this;
        _motor = GetComponent<KinematicCharacterMotor>();
        if (autoBhop) jumpBufferTime = 0.01f; // jump buffer with auto bhop feels bad
    }

    void Start()
    {
        _motor.CharacterController = this;
    }

    void Update()
    {
        UpdateInput();
    }

    void UpdateInput()
    {
        _moveInput = InputSystem.actions["Move"].ReadValue<Vector2>(); 
        _jumpInput = autoBhop ? InputSystem.actions["Jump"].IsPressed() : InputSystem.actions["Jump"].WasPressedThisFrame();
        _inputRot  = mainCamera.transform.rotation;
        if (_jumpInput)
            _jumpBufferCounter = jumpBufferTime;
        else
            _jumpBufferCounter -= Time.deltaTime;
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
    
    // TODO - dash vector starts at eyes not at feet
    /// Performs dash in a given direction with given speed
    public void Dash(Vector3 directionNormalized, float speed) {
	    _dashVelocity = directionNormalized * speed;
	    _shouldDash = true;
	    // TODO maybe figure out how to implement this in a way that adjusts based on if you dash straight into the ground vs along it
	    _motor.ForceUnground(.25f); // lets you dash along objects without insta stopping you
    }

	void VelocitySet(ref Vector3 currentVelocity, float dt)
	{
		if (_shouldDash) {
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
			horivel *= Mathf.Max(speed - drop,0f)/ speed;
		}
		return Accelerate(target,horivel,groundAccel, dt);
	}

	Vector2 MoveAir(Vector2 target, Vector2 horivel, float dt)
	{
		return Accelerate(target,horivel,airAccel,dt);
	}

	Vector2 Accelerate(Vector2 target, Vector2 horivel, float acceleration, float dt)
	{
		float accelVelocity = acceleration * dt;
		float projectedVelocity = Vector2.Dot(target,horivel);
		if (!_motor.GroundingStatus.IsStableOnGround && projectedVelocity >= airSpeed)
		{
			return horivel;
		}
		else
		{
			return horivel + (target * accelVelocity);
		}
	}

    public void BeforeCharacterUpdate(float deltaTime){}
    public void AfterCharacterUpdate(float deltaTime){}

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

    public bool IsColliderValidForCollisions(Collider coll){return true;}
    public void OnDiscreteCollisionDetected(Collider hitCollider){}
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport){}
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport){}
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport){}
}
