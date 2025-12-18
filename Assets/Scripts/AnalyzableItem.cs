using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AnalyzableItem : MonoBehaviour, IAnalyzable
{
    [Header("Settings")]
    [SerializeField] private bool canBePickedUp = false;
    [SerializeField] private bool canBeAnalyzed = true;
    [SerializeField] private string itemName = "Objeto Analisável";
    [SerializeField] private string analysisInfo = "Este é um objeto interessante...";

    private bool _hasBeenAnalyzed = false;

    public bool CanInteract()
    {
        return true;
    }

    public bool CanPickUp()
    {
        return canBePickedUp;
    }

    public bool CanAnalyze()
    {
        return canBeAnalyzed;
    }

    public void OnAnalyze(PlayerScript player)
    {
        Debug.Log($"🔍 [ANALYZE] {player.name} começou a analisar {itemName}");
    }

    public void OnAnalyzeComplete(PlayerScript player)
    {
        _hasBeenAnalyzed = true;
        Debug.Log($"✅ [ANALYZE] Análise completa de {itemName}: {analysisInfo}");
    }
}

