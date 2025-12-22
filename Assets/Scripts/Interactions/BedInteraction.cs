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
    
    [Header("Dream System")]
    [SerializeField] private GameObject sleepingBodyModel; // Modelo do corpo dormindo que aparece na cama
    [SerializeField] private float dreamTransitionDelay = 1f; // Delay antes de entrar no sonho
    [SerializeField] private float bedSideOffset = 1.5f; // Distância ao lado da cama para ver o corpo dormindo
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerStartSleeping; // Evento quando o player começa a dormir (para configurar coisas no sonho)
    [SerializeField] private UnityEvent onPlayerEnterBed;
    [SerializeField] private UnityEvent onPlayerExitBed;
    [SerializeField] private UnityEvent onPlayerEnterDream; // Evento quando entra no sonho
    [SerializeField] private UnityEvent onPlayerExitDream; // Evento quando sai do sonho
    
    #endregion
    
    #region Private
    
    private bool isPlayerSleeping = false;
    private bool isInDream = false; // Se o player está no mundo do sonho
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
        isInDream = false;
        
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
        
        // Chama o evento quando começa a dormir (para configurar coisas no sonho)
        onPlayerStartSleeping?.Invoke();
        
        // Inicia a transição para o sonho
        StartCoroutine(EnterDreamSequence(player, targetPosition, targetRotation));
        
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
        
        // Se está no sonho, precisa sair do sonho primeiro
        if (isInDream)
        {
            StartCoroutine(ExitDreamSequence());
        }
        else
        {
            // Acordar normalmente (ainda não entrou no sonho)
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
    }
    
    #endregion
    
    #region Dream System
    
    private IEnumerator EnterDreamSequence(PlayerScript player, Vector3 bedPosition, Quaternion bedRotation)
    {
        // Aguarda a animação de dormir terminar
        yield return new WaitForSeconds(dreamTransitionDelay);
        
        // Passo 1: Transporta o player para o lado da cama (no mundo real ainda)
        // Calcula a posição ao lado da cama baseado no wakeUpPosition ou na posição da cama
        Vector3 bedSidePosition;
        Quaternion bedSideRotation;
        

        
        // Ativa o modelo dormindo na cama quando o player é teleportado para fora
        if (sleepingBodyModel != null)
        {
            sleepingBodyModel.transform.position = bedPosition;
            sleepingBodyModel.transform.rotation = bedRotation;
            sleepingBodyModel.SetActive(true);
            Debug.Log($"🌙 [DREAM] Modelo dormindo ativado na cama");
        }
        
        if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != WorldManager.WorldState.DreamWorld)
        {
                WorldManager.Instance.ToggleWorld();
        }
        // Aguarda 1 segundo antes de entrar no sonho
        yield return new WaitForSeconds(0.8f);
        
        // Passo 3: Transporta o player para o ponto de "acordou" no sonho (usa wakeUpPosition)
        if (wakeUpPosition != null)
        {

            Vector3 directionToBed = (bedPosition - wakeUpPosition.position).normalized;
            bedSidePosition = bedPosition + directionToBed * bedSideOffset;
            bedSidePosition.y = wakeUpPosition.position.y; // Mantém a altura do wakeUpPosition
            bedSideRotation = Quaternion.LookRotation(bedPosition - bedSidePosition);
        }
        else
        {
            // Calcula baseado na posição da cama
            Vector3 offsetFromBed = (player.transform.position - bedPosition).normalized;
            offsetFromBed.y = 0f;
            bedSidePosition = bedPosition + offsetFromBed * bedSideOffset;
            bedSideRotation = Quaternion.LookRotation(bedPosition - bedSidePosition);
        }
        
        player.MoveToPosition(bedSidePosition, bedSideRotation);
        player.State = PlayerState.Default; // Permite movimento/interação
        Debug.Log($"🌙 [DREAM] Player apareceu ao lado da cama");
        player.MoveToPosition(wakeUpPosition.position, wakeUpPosition.rotation);
        isInDream = true;
            
        onPlayerEnterDream?.Invoke();
            
        Debug.Log($"🌙 [DREAM] Player entrou no sonho e apareceu na posição de acordou");

    }
    
    private IEnumerator ExitDreamSequence()
    {
        if (sleepingPlayer == null) yield break;
        
        Debug.Log($"☀️ [DREAM] Saindo do sonho...");
        
        if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != WorldManager.WorldState.RealWorld)
        {
            WorldManager.Instance.ToggleWorld();
        }
        
        yield return new WaitForSeconds(0.1f); // Pequeno delay para a transição de mundo
        
        if (sleepingBodyModel != null)
        {
            sleepingBodyModel.SetActive(false);
        }
        
        // Volta o player para a cama (posição de dormir)
        Vector3 bedPosition = sleepPosition != null ? sleepPosition.position : customSleepPosition;
        Quaternion bedRotation = Quaternion.Euler(0f, customSleepRotationY, 0f);
        sleepingPlayer.MoveToPosition(bedPosition, bedRotation);
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
        
        onPlayerExitDream?.Invoke();
        onPlayerExitBed?.Invoke();
        
        isInDream = false;
        sleepingPlayer = null;
        isPlayerSleeping = false;
        sleepTimer = 0f;
        
        Debug.Log($"☀️ [DREAM] Player acordou completamente!");
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
        
        // Se está dormindo e no sonho, pode acordar interagindo com a cama
        if (isPlayerSleeping && sleepingPlayer == player)
        {
            // Se está no sonho, precisa interagir com a cama para acordar
            if (isInDream)
            {
                WakeUpPlayer();
            }
            else
            {
                // Ainda não entrou no sonho, pode acordar normalmente
                WakeUpPlayer();
            }
        }
        else if (!isPlayerSleeping)
        {
            PutPlayerToSleep(player);
        }
    }
    
    #endregion
    
    #region Public API
    
    public bool IsPlayerSleeping => isPlayerSleeping;
    public bool IsInDream => isInDream;
    public float RemainingSleepTime => Mathf.Max(0f, sleepTimer);
    public float SleepProgress => isPlayerSleeping ? 1f - (sleepTimer / sleepDuration) : 0f;
    
    #endregion
}
