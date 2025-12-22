using UnityEngine;
using UnityEngine.Events;


public class DoorInteraction : MonoBehaviour, IInteractable
{
    #region Inspector
    
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool useAnimation = true;
    
    [Header("Trigger Settings")]
    [SerializeField] private bool enableTriggerOnOpen = true;
    [SerializeField] private Collider doorTrigger;
    
    [Header("Interactable Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canPickUp = false;
    [SerializeField] private bool canAnalyze = false;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onDoorOpen;
    [SerializeField] private UnityEvent onDoorClose;
    
    #endregion
    
    #region Private
    
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isAnimating = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        
        if (isOpen)
        {
            transform.rotation = openRotation;
        }
        
        if (doorTrigger != null)
        {
            doorTrigger.enabled = enableTriggerOnOpen && isOpen;
        }
    }
    
    private void Update()
    {
        if (isAnimating && useAnimation)
        {
            AnimateDoor();
        }
    }
    
    #endregion
    
    #region Door Animation
    
    private PlayerScript currentInteractingPlayer;
    
    private void AnimateDoor()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, animationSpeed * Time.deltaTime);
        
        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
        {
            transform.rotation = targetRotation;
            isAnimating = false;
            OnAnimationComplete();
        }
    }
    
    private void OnAnimationComplete()
    {
        if (doorTrigger != null)
        {
            doorTrigger.enabled = enableTriggerOnOpen && isOpen;
        }
        
        // Invoca eventos baseado no estado final
        if (isOpen)
        {
            onDoorOpen?.Invoke();
        }
        else
        {
            onDoorClose?.Invoke();
        }
        
        if (currentInteractingPlayer != null)
        {
            currentInteractingPlayer.Status = PlayerStatus.Default;
            currentInteractingPlayer = null;
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
        
        ToggleDoor(player);
    }
    
    #endregion
    
    #region Door Control
    
    public void ToggleDoor(PlayerScript player = null)
    {
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
            currentInteractingPlayer = player;
        }
        
        isOpen = !isOpen;
        isAnimating = useAnimation;
        
        if (!useAnimation)
        {
            transform.rotation = isOpen ? openRotation : closedRotation;
            OnAnimationComplete();
        }
        
        Debug.Log($"🚪 [DOOR] Porta {(isOpen ? "ABERTA" : "FECHADA")}: {gameObject.name}");
    }
    
    public void OpenDoor()
    {
        if (isOpen || isAnimating) return;
        
        isOpen = true;
        isAnimating = useAnimation;
        currentInteractingPlayer = null; // Não precisa de player
        
        if (!useAnimation)
        {
            transform.rotation = openRotation;
            OnAnimationComplete();
        }
        
        Debug.Log($"🚪 [DOOR] Porta ABERTA: {gameObject.name}");
    }
    
    public void CloseDoor()
    {
        if (!isOpen || isAnimating) return;
        
        isOpen = false;
        isAnimating = useAnimation;
        currentInteractingPlayer = null; // Não precisa de player
        
        if (!useAnimation)
        {
            transform.rotation = closedRotation;
            OnAnimationComplete();
        }
        
        Debug.Log($"🚪 [DOOR] Porta FECHADA: {gameObject.name}");
    }
    
    // Versão com PlayerScript para compatibilidade (opcional)
    public void OpenDoor(PlayerScript player)
    {
        OpenDoor();
    }
    
    public void CloseDoor(PlayerScript player)
    {
        CloseDoor();
    }
    
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (doorTrigger != null)
        {
            Gizmos.color = doorTrigger.enabled ? Color.green : Color.red;
            BoxCollider box = doorTrigger as BoxCollider;
            if (box != null)
            {
                Gizmos.matrix = doorTrigger.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
    
    #endregion
}
