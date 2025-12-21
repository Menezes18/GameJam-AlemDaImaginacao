using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "PlayerInputSO", menuName = "Player/PlayerInputSO")]
public class PlayerInputSO : ScriptableObject{
    
    public event Action<CallbackContext> OnMove;
    public void Move(CallbackContext obj) {this.OnMove?.Invoke(obj);}
    
    public event Action<CallbackContext> OnLook;
    public void Look(CallbackContext obj) {this.OnLook?.Invoke(obj);}
    
    public event Action<CallbackContext> OnJump;
    public void Jump(CallbackContext obj) {this.OnJump?.Invoke(obj);}
    
    public event Action<CallbackContext> OnRoll;
    public void Roll(CallbackContext obj) {this.OnRoll?.Invoke(obj);}
    
    public event Action<CallbackContext> OnPickUp;
    public void PickUp(CallbackContext obj) {this.OnPickUp?.Invoke(obj);}
    
    public event Action<CallbackContext> OnAnalyze;
    public void Analyze(CallbackContext obj) {this.OnAnalyze?.Invoke(obj);}
    
    public event Action<CallbackContext> OnTimeControl;
    public void TimeControl(CallbackContext obj) {this.OnTimeControl?.Invoke(obj);}
    
}