using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[System.Serializable]
public class CortinaData
{
    public Transform cortinaTransform;
    public Transform closedPositionTransform;
    public Vector3 openOffset = Vector3.zero;
    
    [HideInInspector]
    public Vector3 closedLocalPos;
    
    [HideInInspector]
    public Vector3 openLocalPos;
}

public class CortinaInteraction : MonoBehaviour, IInteractable
{
    #region Inspector
    
    [Header("Cortina Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool useAnimation = true;
    
    [Header("Cortinas")]
    [SerializeField] private CortinaData[] cortinas;
    [SerializeField] private Vector3 defaultOpenOffset = Vector3.right * 2f;
    
    [Header("Interactable Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canPickUp = false;
    [SerializeField] private bool canAnalyze = false;
    
    [Header("Collider Settings")]
    [SerializeField] private bool autoCreateCollider = true;
    [SerializeField] private float interactionColliderRadius = 1f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onCortinaOpen;
    [SerializeField] private UnityEvent onCortinaClose;
    
    #endregion
    
    #region Private
    
    private bool isAnimating = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        CalculatePositions();
        
        foreach (var cortina in cortinas)
        {
            if (cortina.cortinaTransform == null) continue;
            
            if (isOpen)
            {
                cortina.cortinaTransform.localPosition = cortina.openLocalPos;
            }
            else
            {
                cortina.cortinaTransform.localPosition = cortina.closedLocalPos;
            }
        }
    }
    
    private void CalculatePositions()
    {
        foreach (var cortina in cortinas)
        {
            if (cortina.cortinaTransform == null) continue;
            
            Transform parent = cortina.cortinaTransform.parent;
            
            if (cortina.closedPositionTransform != null)
            {
                cortina.closedLocalPos = parent != null 
                    ? parent.InverseTransformPoint(cortina.closedPositionTransform.position) 
                    : cortina.closedPositionTransform.position;
            }
            else
            {
                cortina.closedLocalPos = cortina.cortinaTransform.localPosition;
            }
            
            Vector3 offset = cortina.openOffset != Vector3.zero 
                ? cortina.openOffset 
                : defaultOpenOffset;
            
            Vector3 worldOffset = parent != null 
                ? parent.TransformDirection(offset) 
                : offset;
            
            Vector3 worldClosedPos = parent != null 
                ? parent.TransformPoint(cortina.closedLocalPos) 
                : cortina.closedLocalPos;
            
            Vector3 worldOpenPos = worldClosedPos + worldOffset;
            
            cortina.openLocalPos = parent != null 
                ? parent.InverseTransformPoint(worldOpenPos) 
                : worldOpenPos;
        }
    }
    
    #endregion
    
    #region Cortina Animation
    
    private IEnumerator AnimateCortina(bool opening, PlayerScript player = null)
    {
        isAnimating = true;
        
        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;
        
        Vector3[] startPositions = new Vector3[cortinas.Length];
        for (int i = 0; i < cortinas.Length; i++)
        {
            if (cortinas[i].cortinaTransform != null)
            {
                startPositions[i] = cortinas[i].cortinaTransform.localPosition;
            }
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = t * t * (3f - 2f * t);
            
            for (int i = 0; i < cortinas.Length; i++)
            {
                if (cortinas[i].cortinaTransform == null) continue;
                
                Vector3 targetPos = opening ? cortinas[i].openLocalPos : cortinas[i].closedLocalPos;
                cortinas[i].cortinaTransform.localPosition = Vector3.Lerp(startPositions[i], targetPos, smoothT);
            }
            
            yield return null;
        }
        
        for (int i = 0; i < cortinas.Length; i++)
        {
            if (cortinas[i].cortinaTransform == null) continue;
            
            cortinas[i].cortinaTransform.localPosition = opening 
                ? cortinas[i].openLocalPos 
                : cortinas[i].closedLocalPos;
        }
        
        isAnimating = false;
        
        if (opening)
        {
            onCortinaOpen?.Invoke();
        }
        else
        {
            onCortinaClose?.Invoke();
        }
        
        if (player != null)
        {
            player.Status = PlayerStatus.Default;
        }
    }
    
    private void UpdateCortinaState(bool isOpening, bool wasOpen)
    {
        foreach (var cortina in cortinas)
        {
            if (cortina.cortinaTransform == null) continue;
            
            cortina.cortinaTransform.localPosition = isOpening ? cortina.openLocalPos : cortina.closedLocalPos;
        }
        
        if (isOpening)
        {
            onCortinaOpen?.Invoke();
        }
        else
        {
            onCortinaClose?.Invoke();
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
        
        bool wasOpen = isOpen;
        isOpen = !isOpen;
        bool isOpening = !wasOpen;
        
        if (useAnimation)
        {
            StartCoroutine(AnimateCortina(isOpening, player));
        }
        else
        {
            UpdateCortinaState(isOpening, wasOpen);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
        
        Debug.Log($"🪟 [CORTINA] Cortina {(isOpen ? "ABERTA" : "FECHADA")} por {player.name}");
    }
    
    #endregion
    
    #region Cortina Control
    
    public void ToggleCortina(PlayerScript player = null)
    {
        if (isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        bool wasOpen = isOpen;
        isOpen = !isOpen;
        bool isOpening = !wasOpen;
        
        if (useAnimation)
        {
            StartCoroutine(AnimateCortina(isOpening, player));
        }
        else
        {
            UpdateCortinaState(isOpening, wasOpen);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public void OpenCortina(PlayerScript player = null)
    {
        if (isOpen || isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        isOpen = true;
        if (useAnimation)
        {
            StartCoroutine(AnimateCortina(true, player));
        }
        else
        {
            UpdateCortinaState(true, false);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public void CloseCortina(PlayerScript player = null)
    {
        if (!isOpen || isAnimating) return;
        
        if (player != null)
        {
            player.Status = PlayerStatus.Interacting;
        }
        
        isOpen = false;
        if (useAnimation)
        {
            StartCoroutine(AnimateCortina(false, player));
        }
        else
        {
            UpdateCortinaState(false, true);
            if (player != null)
            {
                player.Status = PlayerStatus.Default;
            }
        }
    }
    
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        DrawGizmos(false);
    }
    
    private void OnDrawGizmosSelected()
    {
        DrawGizmos(true);
    }
    
    private void DrawGizmos(bool selected)
    {
        if (cortinas == null) return;
        
        Color[] cortinaColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f),
            new Color(0.3f, 0.3f, 1f),
            new Color(1f, 0.8f, 0.3f),
            new Color(0.3f, 1f, 0.3f),
        };
        
        for (int i = 0; i < cortinas.Length; i++)
        {
            var cortina = cortinas[i];
            if (cortina.cortinaTransform == null) continue;
            
            Color cortinaColor = i < cortinaColors.Length ? cortinaColors[i] : Color.white;
            Transform parent = cortina.cortinaTransform.parent;
            
            Vector3 closedWorldPos;
            Vector3 openWorldPos;
            
            if (cortina.closedPositionTransform != null)
            {
                closedWorldPos = cortina.closedPositionTransform.position;
            }
            else
            {
                closedWorldPos = cortina.cortinaTransform.position;
            }
            
            Vector3 offset = cortina.openOffset != Vector3.zero 
                ? cortina.openOffset 
                : defaultOpenOffset;
            
            Vector3 worldOffset = parent != null 
                ? parent.TransformDirection(offset) 
                : offset;
            openWorldPos = closedWorldPos + worldOffset;
            
            Bounds cortinaBounds = GetCortinaBounds(cortina.cortinaTransform);
            Vector3 cortinaSize = cortinaBounds.size;
            if (cortinaSize == Vector3.zero)
            {
                cortinaSize = Vector3.one * 0.5f;
            }
            
            Gizmos.color = new Color(cortinaColor.r, cortinaColor.g, cortinaColor.b, selected ? 0.6f : 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(openWorldPos, cortina.cortinaTransform.rotation, cortinaSize);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            
            Gizmos.matrix = Matrix4x4.identity;
            
            Gizmos.color = new Color(cortinaColor.r, cortinaColor.g, cortinaColor.b, selected ? 0.5f : 0.3f);
            Gizmos.DrawWireSphere(closedWorldPos, 0.1f);
            
            Gizmos.color = new Color(cortinaColor.r, cortinaColor.g, cortinaColor.b, selected ? 1f : 0.7f);
            Gizmos.DrawWireSphere(openWorldPos, 0.15f);
            
            Gizmos.color = new Color(cortinaColor.r, cortinaColor.g, cortinaColor.b, selected ? 0.6f : 0.3f);
            Gizmos.DrawLine(closedWorldPos, openWorldPos);
            
            Vector3 direction = (openWorldPos - closedWorldPos).normalized;
            if (direction.magnitude > 0.01f)
            {
                float arrowSize = 0.3f;
                Vector3 arrowTip = openWorldPos - direction * arrowSize * 0.5f;
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * arrowSize * 0.3f;
                
                Gizmos.DrawLine(openWorldPos, arrowTip + right);
                Gizmos.DrawLine(openWorldPos, arrowTip - right);
            }
            
            if (selected)
            {
                #if UNITY_EDITOR
                string cortinaName = cortina.cortinaTransform.name;
                UnityEditor.Handles.Label(openWorldPos + Vector3.up * (cortinaSize.y * 0.5f + 0.3f), 
                    $"<color=#{ColorUtility.ToHtmlStringRGB(cortinaColor)}>{cortinaName}</color>\n<color=green>ABERTA</color>", 
                    new GUIStyle { 
                        normal = { textColor = Color.white }, 
                        fontSize = 11, 
                        fontStyle = FontStyle.Bold,
                        richText = true
                    });
                #endif
            }
        }
        
        if (selected && cortinas.Length >= 2)
        {
            CheckOverlap();
        }
    }
    
    private Bounds GetCortinaBounds(Transform cortinaTransform)
    {
        Renderer renderer = cortinaTransform.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        renderer = cortinaTransform.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        return new Bounds(cortinaTransform.position, cortinaTransform.localScale);
    }
    
    private void CheckOverlap()
    {
        #if UNITY_EDITOR
        if (cortinas == null || cortinas.Length < 2) return;
        
        for (int i = 0; i < cortinas.Length; i++)
        {
            for (int j = i + 1; j < cortinas.Length; j++)
            {
                if (cortinas[i].cortinaTransform == null || cortinas[j].cortinaTransform == null) continue;
                if (cortinas[i].closedPositionTransform == null || cortinas[j].closedPositionTransform == null) continue;
                
                Vector3 pos1 = cortinas[i].closedPositionTransform.position;
                Vector3 pos2 = cortinas[j].closedPositionTransform.position;
                
                Bounds bounds1 = GetCortinaBounds(cortinas[i].cortinaTransform);
                Bounds bounds2 = GetCortinaBounds(cortinas[j].cortinaTransform);
                
                bounds1.center = pos1;
                bounds2.center = pos2;
                
                if (bounds1.Intersects(bounds2))
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(pos1, pos2);
                    
                    Vector3 midPoint = (pos1 + pos2) * 0.5f;
                    UnityEditor.Handles.Label(midPoint + Vector3.up * 0.5f, 
                        "⚠️ SOBREPOSIÇÃO!", 
                        new GUIStyle { 
                            normal = { textColor = Color.magenta }, 
                            fontSize = 12, 
                            fontStyle = FontStyle.Bold 
                        });
                }
            }
        }
        #endif
    }
    
    #endregion
}
