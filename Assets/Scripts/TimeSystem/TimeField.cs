using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TimeField : MonoBehaviour
{
    #region Fields
    
    [Header("Time Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float timeScale = 0f;
    
    [Header("Filter")]
    [SerializeField] private LayerMask affectedLayers = -1;
    
    [Header("Visual testando ainda")]
    [SerializeField] private Material fieldMaterial;
    
    private HashSet<TimeEntity> entitiesInField = new HashSet<TimeEntity>();
    
    #endregion
    
    #region Properties
    
    public float TimeScale => timeScale;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Não força mais o collider a ser trigger - permite colliders normais
        // para que o jogador possa subir nos objetos mesmo com tempo parado
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            foreach (var entity in entitiesInField)
            {
                if (entity != null)
                {
                    entity.RecalculateTimeScale();
                }
            }
        }
    }
    
    private void FixedUpdate()
    {
        // Detecção manual usando Physics.Overlap para funcionar com colliders normais
        DetectEntitiesInField();
    }
    
    #endregion
    
    #region Public API
    

    public void SetTimeScale(float newScale)
    {
        timeScale = Mathf.Clamp01(newScale);
        
        foreach (var entity in entitiesInField)
        {
            if (entity != null)
            {
                entity.RecalculateTimeScale();
            }
        }
    }
    
    #endregion
    
    #region Helpers
    
    private void DetectEntitiesInField()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        // Coleta todos os colliders dentro do campo de tempo
        Collider[] collidersInField = null;
        
        if (col is BoxCollider boxCollider)
        {
            Vector3 center = transform.TransformPoint(boxCollider.center);
            Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, transform.lossyScale);
            Quaternion rotation = transform.rotation;
            collidersInField = Physics.OverlapBox(center, halfExtents, rotation, affectedLayers);
        }
        else if (col is SphereCollider sphereCollider)
        {
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            collidersInField = Physics.OverlapSphere(center, radius, affectedLayers);
        }
        else if (col is CapsuleCollider capsuleCollider)
        {
            // Para CapsuleCollider, usamos uma aproximação com OverlapSphere
            Vector3 center = transform.TransformPoint(capsuleCollider.center);
            float radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float height = capsuleCollider.height * transform.lossyScale.y;
            collidersInField = Physics.OverlapSphere(center, Mathf.Max(radius, height * 0.5f), affectedLayers);
        }
        
        if (collidersInField == null) return;
        
        // Cria um HashSet temporário dos objetos atualmente no campo
        HashSet<TimeEntity> currentEntities = new HashSet<TimeEntity>();
        
        // Adiciona entidades que estão no campo agora
        foreach (var collider in collidersInField)
        {
            if (collider == null || collider == col) continue; // Ignora o próprio collider do TimeField
            
            TimeEntity entity = collider.GetComponent<TimeEntity>();
            if (entity != null)
            {
                currentEntities.Add(entity);
                
                // Se a entidade não estava no campo antes, adiciona
                if (!entitiesInField.Contains(entity))
                {
                    entitiesInField.Add(entity);
                    entity.AddTimeField(this);
                }
            }
        }
        
        // Remove entidades que não estão mais no campo
        HashSet<TimeEntity> entitiesToRemove = new HashSet<TimeEntity>();
        foreach (var entity in entitiesInField)
        {
            if (entity == null || !currentEntities.Contains(entity))
            {
                entitiesToRemove.Add(entity);
            }
        }
        
        foreach (var entity in entitiesToRemove)
        {
            entitiesInField.Remove(entity);
            if (entity != null)
            {
                entity.RemoveTimeField(this);
            }
        }
    }
    
    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask) != 0;
    }
    
    #endregion
    
    #region Debug/Gizmos
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
    
    #endregion
}

