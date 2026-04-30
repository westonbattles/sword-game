using System;
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;


namespace Sword
{
    [RequireComponent(typeof(Rigidbody))]
    public class SwordController : MonoBehaviour
    {
        
        [SerializeField] float throwSpeed = 10f;
        [SerializeField] float maxThrowDistance = 5f;
        [SerializeField] GameObject holdPoint;
        [SerializeField] Transform cameraTransform;

        private Rigidbody _rigidbody;
        private KinematicCharacterMotor _playerMotor;
        private bool _isHeld; // either held or being thrown
        private bool _shouldTriggerPlayerDash;
        private float _maxThrowTime;
        private float _throwTime;
        private Vector3 _throwDirection;
        private Vector3 _dashDirection;
        private bool _wasGroundedAtThrow;
        
        private bool _throwInput;
        


        private void Start()
        {
            _playerMotor = Player.Instance.Motor;
            _maxThrowTime = maxThrowDistance/throwSpeed;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _isHeld = true;
            _shouldTriggerPlayerDash = false;
        }

        private void Update()
        {
            _throwInput = InputSystem.actions["Throw"].WasPressedThisFrame();
            if (_throwInput && _isHeld && HealthSystem.Instance.manaPoint >= 20) { Throw(); }
            else if (!_isHeld) { CheckIfShouldDash(); }
        }

        private void FixedUpdate()                                                                                  
        {
            // Gravity only affects the sword when we're in the air for more arcadey/synced sword thorws
            if (_isHeld || _rigidbody.isKinematic || _wasGroundedAtThrow) return;
            _rigidbody.AddForce(Vector3.down * Player.Instance.gravity, ForceMode.Acceleration);
        }

        private void Throw()
        {
            _isHeld = false;
            _throwTime = Time.time;
            _wasGroundedAtThrow = Player.Instance.IsGrounded;
            
            
            Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * maxThrowDistance;
            Vector3 throwDir = (aimPoint - transform.position).normalized;
            _throwDirection = throwDir;
            _dashDirection = cameraTransform.forward;
            
            // TODO - smoother rotation
           // transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(throwDir), 360) * Quaternion.Euler(90, 0, 0);;
            
            
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _shouldTriggerPlayerDash = true;
            transform.parent = null;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = (throwDir * throwSpeed) + _playerMotor.Velocity;
        }
        private void CheckIfShouldDash()
        {
            bool throwExceededMaxDistance = getDistFromCamera() >= maxThrowDistance;
            bool throwExceededMaxTime = Time.time - _throwTime >= _maxThrowTime;
            bool swordGettingCloserToPlayer = Vector3.Dot(transform.position - Player.Instance.mainCamera.transform.position, _rigidbody.linearVelocity - Player.Instance.Motor.Velocity) < 0;
            
            // If an external force (ie, hitting the ground after falling) stops us right after throwing the sword
            // theres a chance it'll start getting closer to us before the dash gets triggered
            // which is why we add this swordGettingCloserToPlayer check
            bool shouldDash = _shouldTriggerPlayerDash &&
                              (throwExceededMaxDistance || throwExceededMaxTime || swordGettingCloserToPlayer);
            
            if (shouldDash)
            {
                Debug.Log($"DASH: distExceeded={throwExceededMaxDistance} timeExceeded={throwExceededMaxTime} closer={swordGettingCloserToPlayer}"); 
                _rigidbody.isKinematic = true;
                Player.Instance.Dash(_dashDirection, Player.Instance.dashSpeed);
                _shouldTriggerPlayerDash = false;
                Catch(); // give the sword back to the player
            }
        }

        private void Catch()
        {
            _isHeld = true;
            
            // just tps sword back to player,
            // TODO: cool animation / sword movement to return to player
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            transform.SetParent(holdPoint.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        
        private float getDistFromCamera()
        {
            return (transform.position - Player.Instance.mainCamera.transform.position).magnitude;
        }
    }
}
