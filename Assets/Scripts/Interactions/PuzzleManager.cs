using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class PuzzleManager : MonoBehaviour
{
    #region Inspector
    
    [Header("Puzzle Slots")]
    [SerializeField] private PuzzleSlot[] puzzleSlots = new PuzzleSlot[0];
    
    [Header("Status")]
    [SerializeField] private bool allPuzzlesCompleted = false;
    [SerializeField] private float checkInterval = 0.5f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onAllPuzzlesComplete;
    [SerializeField] private UnityEvent onPuzzleCompleted;
    [SerializeField] private UnityEvent onPuzzleUncompleted;
    
    #endregion
    
    #region Private
    
    private float checkTimer = 0f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (puzzleSlots.Length == 0)
        {
            puzzleSlots = GetComponentsInChildren<PuzzleSlot>();
        }
    }
    
    private void Start()
    {
        CheckAllPuzzlesCompleted();
    }
    
    private void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            CheckAllPuzzlesCompleted();
            checkTimer = checkInterval;
        }
    }
    
    #endregion
    
    #region Puzzle Management
    
    private void CheckAllPuzzlesCompleted()
    {
        int completedCount = GetCompletedCount();
        bool wasAllCompleted = allPuzzlesCompleted;
        allPuzzlesCompleted = completedCount >= puzzleSlots.Length && puzzleSlots.Length > 0;
        
        if (allPuzzlesCompleted && !wasAllCompleted)
        {
            onAllPuzzlesComplete?.Invoke();
            Debug.Log($"🎉 [PUZZLE MANAGER] Todos os {puzzleSlots.Length} puzzles foram completados!");
        }
        else if (!allPuzzlesCompleted && wasAllCompleted)
        {
            onPuzzleUncompleted?.Invoke();
        }
        else if (completedCount > 0 && !wasAllCompleted)
        {
            onPuzzleCompleted?.Invoke();
        }
    }
    
    public void ResetAllPuzzles()
    {
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                slot.ResetPuzzle();
            }
        }
        
        allPuzzlesCompleted = false;
        CheckAllPuzzlesCompleted();
    }
    
    public void ValidateSlots()
    {
        puzzleSlots = puzzleSlots.Where(slot => slot != null).ToArray();
        
        if (puzzleSlots.Length == 0)
        {
            puzzleSlots = GetComponentsInChildren<PuzzleSlot>();
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public int GetCompletedCount()
    {
        return puzzleSlots.Count(slot => slot != null && slot.IsCompleted);
    }
    
    public int GetTotalCount()
    {
        return puzzleSlots.Length;
    }
    
    public float GetProgress()
    {
        if (puzzleSlots.Length == 0) return 0f;
        return (float)GetCompletedCount() / puzzleSlots.Length;
    }
    
    public bool AreAllPuzzlesCompleted => allPuzzlesCompleted;
    
    public PuzzleSlot[] GetAllSlots() => puzzleSlots;
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (puzzleSlots == null || puzzleSlots.Length == 0) return;
        
        #if UNITY_EDITOR
        int completed = GetCompletedCount();
        int total = puzzleSlots.Length;
        
        Vector3 managerPos = transform.position;
        
        string statusText = $"Puzzle Manager\n{completed}/{total} Completos";
        if (allPuzzlesCompleted)
        {
            statusText += "\n✅ TODOS COMPLETOS!";
        }
        
        UnityEditor.Handles.Label(managerPos + Vector3.up * 1.5f, statusText,
            new GUIStyle { 
                normal = { textColor = allPuzzlesCompleted ? Color.green : Color.yellow }, 
                fontSize = 12, 
                fontStyle = FontStyle.Bold 
            });
        
        Gizmos.color = allPuzzlesCompleted ? Color.green : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(managerPos, 0.2f);
        #endif
    }
    
    #endregion
}

