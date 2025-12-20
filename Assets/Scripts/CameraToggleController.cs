using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraToggleController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera _mapCamera;
    [SerializeField] private CinemachineCamera _playerCamera;
    [Header("Input")]
    [SerializeField] private InputActionReference _toggleCameraAction;
    
    private bool _isPlayerFocused = false;

    #region Unity
    private void Start()
    {
        _toggleCameraAction.action.performed += ToggleCamera;
    }
    private void OnDestroy()
    {
        _toggleCameraAction.action.performed -= ToggleCamera;
    }
    #endregion

    #region Private
    private void ToggleCamera(InputAction.CallbackContext context)
    {
        _isPlayerFocused = !_isPlayerFocused;
        _mapCamera.Priority = _isPlayerFocused ? 0 : 10;
        _playerCamera.Priority = _isPlayerFocused ? 20 : 0;
    }
    #endregion
}
