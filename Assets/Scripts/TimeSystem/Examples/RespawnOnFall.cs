using UnityEngine;


public class RespawnOnFall : MonoBehaviour
{
    #region Fields
    
    [Header("Respawn Settings")]
    [SerializeField] private float minHeight = -5f;
    
    [SerializeField] private float respawnHeight = 8f;
    
    [SerializeField] private bool resetVelocity = true;
    
    [Header("Random Spawn")]
    [SerializeField] private bool randomPosition = true;
    
    [SerializeField] private float randomRangeX = 5f;
    
    [SerializeField] private float randomRangeZ = 5f;
    
    private Rigidbody rb;
    private Vector3 initialPosition;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
    }
    
    private void Update()
    {
        if (transform.position.y < minHeight)
        {
            Respawn();
        }
    }
    
    #endregion
    
    #region Respawn
    
    private void Respawn()
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
        
        if (resetVelocity && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        transform.rotation = Random.rotation;
        
        Debug.Log($"[RespawnOnFall] {gameObject.name} respawnado em {newPosition}");
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(-10f, minHeight, -10f), 
            new Vector3(10f, minHeight, 10f)
        );
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(-10f, respawnHeight, -10f), 
            new Vector3(10f, respawnHeight, 10f)
        );
    }
    
    #endregion
}

