using UnityEngine;

/// <summary>
/// Projétil simples que se move para frente e respeita o tempo local.
/// Se autodestrói ao colidir ou após tempo de vida.
/// </summary>
[RequireComponent(typeof(TimeEntity))]
public class SimpleProjectile : MonoBehaviour, ITimeScaledUpdate
{
    #region Fields
    
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    
    [Header("Effects")]
    [SerializeField] private bool destroyOnCollision = true;
    [SerializeField] private GameObject hitEffectPrefab;
    
    private Vector3 direction;
    private float currentLifetime = 0f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // Direção inicial é o forward
        direction = transform.forward;
    }
    
    #endregion
    
    #region ITimeScaledUpdate
    
    public void TimeScaledUpdate(float localDeltaTime)
    {
        // Mover usando tempo local (afetado por slow motion)
        transform.position += direction * speed * localDeltaTime;
        
        // Contar tempo de vida
        currentLifetime += localDeltaTime;
        if (currentLifetime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region Collision
    
    private void OnCollisionEnter(Collision collision)
    {
        if (destroyOnCollision)
        {
            // Efeito de impacto (opcional)
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Define a direção de movimento do projétil
    /// </summary>
    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection.normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    /// <summary>
    /// Define a velocidade do projétil
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    #endregion
}

