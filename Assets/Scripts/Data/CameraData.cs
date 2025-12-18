using UnityEngine;

[CreateAssetMenu(fileName = "CameraData", menuName = "Camera/CameraData")]
public class CameraData : ScriptableObject
{
    [Header("Orbit Sensitivity")]
    public float sensitivityX = 2f;
    public float sensitivityY = 2f;
    public bool invertY = false;

    [Header("Pitch Limits")]
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Distance")]
    public float defaultDistance = 3.5f;
    [Min(0.05f)] 
    public float minDistance = 0.5f;
    public float maxDistance = 6f;
    public float distanceSmoothSpeed = 8f;

    [Header("Camera offset")]
    public Vector3 cameraOffset;

    [Header("Shoulder Offset")]
    public float shoulderOffsetX = 0.5f;
    public float shoulderOffsetY = 0.3f;
    public float shoulderSwitchSpeed = 5f;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 10f;
    public float rotationSmoothSpeed = 8f;

    [Header("Anti-Clipping")]
    public float clipRadius = 0.3f;
    public LayerMask clipMask = ~0;

    [Header("Auto Recenter")]
    public bool autoRecenter = true;
    public float recenterDelay = 2f;
    public float recenterSpeed = 90f;

    [Header("Shake")]
    public float shakeIntensity = 0.15f;
    public float shakeDuration = 0.3f;
    public float shakeRecovery = 2.5f;
}
