
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/Database")]
public class DatabasePlayer : ScriptableObject{

    public float inputAccel;
    public float inputGravity;
    
    public float gravity;
    public float gravityGrounded = 1;
    
    public float minMouseY, maxMouseX;
    public LayerMask cameraColliderMash;
    public float cameraSphereRadius = 0.2f;

    [Header("Player")]
    public float playerSpeed = 1;
    public float playerJumpHeight;
    public float playerAirSpeed;
    public float playerMaxAirSpeed;
    public LayerMask PlayerMask;
    public float playerRespawnDuration;

    [Header("Player Roll")]
    public float playerRollSpeed;
    public AnimationCurve playerRollCurve;
    public float playerRollDuration;
    public float playerRollCooldownDuration;
    


}
