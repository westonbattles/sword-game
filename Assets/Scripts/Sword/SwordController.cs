using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sword
{
    [RequireComponent(typeof(Rigidbody))]
    public class SwordController : MonoBehaviour
    {
        
        public float throwSpeed = 10f;
        public Transform cameraTransform;
        
        private Rigidbody _rigidbody;
        private bool _isHeld;
        private bool _throwInput;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _isHeld = true;
        }

        private void Update()
        {
            _throwInput = InputSystem.actions["Throw"].WasPressedThisFrame();
            if (_throwInput && _isHeld) {Throw();}
        }

        private void FixedUpdate()
        {
            
        }

        private void Throw()
        {
            _isHeld = false;
            transform.parent = null;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = cameraTransform.forward * throwSpeed;
        }
    }
}
