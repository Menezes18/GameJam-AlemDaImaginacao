using UnityEngine;
using UnityEngine.InputSystem;


public class GlobalTimeController : MonoBehaviour
{
    #region Fields
    
    [Header("Controls - Input System")]
    [SerializeField] private InputAction toggleSlowMotionAction;
    
    [Header("Time Settings - Cycle Mode")]
    [SerializeField] private float[] timeScaleLevels = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    
    [Tooltip("Índice do nível atual")]
    private int currentLevelIndex = 4; // Começa em 1.0 (normal)
    
    [Header("References")]
    [SerializeField] private TimeField globalField;
    
    [SerializeField] private GameObject player;
    
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        //SetupGlobalField();
        SetupInputSystem();
        
        currentLevelIndex = timeScaleLevels.Length - 1;
        ApplyTimeScale(timeScaleLevels[currentLevelIndex]);
    }
    
    private void OnEnable()
    {
        toggleSlowMotionAction?.Enable();
    }
    
    private void OnDisable()
    {
        toggleSlowMotionAction?.Disable();
    }
    
    private void OnDestroy()
    {
        // Limpar callback
        if (toggleSlowMotionAction != null)
        {
            toggleSlowMotionAction.performed -= OnToggleSlowMotion;
        }
    }
    
    #endregion
    
    #region Setup
    
    private void SetupInputSystem()
    {
        if (toggleSlowMotionAction == null || toggleSlowMotionAction.bindings.Count == 0)
        {
            toggleSlowMotionAction = new InputAction("ToggleSlowMotion", binding: "<Keyboard>/e");
            toggleSlowMotionAction.Enable();
        }
        
        toggleSlowMotionAction.performed += OnToggleSlowMotion;
    }
    
    private void OnToggleSlowMotion(InputAction.CallbackContext context)
    {
        CycleTimeScale();
    }
    
   
    
   
    #endregion
    
    #region Time Control
    
    private void CycleTimeScale()
    {
        // Avançar pro próximo nível (ciclo)
        currentLevelIndex++;
        if (currentLevelIndex >= timeScaleLevels.Length)
        {
            currentLevelIndex = 0; 
        }
        
        float newScale = timeScaleLevels[currentLevelIndex];
        ApplyTimeScale(newScale);
        
                
        // Log
        string status = GetTimeScaleStatus(newScale);
        Debug.Log($"[GlobalTimeController] TimeScale: {newScale:F2} ({status})");
    }
    
    private void ApplyTimeScale(float scale)
    {
        if (globalField != null)
        {
            globalField.SetTimeScale(scale);
        }
    }
    
    private string GetTimeScaleStatus(float scale)
    {
        if (scale <= 0f) return "FROZEN ❄️";
        if (scale < 0.3f) return "MUITO LENTO 🐌";
        if (scale < 0.6f) return "LENTO ⏳";
        if (scale < 0.9f) return "LEVEMENTE LENTO ⏱️";
        return "NORMAL ▶️";
    }
    
    #endregion
    
       
    #region Public API
    

    public void CycleToNextLevel()
    {
        CycleTimeScale();
    }
    

    public void SetTimeScale(float scale)
    {
        ApplyTimeScale(scale);
    }
    

    public float CurrentTimeScale => timeScaleLevels[currentLevelIndex];
    

    public bool IsFrozen => CurrentTimeScale <= 0f;
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (globalField != null && Application.isPlaying)
        {
            float currentScale = timeScaleLevels[currentLevelIndex];
            Color gizmoColor = Color.Lerp(new Color(1f, 0.2f, 0.2f, 0.15f), new Color(0.2f, 1f, 0.2f, 0.05f), currentScale);
            Gizmos.color = gizmoColor;
            Gizmos.matrix = globalField.transform.localToWorldMatrix;
            
            BoxCollider col = globalField.GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.DrawCube(col.center, col.size);
            }
        }
    }
    
    #endregion
}

