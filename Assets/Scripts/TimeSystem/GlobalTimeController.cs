using UnityEngine;


public class GlobalTimeController : MonoBehaviour
{
    #region Fields
    
    [Header("Time Settings - Cycle Mode")]
    [SerializeField] private float[] timeScaleLevels = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    
    [Tooltip("Índice do nível atual")]
    private int currentLevelIndex = 4; // Começa em 1.0 (normal)
    
    [Header("References")]
    [SerializeField] private TimeField globalField;
    [SerializeField] private PlayerControlsSO playerControlsSO; // Sistema de input unificado
    
    private bool isListenerRegistered = false; // Previne duplicação de listeners
    private float lastTimeControlTime = -1f; // Previne execução dupla no mesmo frame
    private const float TIME_CONTROL_COOLDOWN = 0.1f; // Cooldown mínimo entre execuções
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        SetupInputSystem();
        currentLevelIndex = timeScaleLevels.Length - 1;
        ApplyTimeScale(timeScaleLevels[currentLevelIndex]);
    }
    
    private void OnDestroy()
    {
        if (playerControlsSO != null && isListenerRegistered)
        {
            playerControlsSO.OnTimeControl -= OnTimeControl;
            isListenerRegistered = false;
        }
    }
    
    #endregion
    
    #region Setup
    
    private void SetupInputSystem()
    {
        // Previne registro duplicado
        if (isListenerRegistered) return;
        
        // Busca PlayerControlsSO automaticamente se não foi atribuído
        if (playerControlsSO == null)
        {
            playerControlsSO = Resources.FindObjectsOfTypeAll<PlayerControlsSO>()[0];
        }
        
        if (playerControlsSO != null)
        {
            // Remove primeiro para garantir que não há duplicação
            playerControlsSO.OnTimeControl -= OnTimeControl;
            // Adiciona o listener
            playerControlsSO.OnTimeControl += OnTimeControl;
            isListenerRegistered = true;
        }
        else
        {
            Debug.LogError("[GlobalTimeController] PlayerControlsSO não encontrado! Controle de tempo não funcionará.");
        }
    }
    
    private void OnTimeControl()
    {
        // Previne execução dupla no mesmo frame ou muito próxima
        float currentTime = Time.unscaledTime;
        if (currentTime - lastTimeControlTime < TIME_CONTROL_COOLDOWN)
        {
            return;
        }
        
        lastTimeControlTime = currentTime;
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

