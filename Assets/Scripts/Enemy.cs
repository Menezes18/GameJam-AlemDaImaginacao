using UnityEngine;

[RequireComponent(typeof(TimeEntity))]
public class Enemy : WorldObject
{
    [Header("Attacks")]
    [SerializeField] private GameObject sleepBubblePrefab;
    [SerializeField] private GameObject fireBallPrefab;

    [SerializeField] private float bubbleSpawnInterval = 2f;
    [SerializeField] private float fireBallInterval = 1.2f;

    [SerializeField] private Transform attackSpawnPoint;


    [Header("Awareness")]
    [SerializeField] private float sleepRange = 12f;


    private bool isSleeping = true;
    private float attackTimer;

    private TimeEntity timeEntity;

    private PlayerScript target;

    protected override void Awake()
    {
        base.Awake();
        timeEntity = GetComponent<TimeEntity>();
        attackTimer = bubbleSpawnInterval;
    }

    private void Update()
    {
        if (!CanAct())
            return;

        UpdateAwareness();

        attackTimer -= timeEntity.LocalDeltaTime;

        if (attackTimer <= 0f)
        {
            if (isSleeping)
                SpawnSleepBubble();
            else
                ShootFireBall();

            ResetAttackTimer();
        }
    }


    // ========================
    // Conditions
    // ========================

    private bool CanAct()
    {
        return
            existenceRule == WorldExistence.DreamOnly &&
            WorldManager.Instance.CurrentWorld == WorldManager.WorldState.DreamWorld &&
            timeEntity.LocalTimeScale > 0f;
    }

    private void ResetAttackTimer()
    {
        attackTimer = isSleeping ? bubbleSpawnInterval : fireBallInterval;
    }

    // ========================
    // Attacks
    // ========================

    private void SpawnSleepBubble()
    {
        Vector3 spawnPos = attackSpawnPoint.position;

        EnemyDreamBubble bubble =
            Instantiate(sleepBubblePrefab, spawnPos, attackSpawnPoint.rotation)
            .GetComponent<EnemyDreamBubble>();

        bubble.AddForceAfterSpawn(
            attackSpawnPoint.forward * Random.Range(-4f, -7.5f),
            this
        );
    }

    private void ShootFireBall()
    {

        Vector3 spawnPos = attackSpawnPoint.position;

        Vector3 directionToPlayer =
            (target.transform.position - spawnPos).normalized;

        FireBall fireBall =
            Instantiate(
                fireBallPrefab,
                spawnPos,
                Quaternion.identity
            ).GetComponent<FireBall>();

        fireBall.Init(directionToPlayer);
    }

    // ========================
    // States
    // ========================

    public void WakeUp(PlayerScript player = null)
    {
        if (!isSleeping) return;
        target = player;

        isSleeping = false;
        ResetAttackTimer();
        Debug.Log("Enemy woke up");
    }

    public void EnemySleep()
    {
        if (isSleeping) return;

        isSleeping = true;
        ResetAttackTimer();
        Debug.Log("Enemy fell asleep");
    }

    protected override void UpdateState(WorldManager.WorldState currentWorld)
    {
        base.UpdateState(currentWorld);

        if (currentWorld == WorldManager.WorldState.DreamWorld)
            EnemySleep();
        else
            WakeUp();
    }

    private void UpdateAwareness()
    {
        if (target == null)
        {
            if (!isSleeping) EnemySleep();
            return;
        }

        float distance =
            Vector3.Distance(transform.position, target.transform.position);

        if (!isSleeping && distance >= sleepRange)
        {
            EnemySleep();
            target = null;
        }
    }

}
