using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class InterruptorInteraction : MonoBehaviour, IInteractable
{
    #region Inspector
    
    [Header("Interruptor Settings")]
    [SerializeField] private bool isLigado = false;
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private bool useAnimation = true;
    
    [Header("Luzes")]
    [SerializeField] private Light[] luzes;
    
    [Header("Animação do Interruptor")]
    [SerializeField] private Transform interruptorTransform;
    [SerializeField] private float anguloLigado = 15f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    
    [Header("Interactable Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canPickUp = false;
    [SerializeField] private bool canAnalyze = false;
    
    [Header("Collider Settings")]
    [SerializeField] private bool autoCreateCollider = true;
    [SerializeField] private float interactionColliderRadius = 0.5f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onInterruptorLigar;
    [SerializeField] private UnityEvent onInterruptorDesligar;
    
    #endregion
    
    #region Private
    
    private Quaternion rotacaoDesligado;
    private Quaternion rotacaoLigado;
    private bool isAnimating = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        SetupCollider();
        SetupInterruptorAnimation();
        ApplyInitialState();
    }
    
    private void SetupCollider()
    {
        if (!autoCreateCollider) return;
        
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
        {
            return;
        }
        
        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = interactionColliderRadius;
        sphereCollider.isTrigger = false;
        
        Debug.Log($"✅ [INTERRUPTOR] SphereCollider criado automaticamente em {gameObject.name} com raio {interactionColliderRadius}");
    }
    
    private void SetupInterruptorAnimation()
    {
        if (interruptorTransform == null) return;
        
        rotacaoDesligado = interruptorTransform.localRotation;
        rotacaoLigado = rotacaoDesligado * Quaternion.Euler(rotationAxis * anguloLigado);
    }
    
    private void ApplyInitialState()
    {
        SetLuzesState(isLigado);
        
        if (interruptorTransform != null && !useAnimation)
        {
            interruptorTransform.localRotation = isLigado ? rotacaoLigado : rotacaoDesligado;
        }
    }
    
    #endregion
    
    #region Interruptor Animation
    
    private IEnumerator AnimateInterruptor(bool ligando, PlayerScript player = null)
    {
        if (interruptorTransform == null)
        {
            SetLuzesState(ligando);
            InvokeEvents(ligando);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
            yield break;
        }
        
        isAnimating = true;
        
        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;
        Quaternion startRotation = interruptorTransform.localRotation;
        Quaternion targetRotation = ligando ? rotacaoLigado : rotacaoDesligado;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = t * t * (3f - 2f * t);
            
            interruptorTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            yield return null;
        }
        
        interruptorTransform.localRotation = targetRotation;
        
        SetLuzesState(ligando);
        InvokeEvents(ligando);
        
        isAnimating = false;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Default;
        }
    }
    
    private void SetLuzesState(bool ligado)
    {
        foreach (Light luz in luzes)
        {
            if (luz != null)
            {
                luz.enabled = ligado;
            }
        }
    }
    
    private void InvokeEvents(bool ligando)
    {
        if (ligando)
        {
            onInterruptorLigar?.Invoke();
        }
        else
        {
            onInterruptorDesligar?.Invoke();
        }
    }
    
    #endregion
    
    #region IInteractable
    
    public bool CanInteract()
    {
        return canInteract && !isAnimating;
    }
    
    public bool CanPickUp()
    {
        return canPickUp;
    }
    
    public bool CanAnalyze()
    {
        return canAnalyze;
    }
    
    public void OnInteract(PlayerScript player)
    {
        if (!CanInteract()) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        bool wasLigado = isLigado;
        isLigado = !isLigado;
        bool isLigando = !wasLigado;
        
        if (useAnimation)
        {
            StartCoroutine(AnimateInterruptor(isLigando, player));
        }
        else
        {
            SetLuzesState(isLigando);
            
            if (interruptorTransform != null)
            {
                interruptorTransform.localRotation = isLigando ? rotacaoLigado : rotacaoDesligado;
            }
            
            InvokeEvents(isLigando);
            
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
        
        Debug.Log($"🔌 [INTERRUPTOR] Interruptor {(isLigado ? "LIGADO" : "DESLIGADO")} por {player.name}");
    }
    
    #endregion
    
    #region Interruptor Control
    
    public void ToggleInterruptor(PlayerScript player = null)
    {
        if (isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        bool wasLigado = isLigado;
        isLigado = !isLigado;
        bool isLigando = !wasLigado;
        
        if (useAnimation)
        {
            StartCoroutine(AnimateInterruptor(isLigando, player));
        }
        else
        {
            SetLuzesState(isLigando);
            if (interruptorTransform != null)
            {
                interruptorTransform.localRotation = isLigando ? rotacaoLigado : rotacaoDesligado;
            }
            InvokeEvents(isLigando);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public void LigarInterruptor(PlayerScript player = null)
    {
        if (isLigado || isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        isLigado = true;
        if (useAnimation)
        {
            StartCoroutine(AnimateInterruptor(true, player));
        }
        else
        {
            SetLuzesState(true);
            if (interruptorTransform != null)
            {
                interruptorTransform.localRotation = rotacaoLigado;
            }
            InvokeEvents(true);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public void DesligarInterruptor(PlayerScript player = null)
    {
        if (!isLigado || isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        isLigado = false;
        if (useAnimation)
        {
            StartCoroutine(AnimateInterruptor(false, player));
        }
        else
        {
            SetLuzesState(false);
            if (interruptorTransform != null)
            {
                interruptorTransform.localRotation = rotacaoDesligado;
            }
            InvokeEvents(false);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public bool IsLigado => isLigado;
    public bool IsAnimating => isAnimating;
    
    #endregion
}
