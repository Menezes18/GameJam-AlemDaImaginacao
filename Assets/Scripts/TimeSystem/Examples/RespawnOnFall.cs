using UnityEngine;


public class RespawnOnFall : MonoBehaviour
{
    #region Fields
    
    [Header("Respawn Settings")]
    [SerializeField] private float respawnHeight = 8f;
    
    [SerializeField] private bool resetVelocity = true;
    
    [SerializeField] private bool resetRotation = true;
    
    [Header("Random Spawn")]
    [SerializeField] private bool randomPosition = true;
    
    [SerializeField] private float randomRangeX = 5f;
    
    [SerializeField] private float randomRangeZ = 5f;
    
    [Header("Timer Settings")]
    [SerializeField] private float respawnTimer = 5f;
    
    [SerializeField] private bool useLocalTime = true;
    
    private Rigidbody rb;
    private TimeEntity timeEntity;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float currentTimer = 0f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        timeEntity = GetComponent<TimeEntity>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        currentTimer = 0f;
        
        if (useLocalTime && timeEntity == null)
        {
            Debug.LogWarning($"[RespawnOnFall] {gameObject.name} não possui TimeEntity mas useLocalTime está ativado. Usando tempo real.", this);
            useLocalTime = false;
        }
    }
    
    private void Update()
    {
        float deltaTime = useLocalTime && timeEntity != null 
            ? timeEntity.LocalDeltaTime 
            : Time.deltaTime;
        
        currentTimer += deltaTime;
        
        if (currentTimer >= respawnTimer)
        {
            ResetTransform();
            currentTimer = 0f; 
        }
    }
    
    #endregion
    
    #region Respawn
    
    private void ResetTransform()
    {
        Vector3 newPosition;
        
        if (randomPosition)
        {
            float randomX = Random.Range(-randomRangeX, randomRangeX);
            float randomZ = Random.Range(-randomRangeZ, randomRangeZ);
            newPosition = new Vector3(randomX, respawnHeight, randomZ);
        }
        else
        {
            newPosition = new Vector3(initialPosition.x, respawnHeight, initialPosition.z);
        }
        
        transform.position = newPosition;
        
        if (resetRotation)
        {
            transform.rotation = initialRotation;
        }
        
        if (resetVelocity && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
    }
    

    public void ResetTimer()
    {
        currentTimer = 0f;
    }
    

    public float GetTimerProgress()
    {
        return Mathf.Clamp01(currentTimer / respawnTimer);
    }
    
    #endregion
    

}

