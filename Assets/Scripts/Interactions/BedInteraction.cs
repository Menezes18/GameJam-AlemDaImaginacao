using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class BedInteraction : MonoBehaviour, IInteractable
{
    #region Inspector
    
    [Header("Bed Settings")]
    [SerializeField] private float sleepDuration = 5f;
    [SerializeField] private bool wakeUpAutomatically = false;
    
    [Header("Sleep Position")]
    [SerializeField] private Transform sleepPosition;
    [SerializeField] private Vector3 customSleepPosition = new Vector3(90f, 1.61f, -1.75f);
    [SerializeField] private float customSleepRotationY = 24f;
    
    [Header("Wake Up Position")]
    [SerializeField] private Transform wakeUpPosition;
    
    [Header("Sleep Animation")]
    [SerializeField] private float moveToBedDuration = 1f;
    [SerializeField] private bool useAnimation = true;
    
    [Header("Interactable Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canPickUp = false;
    [SerializeField] private bool canAnalyze = false;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerEnterBed;
    [SerializeField] private UnityEvent onPlayerExitBed;
    
    #endregion
    
    #region Private
    
    private bool isPlayerSleeping = false;
    private PlayerScript sleepingPlayer = null;
    private float sleepTimer = 0f;
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private bool isMovingToBed = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Update()
    {
        if (isPlayerSleeping && sleepingPlayer != null)
        {
            UpdateSleep();
        }
    }
    
    #endregion
    
    #region Sleep System
    
    private void UpdateSleep()
    {
        if (wakeUpAutomatically)
        {
            sleepTimer -= Time.deltaTime;
            
            if (sleepTimer <= 0f)
            {
                WakeUpPlayer();
            }
        }
    }
    
    private void PutPlayerToSleep(PlayerScript player)
    {
        if (isPlayerSleeping || isMovingToBed) return;
        if (player == null) return;
        
        originalPlayerPosition = player.transform.position;
        originalPlayerRotation = player.transform.rotation;
        
        isPlayerSleeping = true;
        sleepingPlayer = player;
        sleepTimer = sleepDuration;
        
        Vector3 targetPosition = sleepPosition != null ? sleepPosition.position : customSleepPosition;
        Quaternion targetRotation = Quaternion.Euler(0f, customSleepRotationY, 0f);
        
        player.State = PlayerState.Sleeping;
        
        if (useAnimation && moveToBedDuration > 0f)
        {
            StartCoroutine(MovePlayerToBed(player, targetPosition, targetRotation));
        }
        else
        {
            player.MoveToPosition(targetPosition, targetRotation);
            player.SetLyingDown(true);
        }
        
        onPlayerEnterBed?.Invoke();
        
        Debug.Log($"🛏️ [BED] Player está dormindo... (Pressione E para acordar)");
    }
    
    private IEnumerator MovePlayerToBed(PlayerScript player, Vector3 targetPosition, Quaternion targetRotation)
    {
        isMovingToBed = true;
        
        Vector3 startPosition = player.transform.position;
        Quaternion startRotation = player.transform.rotation;
        float elapsedTime = 0f;
        
        while (elapsedTime < moveToBedDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveToBedDuration);
            float smoothT = t * t * (3f - 2f * t);
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, smoothT);
            Quaternion currentRotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            player.MoveToPosition(currentPosition, currentRotation);
            
            yield return null;
        }
        
        player.MoveToPosition(targetPosition, targetRotation);
        player.SetLyingDown(true);
        
        isMovingToBed = false;
    }
    
    private void WakeUpPlayer()
    {
        if (!isPlayerSleeping) return;
        if (sleepingPlayer == null) return;
        
        Debug.Log($"☀️ [BED] Player acordou!");
        
        sleepingPlayer.SetLyingDown(false);
        
        Vector3 finalWakeUpPosition;
        Quaternion finalWakeUpRotation;
        
        if (wakeUpPosition != null)
        {
            finalWakeUpPosition = wakeUpPosition.position;
            finalWakeUpRotation = wakeUpPosition.rotation;
        }
        else
        {
            Vector3 offsetFromBed = (sleepingPlayer.transform.position - transform.position).normalized * 1.5f;
            offsetFromBed.y = 0f;
            finalWakeUpPosition = transform.position + offsetFromBed;
            finalWakeUpRotation = originalPlayerRotation;
        }
        
        sleepingPlayer.MoveToPosition(finalWakeUpPosition, finalWakeUpRotation);
        
        sleepingPlayer.State = PlayerState.Default;
        
        onPlayerExitBed?.Invoke();
        
        sleepingPlayer = null;
        isPlayerSleeping = false;
        sleepTimer = 0f;
    }
    
    #endregion
    
    #region IInteractable
    
    public bool CanInteract()
    {
        return canInteract && !isMovingToBed;
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
        
        if (isPlayerSleeping && sleepingPlayer == player)
        {
            WakeUpPlayer();
        }
        else if (!isPlayerSleeping)
        {
            PutPlayerToSleep(player);
        }
    }
    
    #endregion
    
    #region Public API
    
    public bool IsPlayerSleeping => isPlayerSleeping;
    public float RemainingSleepTime => Mathf.Max(0f, sleepTimer);
    public float SleepProgress => isPlayerSleeping ? 1f - (sleepTimer / sleepDuration) : 0f;
    
    #endregion
}
