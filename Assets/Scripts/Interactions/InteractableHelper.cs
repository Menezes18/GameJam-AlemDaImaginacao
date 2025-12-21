using UnityEngine;

/// <summary>
/// Helper para configurar objetos interagíveis corretamente
/// </summary>
public static class InteractableHelper
{
    /// <summary>
    /// Verifica se um GameObject está em uma layer que está incluída no LayerMask
    /// </summary>
    public static bool IsLayerInMask(GameObject obj, LayerMask mask)
    {
        if (obj == null) return false;
        int layer = obj.layer;
        return ((1 << layer) & mask.value) != 0;
    }
    
    /// <summary>
    /// Configura um GameObject para ser interagível (garante que tem Collider e está na layer correta)
    /// </summary>
    public static void SetupInteractable(GameObject obj, LayerMask interactableLayer)
    {
        if (obj == null) return;
        
        // Garante que tem Collider
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            // Adiciona BoxCollider se não tiver nenhum
            col = obj.AddComponent<BoxCollider>();
            Debug.Log($"📦 [SETUP] Adicionado BoxCollider a {obj.name}");
        }
        
        // Configura layer se necessário
        int targetLayer = GetFirstLayerInMask(interactableLayer);
        if (targetLayer != -1 && obj.layer != targetLayer)
        {
            obj.layer = targetLayer;
            Debug.Log($"🏷️ [SETUP] Configurada layer '{LayerMask.LayerToName(targetLayer)}' para {obj.name}");
        }
    }
    
    /// <summary>
    /// Retorna a primeira layer no mask (útil para configurar objetos)
    /// </summary>
    private static int GetFirstLayerInMask(LayerMask mask)
    {
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & mask.value) != 0)
            {
                return i;
            }
        }
        return -1;
    }
}

