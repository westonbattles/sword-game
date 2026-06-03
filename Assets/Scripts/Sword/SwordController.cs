using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;


namespace Sword
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class SwordController : MonoBehaviour
    {
        const string HeldRenderLayerName = "Player";
        const string WorldRenderLayerName = "Default";

        public static SwordController Instance { get; private set; }

        [Header("References")]
        [SerializeField] GameObject holdPoint;
        [SerializeField] Transform cameraTransform;

        [Header("Throw")]
        [SerializeField] float throwSpeed = 10f;
        [SerializeField] float maxThrowDistance = 5f;
        [SerializeField] AudioClip[] throwSounds = System.Array.Empty<AudioClip>();
        [SerializeField, Range(0.5f, 1.5f)] float throwPitchMin = 0.95f;
        [SerializeField, Range(0.5f, 1.5f)] float throwPitchMax = 1.05f;

        [Header("Swing")]
        [SerializeField] LayerMask enemyLayer;
        [SerializeField] int swingDamage = 5;

        public bool IsHeld { get; private set; } // either held or being thrown
        public BoxCollider BoxCollider { get; private set; }

        private Rigidbody _rigidbody;
        private KinematicCharacterMotor _playerMotor;
        private AudioSource _audioSource;

        private readonly HashSet<Actor> _hitActorsThisSwing = new();

        private bool _throwInput;
        private bool _shouldTriggerPlayerDash;
        private bool _wasGroundedAtThrow;

        private float _maxThrowTime;
        private float _throwTime;

        private Vector3 _dashDirection;
        private int _defaultLayer;
        private int _heldRenderLayer;

        private void Start()
        {
            _playerMotor = Player.Instance.Motor;
            _maxThrowTime = maxThrowDistance/throwSpeed;
        }

        private void Awake()
        {
            Instance = this;
            IsHeld = true;
            BoxCollider = GetComponent<BoxCollider>();
            _rigidbody = GetComponent<Rigidbody>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _shouldTriggerPlayerDash = false;
            _heldRenderLayer = LayerMask.NameToLayer(HeldRenderLayerName);
            int worldRenderLayer = LayerMask.NameToLayer(WorldRenderLayerName);
            _defaultLayer = worldRenderLayer >= 0 ? worldRenderLayer : gameObject.layer;
            SetHeldRenderLayer(true);
            
            BoxCollider.isTrigger = true;
            BoxCollider.enabled = false;
        }

        private void Update()
        {
            _throwInput = InputSystem.actions["Throw"].WasPressedThisFrame();
            if (_throwInput && IsHeld && HealthSystem.Instance.manaPoint >= 20)
            {
                if (Player.Instance.attacking)
                {
                    Player.Instance.ResetAttack();
                    Player.Instance.ResetAttackAnimation();
                    StartCoroutine(SkipFramesAndThrow(1));
                }
                else
                {
                    Throw();
                }
            }
            else if (!IsHeld) { CheckIfShouldDash(); }
        }

        private void FixedUpdate()                                                                                  
        {
            // Gravity only affects the sword when we're in the air for more arcadey/synced sword thorws
            if (IsHeld || _rigidbody.isKinematic || _wasGroundedAtThrow) return;
            _rigidbody.AddForce(Vector3.down * Player.Instance.gravity, ForceMode.Acceleration);
        }

        public void StartSwingHitbox()
        {
            _hitActorsThisSwing.Clear();
            BoxCollider.enabled = true;
        }

        public void StopSwingHitbox()
        {
            BoxCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            Actor hitActor = other.GetComponentInParent<Actor>();
            if (hitActor == null) return;
            if (!IsInLayerMask(hitActor.gameObject.layer, enemyLayer)) return;

            if (!_hitActorsThisSwing.Add(hitActor)) return;
            Debug.Log($"Swing hit: {hitActor.name}");
            hitActor.TakeDamage(swingDamage);
            Player.Instance.PlaySwordHitSound();
        }

        void LateUpdate()
        {
            
        }   

        private void Throw()
        {
            
            IsHeld = false;
            PlayRandomSound(throwSounds, throwPitchMin, throwPitchMax);
            SetHeldRenderLayer(false);
            _throwTime = Time.time;
            _wasGroundedAtThrow = Player.Instance.IsGrounded;
            
            
            Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * maxThrowDistance;
            Vector3 throwDir = (aimPoint - transform.position).normalized;
            _dashDirection = cameraTransform.forward;
            
            // TODO - smoother rotation or like use an animation
           // transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(throwDir), 360) * Quaternion.Euler(90, 0, 0);;
            
            
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _shouldTriggerPlayerDash = true;
            transform.parent = null;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = (throwDir * throwSpeed) + _playerMotor.Velocity;
        }

        private IEnumerator SkipFramesAndThrow(int frames = 1)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
            
            Throw();

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
                Player.Instance.Dash(_dashDirection, Player.Instance.dashSpeed, false);
                _shouldTriggerPlayerDash = false;
                Catch(); // give the sword back to the player
            }
        }

        private void Catch()
        {
            IsHeld = true;
            
            // just tps sword back to player,
            // TODO: cool animation / sword movement to return to player
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            transform.SetParent(holdPoint.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            SetHeldRenderLayer(true);
        }
        
        private float getDistFromCamera()
        {
            return (transform.position - Player.Instance.mainCamera.transform.position).magnitude;
        }

        /// Return whether layer is included in layerMask.
        private static bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        private void SetHeldRenderLayer(bool held)
        {
            int targetLayer = held && _heldRenderLayer >= 0 ? _heldRenderLayer : _defaultLayer;
            SetLayerRecursively(transform, targetLayer);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;

            foreach (Transform child in root)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private void PlayRandomSound(AudioClip[] clips, float minPitch, float maxPitch)
        {
            if (_audioSource == null || clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;

            _audioSource.pitch = Random.Range(minPitch, maxPitch);
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = 1f;
        }
    }
}
