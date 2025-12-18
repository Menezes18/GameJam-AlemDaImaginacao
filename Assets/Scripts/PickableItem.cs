using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour, IPickable
{
    [Header("Settings")]
    [SerializeField] private bool canBePickedUp = true;
    [SerializeField] private bool canBeAnalyzed = false;
    [SerializeField] private string itemName = "Item";

    private bool _isPickedUp = false;

    public bool CanInteract()
    {
        return !_isPickedUp;
    }

    public bool CanPickUp()
    {
        return canBePickedUp && !_isPickedUp;
    }

    public bool CanAnalyze()
    {
        return canBeAnalyzed && !_isPickedUp;
    }

    public void OnPickUp(PlayerScript player)
    {
        if (_isPickedUp)
        {
            Debug.LogWarning($"⚠️ [PICKUP] {itemName} já está marcado como pego!");
            return;
        }
        
        _isPickedUp = true;
        Debug.Log($"✅ [PICKUP] {itemName} foi pego por {player.name}");
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void OnDrop()
    {
        Debug.Log($"📦 [DROP] Resetando flag _isPickedUp de {itemName}");
        _isPickedUp = false;
        Debug.Log($"✅ [DROP] {itemName} foi solto e pode ser pego novamente");
    }
}

