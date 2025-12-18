using UnityEngine;


[RequireComponent(typeof(Collider))]
public class PickableAndAnalyzableItem : MonoBehaviour, IPickable, IAnalyzable
{
    [Header("Settings")]
    [SerializeField] private bool canBePickedUp = true;
    [SerializeField] private bool canBeAnalyzed = true;
    [SerializeField] private string itemName = "Item Especial";
    [SerializeField] private string analysisInfo = "Este item contém informações importantes...";

    private bool _isPickedUp = false;
    private bool _hasBeenAnalyzed = false;

    public bool CanInteract()
    {
        return !_isPickedUp;
    }

    public bool CanPickUp()
    {
        return canBePickedUp && !_isPickedUp;
    }

    // Pode analisar mesmo depois de pegar (se ainda estiver segurando)
    public bool CanAnalyze()
    {
        return canBeAnalyzed;
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

    public void OnAnalyze(PlayerScript player)
    {
        if (_isPickedUp)
        {
            Debug.Log($"🔍 [ANALYZE] {player.name} está analisando {itemName} que está segurando");
        }
        else
        {
            Debug.Log($"🔍 [ANALYZE] {player.name} começou a analisar {itemName}");
        }
    }

    public void OnAnalyzeComplete(PlayerScript player)
    {
        _hasBeenAnalyzed = true;
        Debug.Log($"✅ [ANALYZE] Análise completa de {itemName}: {analysisInfo}");
        
        if (_isPickedUp)
        {
            Debug.Log($"💡 [ANALYZE] {itemName} revelou seu segredo enquanto era segurado!");
        }
    }

    public void OnDrop()
    {
        Debug.Log($"📦 [DROP] Resetando flag _isPickedUp de {itemName}");
        _isPickedUp = false;
        Debug.Log($"✅ [DROP] {itemName} foi solto e pode ser pego novamente");
    }
}

