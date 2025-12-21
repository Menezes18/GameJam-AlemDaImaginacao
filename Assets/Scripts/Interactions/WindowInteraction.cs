using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WindowPanel
{
    public Transform panelTransform;
    
    public float openAngle = 90f;
    
    public WindowPanel(Transform transform, float angle)
    {
        panelTransform = transform;
        openAngle = angle;
    }
}


public class WindowInteraction : MonoBehaviour, IInteractable
{
    #region Inspector
    
    [Header("Window Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool useAnimation = true;
    
    [Header("Multiple Panels")]
    [SerializeField] private WindowPanel[] windowPanels;
    
    [Header("Axis")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    
    [Header("Interactable Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canPickUp = false;
    [SerializeField] private bool canAnalyze = false;
    
    #endregion
    
    #region Private
    
    private Quaternion[] closedRotations;
    private Quaternion[] openRotations;
    private Transform[] targets;
    private bool isAnimating = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        SetupWindowPanels();
        
        if (isOpen)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].localRotation = openRotations[i];
                }
            }
        }
    }
    
    private void SetupWindowPanels()
    {
        if (windowPanels == null || windowPanels.Length == 0)
        {
            targets = new Transform[] { transform };
            closedRotations = new Quaternion[1];
            openRotations = new Quaternion[1];
            closedRotations[0] = transform.localRotation;
            openRotations[0] = closedRotations[0] * Quaternion.Euler(rotationAxis * openAngle);
            return;
        }
        
        List<Transform> validTransforms = new List<Transform>();
        List<float> validAngles = new List<float>();
        
        for (int i = 0; i < windowPanels.Length; i++)
        {
            if (windowPanels[i].panelTransform != null)
            {
                validTransforms.Add(windowPanels[i].panelTransform);
                validAngles.Add(windowPanels[i].openAngle);
            }
        }
        
        if (validTransforms.Count == 0)
        {
            targets = new Transform[] { transform };
            closedRotations = new Quaternion[1];
            openRotations = new Quaternion[1];
            closedRotations[0] = transform.localRotation;
            openRotations[0] = closedRotations[0] * Quaternion.Euler(rotationAxis * openAngle);
            return;
        }
        
        targets = validTransforms.ToArray();
        closedRotations = new Quaternion[targets.Length];
        openRotations = new Quaternion[targets.Length];
        
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                closedRotations[i] = targets[i].localRotation;
                float angle = validAngles[i];
                openRotations[i] = closedRotations[i] * Quaternion.Euler(rotationAxis * angle);
            }
        }
    }
    
    #endregion
    
    #region Window Animation
    
    private IEnumerator AnimateWindow(bool opening, PlayerScript player = null)
    {
        isAnimating = true;
        
        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;
        
        Quaternion[] startRotations = new Quaternion[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                startRotations[i] = targets[i].localRotation;
            }
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = t * t * (3f - 2f * t);
            
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                
                Quaternion targetRotation = opening ? openRotations[i] : closedRotations[i];
                targets[i].localRotation = Quaternion.Slerp(startRotations[i], targetRotation, smoothT);
            }
            
            yield return null;
        }
        
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].localRotation = opening ? openRotations[i] : closedRotations[i];
            }
        }
        
        isAnimating = false;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Default;
        }
    }
    
    private void UpdateWindowState(bool open)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].localRotation = open ? openRotations[i] : closedRotations[i];
            }
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
        
        if (isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        isOpen = !isOpen;
        if (useAnimation)
        {
            StartCoroutine(AnimateWindow(isOpen, player));
        }
        else
        {
            UpdateWindowState(isOpen);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
        
        string panelInfo = targets.Length > 1 ? $" ({targets.Length} painéis)" : "";
        Debug.Log($"🪟 [WINDOW] Janela {(isOpen ? "ABERTA" : "FECHADA")}{panelInfo} por {player.name}");
        
        if (windowPanels != null && windowPanels.Length > 0)
        {
            for (int i = 0; i < windowPanels.Length && i < targets.Length; i++)
            {
                if (windowPanels[i].panelTransform != null)
                {
                    Debug.Log($"  └─ Painel {i + 1}: {windowPanels[i].panelTransform.name} → {windowPanels[i].openAngle}°");
                }
            }
        }
    }
    
    #endregion
    
    #region Window Control
    
    public void ToggleWindow(PlayerScript player = null)
    {
        if (isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        isOpen = !isOpen;
        if (useAnimation)
        {
            StartCoroutine(AnimateWindow(isOpen, player));
        }
        else
        {
            UpdateWindowState(isOpen);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
        
        string panelInfo = targets.Length > 1 ? $" ({targets.Length} painéis)" : "";
        Debug.Log($"🪟 [WINDOW] Janela {(isOpen ? "ABERTA" : "FECHADA")}{panelInfo}: {gameObject.name}");
        
        if (windowPanels != null && windowPanels.Length > 0)
        {
            for (int i = 0; i < windowPanels.Length && i < targets.Length; i++)
            {
                if (windowPanels[i].panelTransform != null)
                {
                    Debug.Log($"  └─ Painel {i + 1}: {windowPanels[i].panelTransform.name} → {windowPanels[i].openAngle}°");
                }
            }
        }
    }
    
    public void OpenWindow(PlayerScript player = null)
    {
        if (isOpen || isAnimating) return;
        isOpen = true;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        if (useAnimation)
        {
            StartCoroutine(AnimateWindow(true, player));
        }
        else
        {
            UpdateWindowState(true);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public void CloseWindow(PlayerScript player = null)
    {
        if (!isOpen || isAnimating) return;
        isOpen = false;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        if (useAnimation)
        {
            StartCoroutine(AnimateWindow(false, player));
        }
        else
        {
            UpdateWindowState(false);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public int PanelCount => targets != null ? targets.Length : 0;
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    
    #endregion
}
