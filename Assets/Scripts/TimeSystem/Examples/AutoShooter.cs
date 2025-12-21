using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Atirador automático que dispara projéteis a cada X segundos.
/// Projéteis se destroem automaticamente após um tempo.
/// RESPEITA SLOW MOTION - o intervalo de tiro também fica lento!
/// </summary>
[RequireComponent(typeof(TimeEntity))]
public class AutoShooter : MonoBehaviour, ITimeScaledUpdate
{
    #region Fields
    
    [Header("Shoot Settings")]
    [Tooltip("Intervalo entre tiros (segundos)")]
    [SerializeField] private float shootInterval = 2f;
    
    [Tooltip("Velocidade do projétil")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("Tempo de vida do projétil (segundos)")]
    [SerializeField] private float projectileLifetime = 5f;
    
    [Header("Projectile Prefab")]
    [Tooltip("Prefab do projétil (deixe vazio pra criar esfera automática)")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Header("Spawn")]
    [Tooltip("Ponto de spawn (deixe vazio pra usar posição deste objeto)")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("Direção de disparo (deixe 0,0,0 pra usar forward)")]
    [SerializeField] private Vector3 shootDirection = Vector3.zero;
    
    private float shootTimer = 0f;
    private List<GameObject> activeProjectiles = new List<GameObject>();
    private TimeEntity timeEntity;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // Pegar TimeEntity
        timeEntity = GetComponent<TimeEntity>();
        
        // Se não tem spawn point, usar própria posição
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
        
        // Se não tem prefab, criar um simples
        if (projectilePrefab == null)
        {
            CreateDefaultProjectilePrefab();
        }
        
        // Iniciar timer
        shootTimer = shootInterval;
    }
    
    private void Update()
    {
        // Limpar projéteis nulos da lista
        activeProjectiles.RemoveAll(p => p == null);
    }
    
    #endregion
    
    #region ITimeScaledUpdate
    
    public void TimeScaledUpdate(float localDeltaTime)
    {
        // Contar tempo usando deltaTime local (afetado por slow motion)
        shootTimer -= localDeltaTime;
        
        // Quando timer chega a zero, atirar
        if (shootTimer <= 0f)
        {
            Shoot();
            shootTimer = shootInterval; // Resetar timer
        }
    }
    
    #endregion
    
    #region Shooting
    
    private void Shoot()
    {
        // Criar projétil
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        
        // Configurar Rigidbody
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Determinar direção
            Vector3 direction = shootDirection.magnitude > 0.1f ? shootDirection.normalized : spawnPoint.forward;
            
            // Aplicar velocidade
            rb.linearVelocity = direction * projectileSpeed;
        }
        
        // Adicionar à lista
        activeProjectiles.Add(projectile);
        
        // Agendar destruição
        Destroy(projectile, projectileLifetime);
        
        Debug.Log($"[AutoShooter] Tiro disparado! ({activeProjectiles.Count} projéteis ativos)");
    }
    
    #endregion
    
    #region Prefab Creation
    
    private void CreateDefaultProjectilePrefab()
    {
        // Criar prefab padrão (esfera vermelha)
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        prefab.name = "Projectile";
        prefab.transform.localScale = Vector3.one * 0.3f;
        
        // Cor vermelha
        Renderer rend = prefab.GetComponent<Renderer>();
        rend.material.color = Color.red;
        
        // Rigidbody
        Rigidbody rb = prefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // TimeEntity para ser afetado pelo slow motion
        prefab.AddComponent<TimeEntity>();
        
        // Layer TimeAffected
        int timeLayer = LayerMask.NameToLayer("TimeAffected");
        if (timeLayer != -1)
        {
            prefab.layer = timeLayer;
        }
        
        projectilePrefab = prefab;
        
        Debug.Log("[AutoShooter] Prefab de projétil criado automaticamente");
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (spawnPoint == null) return;
        
        // Desenhar linha mostrando direção de tiro
        Vector3 direction = shootDirection.magnitude > 0.1f ? shootDirection.normalized : spawnPoint.forward;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(spawnPoint.position, direction * 2f);
        Gizmos.DrawWireSphere(spawnPoint.position, 0.2f);
    }
    
    #endregion
}

