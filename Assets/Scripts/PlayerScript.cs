using UnityEngine;

public enum PlayerState
{
    Default,
    Ascend,
    Descend,
    Stagger,
    Roll,
    Death,
    Sleeping,
}

public enum PlayerStatus
{
    Default,
    PickingUp,
    Analyzing,
    Interacting,
}

public class PlayerScript : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("References")]
    [SerializeField] private DatabasePlayer db;
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerControlsSO PlayerControlsSO;
    [SerializeField] private Transform playerModel; // Transform do modelo visual do player (para rotacionar ao deitar)
    [SerializeField] private Transform spawnPoint; // Ponto onde o player vai se posicionar ao dormir

    [Header("2.5D Camera System")]
    [SerializeField] private Transform _cam;
    
    [Header("2.5D Movement Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("State (Debug)")]
    [SerializeField] private PlayerState _state = PlayerState.Default;
    [SerializeField] private PlayerStatus _status = PlayerStatus.Default;
    
    [Header("Interaction")]
    [SerializeField] private LayerMask interactableLayerMask = -1;
    [SerializeField] private Transform itemHoldPoint;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private Transform interactionSphereCenter; 
    [SerializeField] private float interactionSphereRadius = 3f;
    [SerializeField] private bool showInteractionGizmo = true; 
    [SerializeField] private bool debugInteraction = false; 
    

    private float interactionRange => interactionSphereRadius;
    
    [Header("UI")]
    public bool panel;
    
    #endregion

    #region State Properties
    
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
    
    #endregion

    #region Movement Variables
    
    private Vector3 _input;
    private Vector3 _raw;
    private Vector3 _move;
    private Vector3 _inertia;
    private bool _ignoreGroundedNextFrame;
    private float _inertiaCap;
    
    private float InertiaCap
    {
        get => _inertiaCap;
        set => _inertiaCap = Mathf.Clamp(value, db != null ? db.playerSpeed : 0.5f, db != null ? db.playerMaxAirSpeed : 3f);
    }
    
    #endregion

    #region Roll Variables
    
    private Vector3 _rollDir = Vector3.forward;
    private float _rollTimer;
    private float _rollCooldown;
    
    #endregion

    #region Stagger Variables
    
    private float _staggerTimer;
    
    #endregion

    #region Animator Hashes
    
    private static readonly int _STATE = Animator.StringToHash("state");
    private static readonly int _STATUS = Animator.StringToHash("status");
    private static readonly int _MOVEX = Animator.StringToHash("MoveX");
    private static readonly int _MOVEY = Animator.StringToHash("MoveY");
    
    #endregion

    #region Interaction Variables
    
    private IInteractable _currentInteractable;
    private GameObject _heldObject;
    private float _pickUpTimer;
    private float _analyzeTimer;
    private const float PICKUP_DURATION = 0.5f;
    private const float ANALYZE_DURATION = 2f;
    
    #endregion

    #region Unity Lifecycle
    
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
        
        if (State == PlayerState.Sleeping)
        {
            UpdateAnimator();
            return;
        }
        
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
        UpdateCharacterRotation();
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
        // Sistema 2.5D: câmera fixa, não controla mais a rotação da câmera
        // Mantido apenas para compatibilidade com o sistema de input
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

        Vector3 dir = CalculateCameraRelativeMovement();
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }
        _rollDir = dir.normalized;
        _rollTimer = db != null ? db.playerRollDuration : 0.35f;
        State = PlayerState.Roll;
    }
    
    #endregion

    #region 2.5D Movement System
    
    private Vector3 CalculateCameraRelativeMovement()
    {
        if (_cam == null)
        {
            return transform.TransformDirection(_input);
        }
        
        Vector3 cameraForward = _cam.forward;
        Vector3 cameraRight = _cam.right;
        
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 moveDirection = (cameraForward * _input.z) + (cameraRight * _input.x);
        
        if (moveDirection.magnitude > 0.01f)
        {
            moveDirection.Normalize();
        }
        
        return moveDirection;
    }
    
    private void UpdateCharacterRotation()
    {
        if (State == PlayerState.Sleeping) return;
        
        Vector3 moveDirection = CalculateCameraRelativeMovement();
        
        if (moveDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    #endregion

    #region Movement Core
    
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

    #region Movement Behaviours
    
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
        Vector3 dir = CalculateCameraRelativeMovement();
        float spd = db.playerSpeed;
        _move = dir * spd;
        _move.y = vertical;
        _move += Vector3.up * db.gravity;
    }

    private void AerialBehaviour()
    {
        if (State != PlayerState.Ascend && State != PlayerState.Descend) return;

        float vertical = _move.y;
        Vector3 moveDirection = CalculateCameraRelativeMovement();
        float airSpd = (db != null ? db.playerAirSpeed : 2.5f) * Time.deltaTime;
        _inertia += moveDirection * airSpd;
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
        Vector3 moveDirection = CalculateCameraRelativeMovement();

        if (_staggerTimer > 0f) moveDirection = Vector3.zero;

        float airSpeed = (db != null ? db.playerAirSpeed : 2.5f) * 0.6f;
        moveDirection *= airSpeed * Time.deltaTime;

        _inertia += moveDirection;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;

        if (_staggerTimer > 0f) return;
        if (_controller != null && !_controller.isGrounded) return;
        State = PlayerState.Default;
    }
    
    #endregion

    #region Animation
    
    private void UpdateAnimator()
    {
        if (_animator != null)
        {
            _animator.SetFloat(_MOVEX, _input.x, 0.1f, Time.deltaTime);
            _animator.SetFloat(_MOVEY, _input.z, 0.1f, Time.deltaTime);
        }
    }
    
    #endregion

    #region State Management
    
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

    public void RespawnPlayer()
    {
        if (spawnPoint != null)
        {
            _controller.enabled = false;
            transform.position = spawnPoint.position;
            _controller.enabled = true;
            State = PlayerState.Default;
            Status = PlayerStatus.Default;
            _move = Vector3.zero;
            _inertia = Vector3.zero;
            WorldManager.Instance.ForceChangeToRealWorld();
            Debug.Log("✅ [RESPAWN] Player respawned at spawn point.");
        }
        else
        {
            Debug.LogWarning("⚠️ [RESPAWN] Spawn point not set. Cannot respawn player.");
        }
    }
    
    #endregion

    #region Interaction System
    
    private void DetectInteractables()
    {
        if (_cam == null) return;
        if (Status != PlayerStatus.Default) return;

        Vector3 sphereCenter = GetInteractionSphereCenter();
        
        Collider[] colliders = Physics.OverlapSphere(sphereCenter, interactionSphereRadius, interactableLayerMask);
        
        if (debugInteraction)
        {
            Debug.Log($"🔍 [DETECT] OverlapSphere encontrou {colliders.Length} colliders na posição {sphereCenter} com raio {interactionSphereRadius}");
        }
        
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            if (col == null || col.gameObject == null) continue;
            
            if (debugInteraction)
            {
                bool isInMask = ((1 << col.gameObject.layer) & interactableLayerMask.value) != 0;
                Debug.Log($"🔍 [DETECT] Verificando collider: {col.gameObject.name} (Layer: {LayerMask.LayerToName(col.gameObject.layer)}, InMask: {isInMask})");
            }
            
            if (col.isTrigger)
            {
                if (col.transform.parent == null || col.transform.parent.GetComponent<IInteractable>() == null)
                {
                    continue;
                }
            }
            
            IInteractable interactable = FindInteractableInHierarchy(col.transform);
            
            if (interactable != null)
            {
                if (debugInteraction)
                {
                    Debug.Log($"✅ [DETECT] Encontrou IInteractable: {col.gameObject.name}, CanInteract: {interactable.CanInteract()}");
                }
                
                if (interactable.CanInteract())
                {
                    float distance = Vector3.Distance(sphereCenter, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }
            else if (debugInteraction)
            {
                Debug.LogWarning($"⚠️ [DETECT] Collider {col.gameObject.name} não tem IInteractable. Verifique se tem DoorInteraction, WindowInteraction, BedInteraction, etc.");
            }
        }
        
        // Se não encontrou com OverlapSphere, tenta Raycast como fallback
        if (closestInteractable == null)
        {
            Vector3 origin = raycastOrigin != null ? raycastOrigin.position : _cam.position;
            Vector3 direction = _cam.forward;
            
            if (debugInteraction)
            {
                Debug.Log($"🔍 [DETECT] Tentando Raycast de {origin} na direção {direction} com range {interactionSphereRadius}");
            }
            
            if (Physics.Raycast(origin, direction, out RaycastHit hit, interactionSphereRadius, interactableLayerMask))
            {
                if (debugInteraction)
                {
                    Debug.Log($"✅ [DETECT] Raycast hit: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                }
                
                IInteractable interactable = FindInteractableInHierarchy(hit.transform);
                
                if (interactable != null && interactable.CanInteract())
                {
                    closestInteractable = interactable;
                    if (debugInteraction)
                    {
                        Debug.Log($"✅ [DETECT] Raycast encontrou interagível: {hit.collider.gameObject.name}");
                    }
                }
            }
            else if (debugInteraction)
            {
                Debug.Log($"❌ [DETECT] Raycast não encontrou nada");
            }
        }
        
        _currentInteractable = closestInteractable;
        
        if (debugInteraction && _currentInteractable == null)
        {
            Debug.LogWarning($"⚠️ [DETECT] Nenhum interagível encontrado. Verifique LayerMask (valor: {interactableLayerMask.value}) e se os objetos têm IInteractable");
        }
    }
    

    private IInteractable FindInteractableInHierarchy(Transform target)
    {
        if (target == null) return null;
        
        IInteractable interactable = target.GetComponent<IInteractable>();
        if (interactable != null)
        {
            if (debugInteraction)
                Debug.Log($"✅ [FIND] Encontrou IInteractable no próprio objeto: {target.name}");
            return interactable;
        }
        
        if (target.parent != null)
        {
            interactable = target.parent.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (debugInteraction)
                    Debug.Log($"✅ [FIND] Encontrou IInteractable no parent: {target.parent.name}");
                return interactable;
            }
        }
        
        interactable = target.GetComponentInChildren<IInteractable>();
        if (interactable != null)
        {
            if (debugInteraction)
                Debug.Log($"✅ [FIND] Encontrou IInteractable nos children: {interactable.GetType().Name}");
            return interactable;
        }
        
        Transform current = target;
        while (current.parent != null)
        {
            current = current.parent;
            interactable = current.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (debugInteraction)
                    Debug.Log($"✅ [FIND] Encontrou IInteractable no ancestor: {current.name}");
                return interactable;
            }
        }
        
        return null;
    }
    

    private Vector3 GetInteractionSphereCenter()
    {
        if (interactionSphereCenter != null)
        {
            return interactionSphereCenter.position;
        }
        
        Vector3 forward = _cam != null ? _cam.forward : transform.forward;
        forward.y = 0f; 
        forward.Normalize();
        
        return transform.position + forward * (interactionSphereRadius * 0.5f);
    }

    private void OnPickUp()
    {
        if (panel) return;
        if (State == PlayerState.Death || State == PlayerState.Stagger) return;

        if (State == PlayerState.Sleeping)
        {
            if (_currentInteractable != null && _currentInteractable.CanInteract())
            {
                MonoBehaviour bedObj = _currentInteractable as MonoBehaviour;
                string bedName = bedObj != null ? bedObj.gameObject.name : "Desconhecido";
                Debug.Log($"🛏️ [INTERACT] Tentando acordar interagindo com: {bedName}");
                _currentInteractable.OnInteract(this);
            }
            return;
        }
        
        if (_heldObject != null)
        {
            Debug.Log("🔄 [PICKUP] Já está segurando objeto, soltando...");
            DropHeldObject();
            return;
        }

        if (Status != PlayerStatus.Default) return;
        if (_currentInteractable == null)
        {
            Debug.Log("⚠️ [INTERACT] Nenhum objeto interagível detectado");
            return;
        }
        
        if (_currentInteractable.CanPickUp())
        {
            IPickable pickable = _currentInteractable as IPickable;
            if (pickable != null)
            {
                HandlePickUp(pickable);
                return;
            }
        }
        
        MonoBehaviour interactableObj = _currentInteractable as MonoBehaviour;
        string objName = interactableObj != null ? interactableObj.gameObject.name : "Desconhecido";
        Debug.Log($"🎮 [INTERACT] Interagindo com: {objName}");
        _currentInteractable.OnInteract(this);
    }
    

    private void HandlePickUp(IPickable pickable)
    {
        Debug.Log($"🎯 [PICKUP] Objeto detectado: {(pickable as MonoBehaviour)?.gameObject.name}");
        
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
        if (obj == null || itemHoldPoint == null) return;

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
        }

        PickableAndAnalyzableItem comboItem = _heldObject.GetComponent<PickableAndAnalyzableItem>();
        if (comboItem != null)
        {
            comboItem.OnDrop();
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
    
    public void MoveToPosition(Vector3 position, Quaternion rotation)
    {
        if (_controller != null)
        {
            _controller.enabled = false; 
            transform.position = position;
            transform.rotation = rotation;
            _controller.enabled = true;
        }
        else
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
    

    public void SetLyingDown(bool lyingDown)
    {
        Transform modelToRotate = playerModel;
        
              
        if (modelToRotate != null)
        {
            if (lyingDown)
            {
                Quaternion currentRot = modelToRotate.localRotation;
                modelToRotate.localRotation = Quaternion.Euler(90f, currentRot.eulerAngles.y, currentRot.eulerAngles.z);
            }
            else
            {
                Quaternion currentRot = modelToRotate.localRotation;
                modelToRotate.localRotation = Quaternion.Euler(0f, currentRot.eulerAngles.y, currentRot.eulerAngles.z);
            }
        }
    }
    
    #endregion

    #region Debug Gizmos
    
    private void OnDrawGizmos()
    {
        if (!showInteractionGizmo) return;
        
        Vector3 sphereCenter = GetInteractionSphereCenter();
        
        if (_currentInteractable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(sphereCenter, interactionSphereRadius);
            
            MonoBehaviour interactableObj = _currentInteractable as MonoBehaviour;
            if (interactableObj != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(sphereCenter, interactableObj.transform.position);
            }
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(sphereCenter, interactionSphereRadius);
        }
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, sphereCenter);
        
        if (_cam != null)
        {
            Vector3 origin = raycastOrigin != null ? raycastOrigin.position : _cam.position;
            Vector3 direction = _cam.forward;
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(origin, direction * interactionSphereRadius);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showInteractionGizmo) return;
        
        Vector3 sphereCenter = GetInteractionSphereCenter();
        
        if (_currentInteractable != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(sphereCenter, interactionSphereRadius);
            
            MonoBehaviour interactableObj = _currentInteractable as MonoBehaviour;
            if (interactableObj != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(sphereCenter, interactableObj.transform.position);
                
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawCube(interactableObj.transform.position, Vector3.one * 0.3f);
            }
        }
        else
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(sphereCenter, interactionSphereRadius);
        }
        
        Gizmos.color = _currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(sphereCenter, interactionSphereRadius);
    }
    
    #endregion
}
