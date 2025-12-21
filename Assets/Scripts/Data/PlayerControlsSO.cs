using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "PlayerControlsSO", menuName = "Player/PlayerControlsSO")]
public class PlayerControlsSO : ScriptableObject {

    public event Action<Vector2, Vector2> OnMove;
    public event Action<Vector2> OnLook;
    public event Action OnJump;
    public event Action OnRoll;
    public event Action OnPickUp;
    public event Action OnAnalyze;
    public event Action OnTimeControl;

    public void Move(Vector2 move, Vector2 raw) { OnMove?.Invoke(move, raw); }
    public void Look(Vector2 look) { OnLook?.Invoke(look); }
    public void Jump() { OnJump?.Invoke(); }
    public void Roll() { OnRoll?.Invoke(); }
    public void PickUp() { OnPickUp?.Invoke(); }
    public void Analyze() { OnAnalyze?.Invoke(); }
    public void TimeControl() { OnTimeControl?.Invoke(); }

}
