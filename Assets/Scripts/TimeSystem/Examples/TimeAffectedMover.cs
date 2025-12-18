using UnityEngine;


[RequireComponent(typeof(TimeEntity))]
public class TimeAffectedMover : MonoBehaviour, ITimeScaledUpdate
{
    #region Fields
    
    [Header("Movement Settings")]
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private float moveSpeed = 2f;
    
    [Header("Oscillation (Opcional)")]
    [SerializeField] private bool useOscillation = true;
    [SerializeField] private float oscillationDistance = 3f;
    
    private Vector3 startPosition;
    private float traveledDistance = 0f;
    private int direction = 1; // 1 = frente, -1 = trás
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        startPosition = transform.position;
    }
    
    #endregion
    
    #region ITimeScaledUpdate Implementation
    
    public void TimeScaledUpdate(float localDeltaTime)
    {
        if (useOscillation)
        {
            MoveWithOscillation(localDeltaTime);
        }
        else
        {
            MoveContinuous(localDeltaTime);
        }
    }
    
    #endregion
    
    #region Movement Logic
    
    private void MoveContinuous(float deltaTime)
    {
        transform.position += moveDirection.normalized * moveSpeed * deltaTime;
    }
    
    private void MoveWithOscillation(float deltaTime)
    {
        float movement = moveSpeed * deltaTime * direction;
        transform.position += moveDirection.normalized * movement;
        
        traveledDistance += Mathf.Abs(movement);
        
        if (traveledDistance >= oscillationDistance)
        {
            direction *= -1;
            traveledDistance = 0f;
        }
    }
    
    #endregion
}

