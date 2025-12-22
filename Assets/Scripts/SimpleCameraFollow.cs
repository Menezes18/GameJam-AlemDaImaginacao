using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCameraFollow : MonoBehaviour
{
    #region Inspector
    
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float verticalSmoothSpeed = 2f;
    
    public enum CameraPosition
    {
        Behind,
        Front,
        Left,
        Right,
        Custom
    }
    
    [Header("Camera Position")]
    [SerializeField] private CameraPosition cameraPosition = CameraPosition.Behind;
    [SerializeField] private float height = 1.25f;
    [SerializeField] private float distance = 7f;
    
    [Range(-180f, 180f)]
    [SerializeField] private float horizontalAngle = 0f;
    
    [Range(-45f, 45f)]
    [SerializeField] private float verticalAngle = 0f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float normalDistance = 7f;
    [SerializeField] private float zoomOutDistance = 12f;
    [SerializeField] private float zoomSpeed = 3f;
    
    [Header("Input")]
    [SerializeField] private InputActionReference toggleZoomAction;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerScript playerScript;
    
    #endregion
    
    #region Private
    
    private bool isZoomedOut = false;
    private float currentDistance;
    private Vector3 currentOffset;
    private float targetY;
    private bool wasGrounded = true;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        if (playerScript == null && target != null)
        {
            playerScript = target.GetComponent<PlayerScript>();
        }
        
        currentDistance = distance;
        UpdateCameraOffset();
        targetY = transform.position.y;
    }
    
    private void OnEnable()
    {
        if (toggleZoomAction != null)
        {
            toggleZoomAction.action.performed += ToggleZoom;
        }
    }
    
    private void OnDisable()
    {
        if (toggleZoomAction != null)
        {
            toggleZoomAction.action.performed -= ToggleZoom;
        }
    }
    
    private void Start()
    {
        if (target != null)
        {
            UpdateCameraOffset();
            Vector3 targetPos = CalculateCameraPosition(target.position);
            transform.position = targetPos;
        }
        
        targetY = transform.position.y;
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        UpdateZoom();
        UpdateOffset();
        FollowTarget();
    }
    
    #endregion
    
    #region Camera Follow
    
    private void FollowTarget()
    {
        UpdateCameraOffset();
        
        bool isGrounded = IsPlayerGrounded();
        
        Vector3 desiredPosition = CalculateCameraPosition(target.position);
        
        if (isGrounded)
        {
            targetY = desiredPosition.y;
            wasGrounded = true;
        }
        else if (wasGrounded)
        {
        }
        
        desiredPosition.y = Mathf.Lerp(transform.position.y, targetY, verticalSmoothSpeed * Time.deltaTime);
        
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        Vector3 lookTarget = target.position + Vector3.up * height * 0.5f;
        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
    }
    
    private void UpdateCameraOffset()
    {
        float angle = 0f;
        
        switch (cameraPosition)
        {
            case CameraPosition.Behind:
                angle = 0f;
                break;
            case CameraPosition.Front:
                angle = 180f;
                break;
            case CameraPosition.Left:
                angle = -90f;
                break;
            case CameraPosition.Right:
                angle = 90f;
                break;
            case CameraPosition.Custom:
                angle = horizontalAngle;
                break;
        }
        
        float angleRad = angle * Mathf.Deg2Rad;
        
        float x = Mathf.Sin(angleRad) * currentDistance;
        float z = Mathf.Cos(angleRad) * currentDistance;
        
        currentOffset = new Vector3(x, height, z);
    }
    
    private Vector3 CalculateCameraPosition(Vector3 playerPosition)
    {
        return playerPosition + currentOffset;
    }
    
    private bool IsPlayerGrounded()
    {
        if (playerScript == null) return true;
        
        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller != null)
        {
            return controller.isGrounded;
        }
        
        return true;
    }
    
    #endregion
    
    #region Zoom
    
    private void UpdateZoom()
    {
        float targetDistance = isZoomedOut ? zoomOutDistance : normalDistance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);
    }
    
    private void UpdateOffset()
    {
        float baseDistance = distance;
        float zoomDistance = isZoomedOut ? zoomOutDistance : normalDistance;
        currentDistance = baseDistance + (zoomDistance - normalDistance);
    }
    
    private void ToggleZoom(InputAction.CallbackContext context)
    {
        isZoomedOut = !isZoomedOut;
        Debug.Log($"📷 [CAMERA] Zoom {(isZoomedOut ? "OUT" : "IN")}");
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetZoomOut(bool zoomOut)
    {
        isZoomedOut = zoomOut;
    }
    
    public void ToggleZoom()
    {
        isZoomedOut = !isZoomedOut;
    }
    
    public bool IsZoomedOut => isZoomedOut;
    
    #endregion
}
