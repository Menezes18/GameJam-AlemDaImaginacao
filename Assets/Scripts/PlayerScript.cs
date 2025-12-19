using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum PlayerState
{
    Default,
    Ascend,
    Descend,
    Stagger,
    Roll,
    Death,
}

public enum PlayerStatus
{
    Default,
    PickingUp,
    Analyzing,
}

public class PlayerScript : MonoBehaviour
{
    #region Inspector
    [Header("Refs")]
    [SerializeField] private DatabasePlayer db;
    [SerializeField] private CameraData cameraData;
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerControlsSO PlayerControlsSO;

    [Header("Camera")]
    [SerializeField] private bool controlCamera = false;
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform _firstPersonLocation;
    [SerializeField] private Vector3 cameraOffset => cameraData.cameraOffset;

    [Header("State")]
    [SerializeField] private PlayerState _state = PlayerState.Default;
    [SerializeField] private PlayerStatus _status = PlayerStatus.Default;
    
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayerMask = -1;
    [SerializeField] private Transform itemHoldPoint; // Ponto onde o objeto será segurado
    [SerializeField] private Transform raycastOrigin; // Ponto de origem do raycast (se null, usa a câmera)
    
    public bool panel;
    #endregion

    #region State Variables
    public PlayerState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            OnStateChanged(_state, value);
            _state = value;
            if (_animator != null) _animator.SetInteger(_STATE, (int)value);
        }
    }

    public PlayerStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            OnStatusChanged(_status, value);
            _status = value;
            if (_animator != null) _animator.SetInteger(_STATUS, (int)value);
        }
    }

    private Vector3 _input;
    private Vector3 _raw;
    private Vector3 _move;
    private Vector3 _inertia;
    private float _yaw;
    private float _pitch;
    private bool _ignoreGroundedNextFrame;
    private float _inertiaCap;
    
    private float InertiaCap
    {
        get => _inertiaCap;
        set => _inertiaCap = Mathf.Clamp(value, db != null ? db.playerSpeed : 0.5f, db != null ? db.playerMaxAirSpeed : 3f);
    }
    #endregion

    #region Roll
    private Vector3 _rollDir = Vector3.forward;
    private float _rollTimer;
    private float _rollCooldown;
    #endregion

    #region Stagger
    private float _staggerTimer;
    #endregion

    #region Animator
    private static readonly int _STATE = Animator.StringToHash("state");
    private static readonly int _STATUS = Animator.StringToHash("status");
    private static readonly int _MOVEX = Animator.StringToHash("MoveX");
    private static readonly int _MOVEY = Animator.StringToHash("MoveY");
    #endregion

    #region Interaction
    private IInteractable _currentInteractable;
    private GameObject _heldObject;
    private float _pickUpTimer;
    private float _analyzeTimer;
    private const float PICKUP_DURATION = 0.5f;
    private const float ANALYZE_DURATION = 2f;
    
    // Debug do raycast
    private RaycastHit _lastRaycastHit;
    private bool _lastRaycastValid = false;
    private bool _lastRaycastHasInteractable = false;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (_controller == null) _controller = GetComponent<CharacterController>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (PlayerControlsSO == null) return;
        PlayerControlsSO.OnMove += OnMove;
        PlayerControlsSO.OnLook += OnLook;
        PlayerControlsSO.OnJump += OnJump;
        PlayerControlsSO.OnRoll += OnRoll;
        PlayerControlsSO.OnPickUp += OnPickUp;
        PlayerControlsSO.OnAnalyze += OnAnalyze;
    }

    private void OnDisable()
    {
        if (PlayerControlsSO == null) return;
        PlayerControlsSO.OnMove -= OnMove;
        PlayerControlsSO.OnLook -= OnLook;
        PlayerControlsSO.OnJump -= OnJump;
        PlayerControlsSO.OnRoll -= OnRoll;
        PlayerControlsSO.OnPickUp -= OnPickUp;
        PlayerControlsSO.OnAnalyze -= OnAnalyze;
    }

    private void Update()
    {
        UpdateTimers();
        DetectInteractables();
        AerialDetection();

        PickUpBehaviour();
        AnalyzeBehaviour();
        StaggerBehaviour();
        RollBehaviour();
        AerialBehaviour();
        DefaultBehaviour();

        UpdateAnimator();
        ApplyGravity();
        MoveCharacter();

        transform.rotation = Quaternion.Euler(0, _yaw, 0);
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        UpdateCameraPosition();
    }
    #endregion

    #region Input Handlers
    private void OnMove(Vector2 input, Vector2 raw)
    {
        _raw = new Vector3(raw.x, 0f, raw.y);
        _input = new Vector3(input.x, 0f, input.y);
    }

    private void OnLook(Vector2 look)
    {
        if (panel) return;
        if (!controlCamera) return;

        float yawDelta = look.x * cameraData.sensitivityX;
        float pitchDelta = -look.y * cameraData.sensitivityY;

        _yaw += yawDelta;
        _pitch += pitchDelta;

        float minY = db != null ? db.minMouseY : -75f;
        float maxX = db != null ? db.maxMouseX : 75f;
        if (maxX <= minY)
        {
            minY = -75f;
            maxX = 75f;
        }
        _pitch = Mathf.Clamp(_pitch, minY, maxX);
    }

    private void OnJump()
    {
        if (panel) return;
        if (State != PlayerState.Default) return;

        State = PlayerState.Ascend;
        _ignoreGroundedNextFrame = true;
        _move.y = db != null ? db.playerJumpHeight : 6f;
        _inertia = new Vector3(_move.x, 0f, _move.z);
        InertiaCap = _inertia.magnitude;
    }

    private void OnRoll()
    {
        if (panel) return;
        if (State == PlayerState.Stagger || State == PlayerState.Death) return;
        if (_rollCooldown > 0f) return;
        if (State == PlayerState.Ascend || State == PlayerState.Descend) return;

        Vector3 dir = _input.sqrMagnitude > 0.0001f ? _input.normalized : Vector3.forward;
        if (_cam != null) dir = Quaternion.Euler(0f, _cam.eulerAngles.y, 0f) * dir;
        _rollDir = dir;
        _rollTimer = db != null ? db.playerRollDuration : 0.35f;
        State = PlayerState.Roll;
    }
    #endregion

    #region Movement
    private void UpdateTimers()
    {
        if (_rollTimer > 0f) _rollTimer -= Time.deltaTime;
        if (_rollCooldown > 0f) _rollCooldown -= Time.deltaTime;
        if (_staggerTimer > 0f) _staggerTimer -= Time.deltaTime;
        if (_pickUpTimer > 0f) _pickUpTimer -= Time.deltaTime;
        if (_analyzeTimer > 0f) _analyzeTimer -= Time.deltaTime;
    }

    private void ApplyGravity()
    {
        float gravity = db.gravity;
        _move += Vector3.up * gravity * Time.deltaTime;
    }

    private void MoveCharacter()
    {
        if (_controller == null || State == PlayerState.Death) return;

        _controller.Move(_move * Time.deltaTime);
        if (_controller.isGrounded)
        {
            float grounded = db.gravityGrounded;
            _move.y = grounded;
        }
    }
    #endregion

    #region Camera
    private void UpdateCameraPosition()
    {
        _cam.transform.position = _firstPersonLocation.position;
        _cam.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    #endregion

    #region Behaviours
    private void AerialDetection()
    {
        if (State == PlayerState.Death || State == PlayerState.Stagger || State == PlayerState.Roll) return;

        if (_move.y > 0f) State = PlayerState.Ascend;
        else if (_move.y < (db != null ? db.gravityGrounded : -1f)) State = PlayerState.Descend;

        if (_ignoreGroundedNextFrame)
        {
            _ignoreGroundedNextFrame = false;
            return;
        }

        if (_controller != null && _controller.isGrounded)
            State = PlayerState.Default;
    }

    private void DefaultBehaviour()
    {
        if (State != PlayerState.Default) return;

        float vertical = _move.y;
        Vector3 dir = _input;
        if (_cam != null) dir = Quaternion.Euler(0f, _cam.eulerAngles.y, 0f) * dir;
        float spd = db.playerSpeed;
        _move = dir * spd;
        _move.y = vertical;
        _move += Vector3.up * db.gravity;
    }

    private void AerialBehaviour()
    {
        if (State != PlayerState.Ascend && State != PlayerState.Descend) return;

        float vertical = _move.y;
        Vector3 input = new Vector3(_input.x, 0f, _input.z);
        if (_cam != null) input = Quaternion.Euler(0f, _cam.eulerAngles.y, 0f) * input;
        float airSpd = (db != null ? db.playerAirSpeed : 2.5f) * Time.deltaTime;
        _inertia += input * airSpd;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;
    }

    private void RollBehaviour()
    {
        if (State != PlayerState.Roll) return;

        float vertical = _move.y;
        float t = Mathf.Clamp01(_rollTimer / Mathf.Max(0.0001f, (db != null ? db.playerRollDuration : 0.35f)));
        float curve = db != null && db.playerRollCurve != null ? db.playerRollCurve.Evaluate(1f - t) : 1f - t;
        float speed = (db != null ? db.playerRollSpeed : 6f) * curve;

        Vector3 horizontal = _rollDir * speed;
        _move = new Vector3(horizontal.x, vertical, horizontal.z);

        if (_rollTimer <= 0f)
        {
            State = PlayerState.Default;
            _rollCooldown = db != null ? db.playerRollCooldownDuration : 0.5f;
        }
    }

    private void StaggerBehaviour()
    {
        if (State != PlayerState.Stagger) return;

        float vertical = _move.y;
        Vector3 input = new Vector3(_input.x, 0f, _input.z);
        if (_cam != null) input = Quaternion.Euler(0f, _cam.eulerAngles.y, 0f) * input;

        if (_staggerTimer > 0f) input = Vector3.zero;

        float airSpeed = (db != null ? db.playerAirSpeed : 2.5f) * 0.6f;
        input *= airSpeed * Time.deltaTime;

        _inertia += input;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;

        if (_staggerTimer > 0f) return;
        if (_controller != null && !_controller.isGrounded) return;
        State = PlayerState.Default;
    }
    #endregion

    #region Helpers
    private void UpdateAnimator()
    {
        if (_animator != null)
        {
            _animator.SetFloat(_MOVEX, _input.x, 0.1f, Time.deltaTime);
            _animator.SetFloat(_MOVEY, _input.z, 0.1f, Time.deltaTime);
        }
    }

    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        if (oldState == PlayerState.Roll)
        {
            _rollCooldown = db != null ? db.playerRollCooldownDuration : 0.5f;
        }
    }

    private void OnStatusChanged(PlayerStatus oldStatus, PlayerStatus newStatus)
    {
        Debug.Log($"🔄 [STATUS] {oldStatus} → {newStatus}");
    }
    #endregion

    #region Interaction System
    // Detecta objetos interagíveis na frente do jogador usando raycast
    // Origem: Transform do player (ou câmera se não definido)
    // Direção: guiada pela câmera
    private void DetectInteractables()
    {
        if (_cam == null) return;
        if (Status != PlayerStatus.Default) return;

        Vector3 origin = raycastOrigin != null ? raycastOrigin.position : _cam.position;
        Vector3 direction = _cam.forward;
        
        _lastRaycastValid = false;
        _lastRaycastHasInteractable = false;
        
        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactionRange, interactableLayerMask))
        {
            _lastRaycastHit = hit;
            _lastRaycastValid = true;
            
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                _currentInteractable = interactable;
                _lastRaycastHasInteractable = true;
                return;
            }
        }
        
        _currentInteractable = null;
    }

    private void OnPickUp()
    {
        if (panel) return;
        if (State == PlayerState.Death || State == PlayerState.Stagger) return;

        if (_heldObject != null)
        {
            Debug.Log("🔄 [PICKUP] Já está segurando objeto, soltando...");
            DropHeldObject();
            return;
        }

        if (Status != PlayerStatus.Default) return;
        
        if (_currentInteractable == null)
        {
            Debug.Log("⚠️ [PICKUP] Nenhum objeto interagível detectado");
            return;
        }
        
        Debug.Log($"🎯 [PICKUP] Objeto detectado: {(_currentInteractable as MonoBehaviour)?.gameObject.name}");
        
        if (!_currentInteractable.CanPickUp())
        {
            Debug.Log($"❌ [PICKUP] Objeto não pode ser pego (CanPickUp = false)");
            return;
        }

        IPickable pickable = _currentInteractable as IPickable;
        if (pickable == null)
        {
            Debug.Log("❌ [PICKUP] Objeto não implementa IPickable");
            return;
        }

        Status = PlayerStatus.PickingUp;
        _pickUpTimer = PICKUP_DURATION;
        pickable.OnPickUp(this);
        
        if (pickable.GetGameObject() != null)
        {
            _heldObject = pickable.GetGameObject();
            AttachObjectToPlayer(_heldObject);
            Debug.Log($"✅ [PICKUP] Objeto pegado com sucesso: {_heldObject.name}");
        }
    }

    private void OnAnalyze()
    {
        if (panel) return;
        if (Status != PlayerStatus.Default) return;
        if (State == PlayerState.Death || State == PlayerState.Stagger) return;

        if (_heldObject != null)
        {
            IAnalyzable heldAnalyzable = _heldObject.GetComponent<IAnalyzable>();
            if (heldAnalyzable != null && heldAnalyzable.CanAnalyze())
            {
                Status = PlayerStatus.Analyzing;
                _analyzeTimer = ANALYZE_DURATION;
                heldAnalyzable.OnAnalyze(this);
                return;
            }
        }

        if (_currentInteractable == null) return;
        if (!_currentInteractable.CanAnalyze()) return;

        IAnalyzable analyzable = _currentInteractable as IAnalyzable;
        if (analyzable == null) return;

        Status = PlayerStatus.Analyzing;
        _analyzeTimer = ANALYZE_DURATION;
        analyzable.OnAnalyze(this);
    }

    private void PickUpBehaviour()
    {
        if (Status != PlayerStatus.PickingUp) return;

        _input = Vector3.zero;

        if (_pickUpTimer <= 0f)
        {
            Status = PlayerStatus.Default;
            Debug.Log("✅ [PICKUP] Objeto pego com sucesso!");
        }
    }

    private void AnalyzeBehaviour()
    {
        if (Status != PlayerStatus.Analyzing) return;

        _input *= 0.3f;

        if (_analyzeTimer <= 0f)
        {
            Status = PlayerStatus.Default;
            
            IAnalyzable analyzable = null;
            if (_heldObject != null)
            {
                analyzable = _heldObject.GetComponent<IAnalyzable>();
            }
            else if (_currentInteractable is IAnalyzable)
            {
                analyzable = _currentInteractable as IAnalyzable;
            }
            
            if (analyzable != null)
            {
                analyzable.OnAnalyzeComplete(this);
            }
            
            Debug.Log("🔍 [ANALYZE] Análise concluída!");
        }
    }

    private void AttachObjectToPlayer(GameObject obj)
    {
        if (obj == null) return;
        
        if (itemHoldPoint == null)
        {
            
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        obj.transform.SetParent(itemHoldPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    public void DropHeldObject()
    {
        if (_heldObject == null) return;

        Debug.Log($"📦 [DROP] Soltando objeto: {_heldObject.name}");

        PickableItem pickableItem = _heldObject.GetComponent<PickableItem>();
        if (pickableItem != null)
        {
            pickableItem.OnDrop();
            Debug.Log($"✅ [DROP] OnDrop() chamado em PickableItem");
        }

        PickableAndAnalyzableItem comboItem = _heldObject.GetComponent<PickableAndAnalyzableItem>();
        if (comboItem != null)
        {
            comboItem.OnDrop();
            Debug.Log($"✅ [DROP] OnDrop() chamado em PickableAndAnalyzableItem");
        }

        _heldObject.transform.SetParent(null);
        
        Rigidbody rb = _heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        Collider col = _heldObject.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        if (rb != null && _cam != null)
        {
            rb.AddForce(_cam.forward * 3f, ForceMode.Impulse);
        }

        _heldObject = null;
        Status = PlayerStatus.Default;
    }

    public bool IsHoldingObject => _heldObject != null;
    #endregion

    #region Debug Gizmos
    // private void OnDrawGizmos()
    // {
    //     if (_cam == null) return;

    //     Vector3 origin = raycastOrigin != null ? raycastOrigin.position : _cam.position;
    //     Vector3 direction = _cam.forward;
        
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawRay(origin, direction * interactionRange);
        
    //     if (_lastRaycastValid)
    //     {
    //         Gizmos.color = _lastRaycastHasInteractable ? Color.green : Color.red;
    //         Gizmos.DrawRay(origin, direction * _lastRaycastHit.distance);
            
    //         Gizmos.color = _lastRaycastHasInteractable ? Color.green : Color.red;
    //         Gizmos.DrawWireSphere(_lastRaycastHit.point, 0.1f);
            
    //         Gizmos.color = Color.cyan;
    //         Gizmos.DrawRay(_lastRaycastHit.point, _lastRaycastHit.normal * 0.3f);
    //     }
        
    //     Gizmos.color = Color.white;
    //     Gizmos.DrawWireSphere(origin, 0.05f);
        
    //     if (raycastOrigin != null && _cam != null)
    //     {
    //         Gizmos.color = Color.magenta;
    //         Gizmos.DrawLine(origin, _cam.position);
    //     }
    // }
    #endregion

}
