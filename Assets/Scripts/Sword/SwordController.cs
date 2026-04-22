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
            if (_throwInput && _isHeld) { Throw(); }
            else if (!_isHeld) { CheckIfShouldDash(); }
        }

        private void FixedUpdate()
        {
            
        }

        private void Throw()
        {
            _isHeld = false;
            _throwTime = Time.time;
            
            Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * maxThrowDistance;
            Vector3 throwDir = (aimPoint - transform.position).normalized;
            
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _shouldTriggerPlayerDash = true;
            transform.parent = null;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = (throwDir * throwSpeed) + _playerMotor.Velocity;
        }

        private void CheckIfShouldDash()
        {
            if (_shouldTriggerPlayerDash && (getDistFromCamera() >= maxThrowDistance) || (Time.time - _throwTime >= _maxThrowTime))
            {
                _rigidbody.isKinematic = true;
                Player.Instance.Dash((transform.position - Player.Instance.mainCamera.transform.position).normalized, Player.Instance.dashSpeed);
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
