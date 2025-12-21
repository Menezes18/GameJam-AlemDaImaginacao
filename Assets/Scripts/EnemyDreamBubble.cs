using UnityEngine;

public class EnemyDreamBubble : WorldObject
{
    [Header("Bubble Movement")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lateralNoiseStrength = 0.5f;
    [SerializeField] private float lateralNoiseSpeed = 1f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 5f;

    [Header("Initial Push")]
    [SerializeField] private float pushDamping = 5f;

    private Vector3 initialVelocity;
    private float noiseOffset;
    private float noiseTime;

    private Collider bubbleCollider;

    private Enemy enemyParent;


    private TimeEntity timeEntity;

    protected override void Awake()
    {
        base.Awake();
        noiseOffset = Random.Range(0f, 100f);
        noiseTime = Random.Range(0f, 10f);

        timeEntity = GetComponent<TimeEntity>();
        bubbleCollider = GetComponent<Collider>();
    }

    public void AddForceAfterSpawn(Vector3 force, Enemy parent = null)
    {
        enemyParent = parent;
        initialVelocity += force;
    }

    private void Update()
    {
        if (WorldManager.Instance.CurrentWorld != WorldManager.WorldState.DreamWorld)
            return;

        // ⏸️ Se tempo local está parado, nem calcula
        if (timeEntity.LocalTimeScale <= 0f)
        {
            if (bubbleCollider != null && bubbleCollider.isTrigger)
                bubbleCollider.isTrigger = false;
            return;
        } else
        {
            if (bubbleCollider != null && !bubbleCollider.isTrigger)
                bubbleCollider.isTrigger = true;
        }

        MoveBubble();
        HandleLifetime();
    }

    private void MoveBubble()
    {
        float dt = timeEntity.LocalDeltaTime;

        // Movimento vertical
        Vector3 movement = Vector3.up * floatSpeed;

        // Noise lateral (tempo próprio, não afetado por slow)
        noiseTime += dt * lateralNoiseSpeed;
        float noise = Mathf.PerlinNoise(noiseTime, noiseOffset) - 0.5f;
        movement += Vector3.forward * noise * lateralNoiseStrength;

        // Impulso inicial (sopro)
        if (initialVelocity.sqrMagnitude > 0.0001f)
        {
            movement += initialVelocity;
            initialVelocity = Vector3.Lerp(
                initialVelocity,
                Vector3.zero,
                pushDamping * dt
            );
        }

        transform.position += movement * dt;
    }

    private void HandleLifetime()
    {
        lifetime -= timeEntity.LocalDeltaTime;
        if (lifetime <= 0f)
        {
            Pop();
        }
    }

    private void Pop(PlayerScript player = null)
    {
        // Notifica inimigo pai, se houver
        if (enemyParent != null && player != null)
        {
            enemyParent.WakeUp(player);
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("Player hit by dream bubble!");
        Pop(other.GetComponent<PlayerScript>());
    }

    override protected void UpdateState(WorldManager.WorldState currentWorld)
    {
        bool exists = DoesExistInWorld(currentWorld);
        // desttoy se sair do mundo dos sonhos
        if (!exists)
        {
            Destroy(gameObject);
            }

    }
}
