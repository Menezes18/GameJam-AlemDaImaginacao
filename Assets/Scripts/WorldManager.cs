using System;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    public EventHandler<WorldState> OnWorldChanged;

    public enum WorldState
    {
        RealWorld,
        DreamWorld
    }

    public WorldState CurrentWorld { get; private set; } = WorldState.RealWorld;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ToggleWorld()
    {
        if (CurrentWorld == WorldState.RealWorld)
        {
            SwitchToDreamWorld();
        }
        else
        {
            SwitchToRealWorld();
        }

        OnWorldChanged?.Invoke(this, CurrentWorld);
    }

    private void SwitchToDreamWorld()
    {
        CurrentWorld = WorldState.DreamWorld;
        // Additional logic for switching to the dream world can be added here
        Debug.Log("Switched to Dream World");
    }

    private void SwitchToRealWorld()
    {
        CurrentWorld = WorldState.RealWorld;
        // Additional logic for switching to the real world can be added here
        Debug.Log("Switched to Real World");
    }
}
