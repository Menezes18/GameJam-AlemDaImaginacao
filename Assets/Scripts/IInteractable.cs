using UnityEngine;

// Interface base para objetos que podem ser interagidos
public interface IInteractable
{
    bool CanInteract();
    bool CanPickUp();
    bool CanAnalyze();
    
    // Método genérico de interação (E)
    void OnInteract(PlayerScript player);
}

// Objetos que podem ser pegos e segurados pelo jogador
public interface IPickable : IInteractable
{
    void OnPickUp(PlayerScript player);
    GameObject GetGameObject();
}

// Objetos que podem ser analisados pelo jogador
public interface IAnalyzable : IInteractable
{
    void OnAnalyze(PlayerScript player);
    void OnAnalyzeComplete(PlayerScript player);
}

