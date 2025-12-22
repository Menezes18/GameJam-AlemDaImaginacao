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

    public event Action<CallbackContext> OnTelekinesis;
    public void Telekinesis(CallbackContext obj) {this.OnTelekinesis?.Invoke(obj);}
    
    public event Action<CallbackContext> OnPointerPosition;
    public void PointAction(CallbackContext obj) {this.OnPointerPosition?.Invoke(obj);}
    
    public event Action<CallbackContext> OnScrollAction;
    public void ScrollAction(CallbackContext obj) {this.OnScrollAction?.Invoke(obj);}

}