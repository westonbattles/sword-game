using System;
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;


namespace Sword
{
    [RequireComponent(typeof(Rigidbody))]
    public class SwordController : MonoBehaviour
    {
        
        public float throwSpeed = 10f;
        public float reboundTime = 0.5f; // how far the sword can go before giving you boost
        public GameObject holdPoint;
        public Transform cameraTransform;
        
        private Rigidbody _rigidbody;
        private KinematicCharacterMotor _playerMoter;
        private bool _isHeld; // either held or being thrown
        private bool _throwInput;
        private float _throwTime;
        private bool _shouldTriggerPlayerDash;


        private void Start()
        {
            _playerMoter = Player.Instance.Motor;
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
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _shouldTriggerPlayerDash = true;
            _throwTime = Time.time;
            transform.parent = null;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = (cameraTransform.forward * throwSpeed) + _playerMoter.Velocity;
        }

        private void CheckIfShouldDash()
        {

            float currentTime = Time.time;
            if (currentTime >= reboundTime + _throwTime && _shouldTriggerPlayerDash)
            {
                _rigidbody.isKinematic = true;
                Player.Instance.DashTo(transform.position, Player.Instance.dashSpeed);
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
    }
}
