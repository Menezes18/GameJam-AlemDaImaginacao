using UnityEngine;
using UnityEngine.Events;

public class PuzzleSlot : MonoBehaviour
{
    #region Inspector
    
    [Header("Puzzle Settings")]
    [SerializeField] private GameObject objetoEsperado;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private float distanciaTolerancia = 0.3f;
    [SerializeField] private float rotacaoTolerancia = 15f;
    [SerializeField] private bool autoSnap = true;
    [SerializeField] private float snapSpeed = 5f;
    
    [Header("Preview Settings")]
    [SerializeField] private bool showPreview = true;
    [SerializeField] private Color previewColorNormal = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color previewColorReady = new Color(0f, 1f, 0f, 0.7f);
    [SerializeField] private float previewRotationX = 0f;
    [SerializeField] private float previewRotationY = 0f;
    [SerializeField] private float previewRotationZ = 0f;
    [SerializeField] private float gizmoAxisSize = 0.4f;
    [SerializeField] private bool showAxisArrows = true;
    
    [Header("Status")]
    [SerializeField] private bool isCompleted = false;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPuzzleComplete;
    [SerializeField] private UnityEvent onPuzzleIncomplete;
    
    #endregion
    
    #region Private
    
    private TelekinesisObject objetoAtual;
    private bool isSnapping = false;
    private Vector3 worldTargetPosition;
    private Quaternion worldTargetRotation;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (targetPosition == null)
        {
            targetPosition = transform;
        }
        
