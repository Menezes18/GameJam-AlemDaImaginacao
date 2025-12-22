using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;


public class TimeEntity : MonoBehaviour
{
    #region Fields
    
    private HashSet<TimeField> activeFields = new HashSet<TimeField>();
    
    private float currentTimeScale = 1f;
    
    #endregion
    
    #region Cached Components
    
    private Rigidbody rb;
    private Animator animator;
    private NavMeshAgent navAgent;
    private ParticleSystem[] particleSystems;
    
    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;
    private bool wasKinematic;
    private bool isFrozen = false;
    
    #endregion
    
    #region Properties
    

    public float LocalTimeScale => currentTimeScale;
    

    public float LocalDeltaTime => Time.deltaTime * currentTimeScale;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        CacheComponents();
    }
    
    private void Update()
    {
        // Notificar scripts que implementam ITimeScaledUpdate
        NotifyTimeScaledUpdates();
    }
    
    private void FixedUpdate()
    {
        // Aplicar slow motion na física do Rigidbody
        ApplyRigidbodySlowMotion();
    }
    
    #endregion
    
    #region Public API
    
    public void AddTimeField(TimeField field)
    {
        activeFields.Add(field);
        RecalculateTimeScale();
    }
    
    public void RemoveTimeField(TimeField field)
    {
        activeFields.Remove(field);
        RecalculateTimeScale();
    }
    
    public void RecalculateTimeScale()
    {
        float newTimeScale = 1f;
        
        foreach (var field in activeFields)
        {
            if (field != null)
            {
                newTimeScale = Mathf.Min(newTimeScale, field.TimeScale);
            }
        }
        
        if (!Mathf.Approximately(currentTimeScale, newTimeScale))
        {
            currentTimeScale = newTimeScale;
            ApplyTimeScaleToComponents();
        }
    }

    public bool IsFrozen() => isFrozen;
    
    #endregion
    
    #region Core - Component Management
    
    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        particleSystems = GetComponentsInChildren<ParticleSystem>();
    }
    
    private void ApplyTimeScaleToComponents()
    {
        ApplyToRigidbody();
        ApplyToAnimator();
        ApplyToParticleSystems();
    }
    
    #endregion
    
    #region Component Handlers - Rigidbody
    
    private void ApplyToRigidbody()
    {
        if (rb == null) return;
        
        if (currentTimeScale <= 0f && !isFrozen)
        {
            FreezeRigidbody();
        }
        else if (currentTimeScale > 0f && isFrozen)
        {
            UnfreezeRigidbody();
        }
    }
    
    private void ApplyRigidbodySlowMotion()
    {
        if (rb == null || isFrozen) return;
        
        if (currentTimeScale < 1f && currentTimeScale > 0f)
        {

            float slowdownFactor = 1f - currentTimeScale;
            rb.linearVelocity *= (1f - slowdownFactor * 0.15f); // Desacelera gradualmente
            rb.angularVelocity *= (1f - slowdownFactor * 0.15f);
        }
    }
    
    private void FreezeRigidbody()
    {
        // Salvar estado atual
        savedVelocity = rb.linearVelocity;
        savedAngularVelocity = rb.angularVelocity;
        wasKinematic = rb.isKinematic;
        
        // Congelar
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        isFrozen = true;
    }
    
    private void UnfreezeRigidbody()
    {
        // Restaurar estado
        rb.isKinematic = wasKinematic;
        rb.linearVelocity = savedVelocity;
        rb.angularVelocity = savedAngularVelocity;
        
        isFrozen = false;
    }
    
    #endregion
    
    #region Component Handlers - Animator
    
    private void ApplyToAnimator()
    {
        if (animator == null) return;
        
        animator.speed = currentTimeScale;
    }
    
    #endregion
    
       
    #region Component Handlers - ParticleSystem
    
    private void ApplyToParticleSystems()
    {
        if (particleSystems == null || particleSystems.Length == 0) return;
        
        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;
            
            var main = ps.main;
            
            if (currentTimeScale <= 0f)
            {
                // Pausar partículas
                ps.Pause();
            }
            else
            {
                if (ps.isPaused)
                {
                    ps.Play();
                }
                main.simulationSpeed = currentTimeScale;
            }
        }
    }
    
    #endregion
    
    #region ITimeScaledUpdate Support
    
    private void NotifyTimeScaledUpdates()
    {
        var updatables = GetComponents<ITimeScaledUpdate>();
        foreach (var updatable in updatables)
        {
            updatable.TimeScaledUpdate(LocalDeltaTime);
        }
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = currentTimeScale > 0.5f ? Color.green : 
                       currentTimeScale > 0f ? Color.yellow : Color.red;
        
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    
    #endregion
}

