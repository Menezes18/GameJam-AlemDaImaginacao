using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InteractionUI : MonoBehaviour
{
    #region Inspector
    
    [Header("UI References")]
    [SerializeField] private CanvasGroup interactionCanvasGroup;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private string defaultInteractionMessage = "Aperte E para interagir";
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerScript player;
    
    #endregion
    
    #region Private
    
    private IInteractable currentInteractable;
    private Coroutine fadeCoroutine;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (interactionCanvasGroup == null)
        {
            interactionCanvasGroup = GetComponent<CanvasGroup>();
            if (interactionCanvasGroup == null)
            {
                interactionCanvasGroup = GetComponentInChildren<CanvasGroup>();
            }
        }
        
               
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0f;
            interactionCanvasGroup.blocksRaycasts = false;
            interactionCanvasGroup.interactable = false;
        }
    }
    
    private void Update()
    {
        if (player == null) return;
        
        UpdateInteractionUI();
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateInteractionUI()
    {
        IInteractable interactable = GetCurrentInteractable();
        
        if (interactable != null && interactable.CanInteract())
        {
            if (currentInteractable != interactable)
            {
                currentInteractable = interactable;
                ShowInteractionUI();
            }
        }
        else
        {
            if (currentInteractable != null)
            {
                currentInteractable = null;
                HideInteractionUI();
            }
        }
    }
    
    private IInteractable GetCurrentInteractable()
    {
        if (player == null) return null;
        return player.CurrentInteractable;
    }
    
    private void ShowInteractionUI()
    {
        if (interactionText != null && currentInteractable != null)
        {
            MonoBehaviour interactableObj = currentInteractable as MonoBehaviour;
            string objectName = interactableObj != null ? interactableObj.gameObject.name : "";
            
            if (string.IsNullOrEmpty(objectName))
            {
                interactionText.text = defaultInteractionMessage;
            }
            else
            {
                interactionText.text = defaultInteractionMessage;
            }
        }
        
        if (interactionCanvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeIn());
        }
    }
    
    private void HideInteractionUI()
    {
        if (interactionCanvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOut());
        }
    }
    
    private IEnumerator FadeIn()
    {
        if (interactionCanvasGroup == null) yield break;
        
        interactionCanvasGroup.blocksRaycasts = true;
        interactionCanvasGroup.interactable = true;
        
        float elapsed = 0f;
        float startAlpha = interactionCanvasGroup.alpha;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            interactionCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }
        
        interactionCanvasGroup.alpha = 1f;
        fadeCoroutine = null;
    }
    
    private IEnumerator FadeOut()
    {
        if (interactionCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        float startAlpha = interactionCanvasGroup.alpha;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            interactionCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        interactionCanvasGroup.alpha = 0f;
        interactionCanvasGroup.blocksRaycasts = false;
        interactionCanvasGroup.interactable = false;
        fadeCoroutine = null;
    }
    
    #endregion
}