        worldTargetPosition = targetPosition.position;
        worldTargetRotation = targetPosition.rotation * Quaternion.Euler(targetRotation);
    }
    
    private void Update()
    {
        if (isCompleted) return;
        
        CheckForNearbyObject();
        
        if (isSnapping && objetoAtual != null)
        {
            SnapObject();
        }
    }
    
    #endregion
    
    #region Puzzle Logic
    
    private void CheckForNearbyObject()
    {
        Collider[] colliders = Physics.OverlapSphere(worldTargetPosition, distanciaTolerancia * 2f);
        
        TelekinesisObject nearestObject = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            TelekinesisObject tkObject = col.GetComponent<TelekinesisObject>();
            if (tkObject == null) continue;
            
            if (objetoEsperado != null && tkObject.gameObject != objetoEsperado)
            {
                continue;
            }
            
            float distance = Vector3.Distance(tkObject.transform.position, worldTargetPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestObject = tkObject;
            }
        }
        
        if (nearestObject != null && nearestObject != objetoAtual)
        {
            objetoAtual = nearestObject;
            CheckIfReadyToSnap();
        }
        else if (nearestObject == null && objetoAtual != null)
        {
            isSnapping = false;
            objetoAtual = null;
        }
        else if (objetoAtual != null)
        {
            CheckIfReadyToSnap();
        }
    }
    
    private void CheckIfReadyToSnap()
    {
        if (objetoAtual == null) return;
        
        float distancia = Vector3.Distance(objetoAtual.transform.position, worldTargetPosition);
        float rotacaoDiff = Quaternion.Angle(objetoAtual.transform.rotation, worldTargetRotation);
        
        bool dentroDistancia = distancia <= distanciaTolerancia;
        bool dentroRotacao = rotacaoDiff <= rotacaoTolerancia;
        
        if (dentroDistancia && dentroRotacao && autoSnap && !isSnapping)
        {
            isSnapping = true;
            CompletePuzzle();
        }
    }
    
    private void SnapObject()
    {
        if (objetoAtual == null)
        {
            isSnapping = false;
            return;
        }
        
        Transform objTransform = objetoAtual.transform;
        
        objTransform.position = Vector3.Lerp(objTransform.position, worldTargetPosition, snapSpeed * Time.deltaTime);
        objTransform.rotation = Quaternion.Lerp(objTransform.rotation, worldTargetRotation, snapSpeed * Time.deltaTime);
        
        float distancia = Vector3.Distance(objTransform.position, worldTargetPosition);
        float rotacaoDiff = Quaternion.Angle(objTransform.rotation, worldTargetRotation);
        
        if (distancia < 0.01f && rotacaoDiff < 1f)
        {
            objTransform.position = worldTargetPosition;
            objTransform.rotation = worldTargetRotation;
            
            FixObjectInPlace();
            
            isSnapping = false;
        }
    }
    
    private void CompletePuzzle()
    {
        if (isCompleted) return;
        
        isCompleted = true;
        
        if (objetoAtual != null)
        {
            FixObjectInPlace();
        }
        
        onPuzzleComplete?.Invoke();
        
        Debug.Log($"✅ [PUZZLE] {gameObject.name} completo! Objeto {objetoAtual.name} encaixado e fixado corretamente.");
    }
    
    private void FixObjectInPlace()
    {
        if (objetoAtual == null) return;
        
        GameObject obj = objetoAtual.gameObject;
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
            Debug.Log($"🔒 [PUZZLE] Rigidbody removido de {obj.name}");
        }
        
        TelekinesisObject tkObj = obj.GetComponent<TelekinesisObject>();
        if (tkObj != null)
        {
            Destroy(tkObj);
            Debug.Log($"🔒 [PUZZLE] TelekinesisObject removido de {obj.name} - não pode mais ser pego com telekinesis");
        }
    }
    
    public void ResetPuzzle()
    {
        isCompleted = false;
        isSnapping = false;
        objetoAtual = null;
        onPuzzleIncomplete?.Invoke();
    }
    
    #endregion
    
    #region Public Methods
    
    public bool IsCompleted => isCompleted;
    
    public bool IsObjectNearby()
    {
        if (objetoAtual == null) return false;
        float distancia = Vector3.Distance(objetoAtual.transform.position, worldTargetPosition);
        return distancia <= distanciaTolerancia;
    }
    
    public bool IsRotationCorrect()
    {
        if (objetoAtual == null) return false;
        float rotacaoDiff = Quaternion.Angle(objetoAtual.transform.rotation, worldTargetRotation);
        return rotacaoDiff <= rotacaoTolerancia;
    }
    
    public float GetDistanceToTarget()
    {
        if (objetoAtual == null) return float.MaxValue;
        return Vector3.Distance(objetoAtual.transform.position, worldTargetPosition);
    }
    
    public float GetRotationDifference()
    {
        if (objetoAtual == null) return float.MaxValue;
        return Quaternion.Angle(objetoAtual.transform.rotation, worldTargetRotation);
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (!showPreview) return;
        
        Vector3 targetPos = targetPosition != null ? targetPosition.position : transform.position;
        Vector3 previewRotEuler = targetRotation + new Vector3(previewRotationX, previewRotationY, previewRotationZ);
        Quaternion targetRot = (targetPosition != null 
            ? targetPosition.rotation * Quaternion.Euler(previewRotEuler)
            : Quaternion.Euler(previewRotEuler));
        
        bool isReady = Application.isPlaying && objetoAtual != null && 
                      IsObjectNearby() && IsRotationCorrect();
        
        Color gizmoColor = isReady ? previewColorReady : previewColorNormal;
        
        if (objetoEsperado != null)
        {
            DrawObjectPreview(objetoEsperado, targetPos, targetRot, gizmoColor);
        }
        
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawWireSphere(targetPos, distanciaTolerancia);
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(targetPos, 0.05f);
        
        if (showAxisArrows)
        {
            DrawRotationAxes(targetPos, targetRot, gizmoAxisSize);
        }
    }
    
    private void DrawObjectPreview(GameObject obj, Vector3 position, Quaternion targetRotation, Color color)
    {
        #if UNITY_EDITOR
        if (obj == null) return;
        
        Transform objTransform = obj.transform;
        Quaternion objCurrentRot = objTransform.rotation;
        Quaternion rotationDiff = targetRotation * Quaternion.Inverse(objCurrentRot);
        
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0) return;
        
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            if (r != null) combinedBounds.Encapsulate(r.bounds);
        }
        Vector3 objCenterWorld = combinedBounds.center;
        Vector3 objCenterLocal = objTransform.InverseTransformPoint(objCenterWorld);
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) continue;
            
            Mesh mesh = meshFilter.sharedMesh;
            Transform rendererTransform = renderer.transform;
            
            Vector3 rendererLocalPos = objTransform.InverseTransformPoint(rendererTransform.position);
            Vector3 rendererScale = rendererTransform.lossyScale;
            Quaternion rendererLocalRot = Quaternion.Inverse(objCurrentRot) * rendererTransform.rotation;
            
            Vector3 offsetFromObjCenter = rendererLocalPos - objCenterLocal;
            Vector3 rotatedOffset = rotationDiff * offsetFromObjCenter;
            Vector3 previewPos = position + rotatedOffset;
            
            Quaternion previewRot = targetRotation * rendererLocalRot;
            
            DrawMeshWireframe(mesh, previewPos, previewRot, rendererScale, color);
        }
        #endif
    }
    
    private void DrawMeshWireframe(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        #if UNITY_EDITOR
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        if (vertices.Length == 0 || triangles.Length == 0) return;
        
        Vector3[] transformedVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 scaled = Vector3.Scale(vertices[i], scale);
            transformedVertices[i] = position + rotation * scaled;
        }
        
        UnityEditor.Handles.color = new Color(color.r, color.g, color.b, color.a * 0.8f);
        
        int maxTriangles = Mathf.Min(triangles.Length / 3, 1000);
        for (int i = 0; i < maxTriangles; i++)
        {
            int idx = i * 3;
            if (idx + 2 >= triangles.Length) break;
            
            int v0 = triangles[idx];
            int v1 = triangles[idx + 1];
            int v2 = triangles[idx + 2];
            
            if (v0 >= transformedVertices.Length || v1 >= transformedVertices.Length || v2 >= transformedVertices.Length)
                continue;
            
            UnityEditor.Handles.DrawLine(transformedVertices[v0], transformedVertices[v1]);
            UnityEditor.Handles.DrawLine(transformedVertices[v1], transformedVertices[v2]);
            UnityEditor.Handles.DrawLine(transformedVertices[v2], transformedVertices[v0]);
        }
        
        Bounds bounds = mesh.bounds;
        Vector3 boundsSize = Vector3.Scale(bounds.size, scale);
        Vector3 boundsCenter = position + rotation * Vector3.Scale(bounds.center, scale);
        
        UnityEditor.Handles.color = new Color(color.r, color.g, color.b, color.a * 0.3f);
        UnityEditor.Handles.DrawWireCube(boundsCenter, boundsSize);
        #endif
    }
    
    private void DrawRotationAxes(Vector3 position, Quaternion rotation, float size)
    {
        #if UNITY_EDITOR
        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.8f);
        UnityEditor.Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(forward), size * 1.2f, EventType.Repaint);
        UnityEditor.Handles.DrawLine(position, position + forward * size);
        
        UnityEditor.Handles.color = new Color(0f, 1f, 0f, 0.8f);
        UnityEditor.Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(up), size * 1.2f, EventType.Repaint);
        UnityEditor.Handles.DrawLine(position, position + up * size);
        
        UnityEditor.Handles.color = new Color(0f, 0f, 1f, 0.8f);
        UnityEditor.Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(right), size * 1.2f, EventType.Repaint);
        UnityEditor.Handles.DrawLine(position, position + right * size);
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 targetPos = targetPosition != null ? targetPosition.position : transform.position;
        Vector3 previewRotEuler = targetRotation + new Vector3(previewRotationX, previewRotationY, previewRotationZ);
        Quaternion targetRot = (targetPosition != null 
            ? targetPosition.rotation * Quaternion.Euler(previewRotEuler)
            : Quaternion.Euler(previewRotEuler));
        
        #if UNITY_EDITOR
        string info = $"Puzzle Slot: {gameObject.name}";
        info += $"\nDistância: {distanciaTolerancia}m | Rotação: {rotacaoTolerancia}°";
        if (objetoEsperado != null)
        {
            info += $"\nObjeto: {objetoEsperado.name}";
        }
        info += $"\nRotação Base: ({targetRotation.x:F1}°, {targetRotation.y:F1}°, {targetRotation.z:F1}°)";
        info += $"\nPreview Offset: ({previewRotationX:F1}°, {previewRotationY:F1}°, {previewRotationZ:F1}°)";
        
        if (Application.isPlaying && objetoAtual != null)
        {
            float dist = GetDistanceToTarget();
            float rotDiff = GetRotationDifference();
            info += $"\n\nStatus:";
            info += $"\nDistância: {dist:F3}m {(dist <= distanciaTolerancia ? "✅" : "❌")}";
            info += $"\nRotações: {rotDiff:F1}° {(rotDiff <= rotacaoTolerancia ? "✅" : "❌")}";
        }
        
        UnityEditor.Handles.Label(targetPos + Vector3.up * 0.8f, info,
            new GUIStyle { 
                normal = { textColor = Color.yellow }, 
                fontSize = 10, 
                fontStyle = FontStyle.Bold 
            });
        
        DrawRotationAxes(targetPos, targetRot, gizmoAxisSize * 1.5f);
        #endif
    }
    
    #endregion
}
