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
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"TimeField em {gameObject.name} não está como Trigger. Configurando automaticamente.", this);
            col.isTrigger = true;
        }
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
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsLayerInMask(other.gameObject.layer, affectedLayers))
            return;
        
        TimeEntity entity = other.GetComponent<TimeEntity>();
        if (entity != null)
        {
            entitiesInField.Add(entity);
            entity.AddTimeField(this);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!IsLayerInMask(other.gameObject.layer, affectedLayers))
            return;
        
        TimeEntity entity = other.GetComponent<TimeEntity>();
        if (entity != null)
        {
            entitiesInField.Remove(entity);
            entity.RemoveTimeField(this);
        }
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

