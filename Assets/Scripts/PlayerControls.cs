using static UnityEngine.InputSystem.InputAction;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour {
    
    [SerializeField] PlayerInputSO PlayerInputSO;
    [SerializeField] PlayerControlsSO PlayerControlsSO;
    [SerializeField] DatabasePlayer db;
    [SerializeField] PlayerScript playerScript;

    [SerializeField]
    PlayerInput _playerInput;
    float _rawX, _rawY;
    float _x, _y;

    private float _mouse;
    private void Start(){
        
        playerScript = GetComponent<PlayerScript>();
        
        PlayerInputSO.OnMove += PlayerInputSO_OnMove;
        PlayerInputSO.OnJump += PlayerInputSO_OnJump;
        PlayerInputSO.OnRoll += PlayerInputSO_OnRoll;
        PlayerInputSO.OnLook += PlayerInputSO_OnLook;
        PlayerInputSO.OnPickUp += PlayerInputSO_OnPickUp;
        PlayerInputSO.OnAnalyze += PlayerInputSO_OnAnalyze;
        PlayerInputSO.OnTimeControl += PlayerInputSO_OnTimeControl;
        PlayerInputSO.OnTelekinesis += PlayerInputSO_OnTelekinesis;
        PlayerInputSO.OnScrollAction += PlayerInputSO_OnScrollAction;
        PlayerInputSO.OnPointerPosition += PlayerInputSO_OnPointerPosition;
    }

    private void OnDestroy(){
        PlayerInputSO.OnMove -= PlayerInputSO_OnMove;
        PlayerInputSO.OnJump -= PlayerInputSO_OnJump;
        PlayerInputSO.OnRoll -= PlayerInputSO_OnRoll;
        PlayerInputSO.OnLook -= PlayerInputSO_OnLook;
        PlayerInputSO.OnPickUp -= PlayerInputSO_OnPickUp;
        PlayerInputSO.OnAnalyze -= PlayerInputSO_OnAnalyze;
        PlayerInputSO.OnTimeControl -= PlayerInputSO_OnTimeControl;
        PlayerInputSO.OnTelekinesis -= PlayerInputSO_OnTelekinesis;
        PlayerInputSO.OnScrollAction -= PlayerInputSO_OnScrollAction;
        PlayerInputSO.OnPointerPosition -= PlayerInputSO_OnPointerPosition;
    }
    
    
    private void Update(){

        if (playerScript.panel)
        {
            _rawX = 0;
            _rawY = 0;
            _x = 0;
            _y = 0;
            PlayerControlsSO.Move(new Vector2(_x, _y), new Vector2(_rawX, _rawY));
            return;
        }

        if (_rawX == 0)
            _x = Mathf.MoveTowards(_x, 0, db.inputGravity * Time.deltaTime);
        else
            _x = Mathf.MoveTowards(_x, _rawX, db.inputAccel * Time.deltaTime);
        
        if(_rawY == 0)
            _y = Mathf.MoveTowards(_y, 0, db.inputGravity * Time.deltaTime);
        else
            _y = Mathf.MoveTowards(_y, _rawY, db.inputAccel * Time.deltaTime);
        
        PlayerControlsSO.Move(new Vector2(_x, _y), new Vector2(_rawX, _rawY));
    }
    private void PlayerInputSO_OnMove(CallbackContext obj)
    {
        if (playerScript.panel)
        {
            _rawX = 0;
            _rawY = 0;
            return;
        }
        _rawX = obj.ReadValue<Vector2>().x;
        _rawY = obj.ReadValue<Vector2>().y;
        
    }
    private void PlayerInputSO_OnLook(CallbackContext obj)
    {
        if (playerScript.panel) return;
        PlayerControlsSO.Look(obj.ReadValue<Vector2>());
    }
    private void PlayerInputSO_OnJump(CallbackContext obj)
    {
        if(obj.performed){
            PlayerControlsSO.Jump();
        }

    }
      
    private void PlayerInputSO_OnRoll(CallbackContext obj)
    {
        if (playerScript.panel) return;
        if(obj.performed)
            PlayerControlsSO.Roll();
    }
    private void EventMove(Vector2 obj){
        obj = obj.normalized;
        
        _rawX = obj.x;
        _rawY = obj.y;
    }
    private void Telekinesis(bool value){
        if (playerScript.panel) return;
        PlayerControlsSO.Telekinesis(value);
    }
    private void PointAction(Vector2 point){
        PlayerControlsSO.PointerPosition(point);
    }
    private void ScrollAction(Vector2 scroll){
        PlayerControlsSO.ScrollAction(scroll);
    }
    public Vector2 GetInput()
    {
        return new Vector2(_x, _y);
    }
    
    // Bridge methods for PlayerInput (Send Messages). Ensure action names match the Input Actions.
    public void OnMove(CallbackContext ctx)
    {
        PlayerInputSO?.Move(ctx);
    }

    public void OnLook(CallbackContext ctx)
    {
        PlayerInputSO?.Look(ctx);
    }

    public void OnJump(CallbackContext ctx)
    {
        PlayerInputSO?.Jump(ctx);
    }

    public void OnRoll(CallbackContext ctx)
    {
        PlayerInputSO?.Roll(ctx);
    }

    public void OnPickUp(CallbackContext ctx)
    {
        PlayerInputSO?.PickUp(ctx);
    }

    public void OnAnalyze(CallbackContext ctx)
    {
        PlayerInputSO?.Analyze(ctx);
    }

    public void OnTimeControl(CallbackContext ctx)
    {
        PlayerInputSO?.TimeControl(ctx);
    }

    // Conecta a ação "Interact" (tecla E) ao sistema de pegar objetos
    public void OnInteract(CallbackContext ctx)
    {
        if (playerScript.panel) return;
        if (ctx.performed)
            PlayerControlsSO.PickUp();
    }

    private void PlayerInputSO_OnPickUp(CallbackContext obj)
    {
        if (playerScript.panel) return;
        if (obj.performed)
            PlayerControlsSO.PickUp();
    }

    private void PlayerInputSO_OnAnalyze(CallbackContext obj)
    {
        if (playerScript.panel) return;
        if (obj.performed)
            PlayerControlsSO.Analyze();
    }

    private void PlayerInputSO_OnTimeControl(CallbackContext obj)
    {
        if (playerScript.panel) return;
        // Verifica se há interação disponível (prioridade)
        // Se o player estiver segurando objeto, não controla o tempo
        if (playerScript != null && playerScript.IsHoldingObject)
        {
            return; // Não controla tempo enquanto segura objeto
        }
        
        if (obj.performed)
            PlayerControlsSO.TimeControl();
    }
    private void PlayerInputSO_OnTelekinesis(CallbackContext obj)
    {
        if (playerScript.panel) return;
        if (obj.performed || obj.canceled) Telekinesis(obj.performed);
    }
    private void PlayerInputSO_OnPointerPosition(CallbackContext obj)
    {
        if (playerScript.panel) return;
        if (obj.performed || obj.canceled) PointAction(obj.ReadValue<Vector2>());
    }
    private void PlayerInputSO_OnScrollAction(CallbackContext obj)
    {
        if (playerScript.panel) return;
        if (obj.performed || obj.canceled) ScrollAction(obj.ReadValue<Vector2>());
        
    }
}
