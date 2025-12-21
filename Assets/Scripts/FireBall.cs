using UnityEngine;

[RequireComponent(typeof(TimeEntity))]
public class FireBall : WorldObject
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 4f;

    private Vector3 direction;
    private TimeEntity timeEntity;

    override protected void Awake()
    {
        base.Awake();
        timeEntity = GetComponent<TimeEntity>();
    }

    public void Init(Vector3 dir)
    {
        direction = dir.normalized;
    }

    private void Update()
    {
        if (timeEntity.LocalTimeScale <= 0f)
            return;

        transform.position +=
            direction * speed * timeEntity.LocalDeltaTime;

        lifetime -= timeEntity.LocalDeltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Debug.Log("Player hit by fireball!");
        other.GetComponent<PlayerScript>()?.RespawnPlayer();
        Destroy(gameObject);
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
