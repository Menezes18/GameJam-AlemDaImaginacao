using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Transform target;
    [SerializeField] private CameraData cameraData;
    
    [Header("Shake Test")]
    [SerializeField] private bool shakeTest = false;
    [Range(0f, 1f)] [SerializeField] private float shakeTestAmount = 0.4f;
    #endregion

    #region State
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float currentDistance;
    private float targetDistance;
    private float currentShoulderOffset = 1f;
    private float targetShoulderOffset = 1f;
    private float timeSinceLastInput = 0f;
    private bool isRecentering = false;
    private float trauma = 0f;
    private Vector3 shakeOffset = Vector3.zero;
    private Vector3 smoothedPosition;
    private Quaternion smoothedRotation;
    #endregion

    #region Dependencies
    private Camera cameraComponent;
    private Transform cameraTransform;
    private Vector3 cameraBaseLocalPosition;
    #endregion

    #region Input
    private Vector2 lookInput = Vector2.zero;
    #endregion

    #region Unity Callbacks
    private void OnValidate()
    {
        if (!Application.isPlaying && cameraData != null)
        {
            float d = Mathf.Clamp(cameraData.defaultDistance, cameraData.minDistance, cameraData.maxDistance);
            currentDistance = d;
            targetDistance = d;
        }
    }

    private void Awake()
    {
        EnsureCameraDataOrDisable();
        if (!enabled) return;

        InitializeCameraReferences();
        InitializeDistance();
        InitializeRotationFromTarget();
        
        smoothedPosition = transform.position;
        smoothedRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float deltaTime = Time.deltaTime;

        if (shakeTest)
        {
            TriggerShake(shakeTestAmount);
            shakeTest = false;
        }

        UpdateOrbit(deltaTime);
        UpdateShoulderOffset(deltaTime);
        UpdateDistance(deltaTime);
        UpdateShake(deltaTime);

        CalculateDesiredCameraTransform(out Vector3 desiredPosition, out Quaternion desiredRotation);
        desiredPosition = ApplyAntiClipping(desiredPosition);

        smoothedPosition = Vector3.Lerp(smoothedPosition, desiredPosition, cameraData.positionSmoothSpeed * deltaTime);
        smoothedRotation = Quaternion.Slerp(smoothedRotation, desiredRotation, cameraData.rotationSmoothSpeed * deltaTime);

        transform.position = smoothedPosition;
        transform.rotation = smoothedRotation;

        ApplyShakeToCamera();
    }
    #endregion

    #region Public API
    public void SetLookInput(Vector2 input) 
    { 
        lookInput = input; 
    }

    public void SetShoulderRight(bool right) 
    { 
        targetShoulderOffset = right ? 1f : -1f; 
    }

    public void SetDistance(float distance)
    {
        float d = cameraData != null
            ? Mathf.Clamp(distance, cameraData.minDistance, cameraData.maxDistance)
            : Mathf.Max(0.1f, distance);
        currentDistance = d;
        targetDistance = d;
    }

    public void AdjustDistance(float delta)
    {
        float desired = targetDistance + delta;
        float d = cameraData != null
            ? Mathf.Clamp(desired, cameraData.minDistance, cameraData.maxDistance)
            : Mathf.Max(0.1f, desired);
        targetDistance = d;
    }

    public void TriggerShake(float amount) 
    { 
        trauma = Mathf.Clamp01(trauma + Mathf.Abs(amount)); 
    }
    #endregion

    #region Orbit
    private void UpdateOrbit(float deltaTime)
    {
        float deltaYaw = lookInput.x * cameraData.sensitivityX;
        float deltaPitch = lookInput.y * cameraData.sensitivityY * (cameraData.invertY ? 1f : -1f);

        currentYaw += deltaYaw;
        currentPitch += deltaPitch;
        currentPitch = Mathf.Clamp(currentPitch, cameraData.minPitch, cameraData.maxPitch);

        if (cameraData.autoRecenter)
        {
            UpdateAutoRecenter(deltaTime);
        }
    }

    private void UpdateAutoRecenter(float deltaTime)
    {
        if (lookInput.sqrMagnitude > 0.01f)
        {
            timeSinceLastInput = 0f;
            isRecentering = false;
        }
        else
        {
            timeSinceLastInput += deltaTime;
            if (timeSinceLastInput >= cameraData.recenterDelay)
                isRecentering = true;
        }

        if (isRecentering)
        {
            float targetYaw = target.eulerAngles.y;
            float yawDifference = Mathf.DeltaAngle(currentYaw, targetYaw);
            float recenterAmount = cameraData.recenterSpeed * deltaTime;

            if (Mathf.Abs(yawDifference) < recenterAmount)
            {
                currentYaw = targetYaw;
                isRecentering = false;
            }
            else
            {
                currentYaw += Mathf.Sign(yawDifference) * recenterAmount;
            }
        }
    }
    #endregion

    #region Camera
    private void UpdateShoulderOffset(float deltaTime)
    {
        currentShoulderOffset = Mathf.Lerp(currentShoulderOffset, targetShoulderOffset, cameraData.shoulderSwitchSpeed * deltaTime);
    }

    private void UpdateDistance(float deltaTime)
    {
        if (cameraData == null) return;
        targetDistance = Mathf.Clamp(targetDistance, cameraData.minDistance, cameraData.maxDistance);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, cameraData.distanceSmoothSpeed * deltaTime);
    }

    private void CalculateDesiredCameraTransform(out Vector3 position, out Quaternion rotation)
    {
        rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 pivotPoint = target.position + Vector3.up * cameraData.shoulderOffsetY;
        Vector3 shoulderOffset = rotation * Vector3.right * (cameraData.shoulderOffsetX * currentShoulderOffset);
        Vector3 targetPosition = pivotPoint + shoulderOffset - (rotation * Vector3.forward * currentDistance);
        targetPosition += rotation * cameraData.cameraOffset;
        position = targetPosition;
    }

    private Vector3 ApplyAntiClipping(Vector3 desiredPosition)
    {
        Vector3 pivotPoint = target.position + Vector3.up * cameraData.shoulderOffsetY;
        Vector3 direction = desiredPosition - pivotPoint;
        float desiredDistance = direction.magnitude;

        if (Physics.SphereCast(pivotPoint, cameraData.clipRadius, direction.normalized, out RaycastHit hit, desiredDistance, cameraData.clipMask, QueryTriggerInteraction.Ignore))
        {
            float clippedDistance = hit.distance - cameraData.clipRadius * 0.5f;
            clippedDistance = Mathf.Max(clippedDistance, 0.2f);
            return pivotPoint + direction.normalized * clippedDistance;
        }
        return desiredPosition;
    }
    #endregion

    #region Shake
    private void UpdateShake(float deltaTime)
    {
        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - cameraData.shakeRecovery * deltaTime);
            float shake = trauma * trauma;
            float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * cameraData.shakeIntensity * shake;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * cameraData.shakeIntensity * shake;
            float offsetZ = (Mathf.PerlinNoise(Time.time * 25f, Time.time * 25f) - 0.5f) * 2f * cameraData.shakeIntensity * shake;
            shakeOffset = new Vector3(offsetX, offsetY, offsetZ);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    private void ApplyShakeToCamera()
    {
        if (cameraTransform == null || cameraTransform == transform)
        {
            transform.position = smoothedPosition + shakeOffset;
            return;
        }

        if (cameraTransform.IsChildOf(transform))
        {
            cameraTransform.localPosition = cameraBaseLocalPosition + shakeOffset;
        }
        else if (transform.IsChildOf(cameraTransform))
        {
            Vector3 cameraWorldPosBeforeShake = cameraTransform.position;
            cameraTransform.position = cameraWorldPosBeforeShake + shakeOffset;
            Vector3 shakeDelta = shakeOffset;
            transform.position = smoothedPosition - shakeDelta;
        }
        else
        {
            cameraTransform.position = cameraTransform.position + shakeOffset;
        }
    }
    #endregion

    #region Helpers
    private void EnsureCameraDataOrDisable()
    {
        if (cameraData != null) return;
        Debug.LogError("ThirdPersonCamera requires a CameraData assigned.", this);
        enabled = false;
    }

    private void InitializeCameraReferences()
    {
        cameraComponent = GetComponentInParent<Camera>();
        if (cameraComponent == null)
        {
            cameraComponent = GetComponent<Camera>();
        }
        
        cameraTransform = cameraComponent != null ? cameraComponent.transform : transform;
        
        if (cameraTransform != null && cameraTransform != transform)
        {
            cameraBaseLocalPosition = cameraTransform.localPosition;
        }
    }

    private void InitializeDistance()
    {
        float d0 = Mathf.Clamp(cameraData.defaultDistance, cameraData.minDistance, cameraData.maxDistance);
        currentDistance = d0;
        targetDistance = d0;
    }

    private void InitializeRotationFromTarget()
    {
        if (target == null) return;

        Vector3 direction = transform.position - target.position;
        currentYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        currentPitch = Mathf.Asin(direction.y / Mathf.Max(0.0001f, direction.magnitude)) * Mathf.Rad2Deg;
    }
    #endregion

}
