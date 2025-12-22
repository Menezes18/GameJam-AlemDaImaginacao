using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorldExistence
{
    RealOnly,
    DreamOnly,
    Both
}


public class WorldObject : MonoBehaviour
{
    [Header("World Rules")]
    public WorldExistence existenceRule;

    [Header("Visuals")]
    public Material realWorldMaterial;
    public Material dreamWorldMaterial;

    private List<Renderer> objectRenderers;
    private List<Collider> objectColliders;

    virtual protected void Awake()
    {
        objectRenderers = GetComponentsInChildren<Renderer>().ToList();
        objectColliders = GetComponentsInChildren<Collider>().ToList();
    }

    private void Start()
    {
        UpdateState(WorldManager.Instance.CurrentWorld);
        WorldManager.Instance.OnWorldChanged += WorldManager_HandleWorldChange;
    }

    private void OnDisable()
    {
        WorldManager.Instance.OnWorldChanged -= WorldManager_HandleWorldChange;
    }

    private void WorldManager_HandleWorldChange(object sender, WorldManager.WorldState newWorld)
    {
        UpdateState(newWorld);
    }

    virtual protected void UpdateState(WorldManager.WorldState currentWorld)
    {
        bool exists = DoesExistInWorld(currentWorld);
        Material targetMaterial = currentWorld == WorldManager.WorldState.RealWorld ? realWorldMaterial : dreamWorldMaterial;

        foreach (var objectRenderer in objectRenderers)
        {
            objectRenderer.enabled = exists;
            if (exists && targetMaterial != null)
            {
                objectRenderer.material =
                    currentWorld == WorldManager.WorldState.RealWorld
                    ? realWorldMaterial
                    : dreamWorldMaterial;
            }
        }

        foreach (var objectCollider in objectColliders)
        {
            objectCollider.enabled = exists;
        }

    }

    protected bool DoesExistInWorld(WorldManager.WorldState world)
    {
        switch (existenceRule)
        {
            case WorldExistence.RealOnly:
                return world == WorldManager.WorldState.RealWorld;

            case WorldExistence.DreamOnly:
                return world == WorldManager.WorldState.DreamWorld;

            case WorldExistence.Both:
                return true;
        }

        return false;
    }
}
