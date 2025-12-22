using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WorldTransitionUI : MonoBehaviour
{
    public static WorldTransitionUI Instance;

    [SerializeField] private Image fadeImage;
    
    [Header("Day Transition (Real World)")]
    [SerializeField] private Color dayTransitionColor = Color.black; // Cor da transição para o dia
    [SerializeField] private float dayFadeInDuration = 0.5f; // Tempo para escurecer ao entrar no dia
    [SerializeField] private float dayHoldDuration = 0.3f; // Tempo que fica preto no dia
    [SerializeField] private float dayFadeOutDuration = 0.5f; // Tempo para clarear no dia
    
    [Header("Night Transition (Dream World)")]
    [SerializeField] private Color nightTransitionColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Cor da transição para a noite (azul escuro)
    [SerializeField] private float nightFadeInDuration = 0.8f; // Tempo para escurecer ao entrar na noite
    [SerializeField] private float nightHoldDuration = 0.5f; // Tempo que fica escuro na noite
    [SerializeField] private float nightFadeOutDuration = 0.8f; // Tempo para clarear na noite
    

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================
    // PUBLIC API
    // =========================

    public void TransitionToDream(System.Action onMidFade)
    {
        StartCoroutine(FadeRoutine(nightTransitionColor, onMidFade, nightFadeInDuration, nightHoldDuration, nightFadeOutDuration));
    }

    public void TransitionToReal(System.Action onMidFade)
    {
        StartCoroutine(FadeRoutine(dayTransitionColor, onMidFade, dayFadeInDuration, dayHoldDuration, dayFadeOutDuration));
    }
    
    // Versão com tempos customizados
    public void TransitionToDream(System.Action onMidFade, float fadeInTime, float holdTime, float fadeOutTime)
    {
        StartCoroutine(FadeRoutine(nightTransitionColor, onMidFade, fadeInTime, holdTime, fadeOutTime));
    }

    public void TransitionToReal(System.Action onMidFade, float fadeInTime, float holdTime, float fadeOutTime)
    {
        StartCoroutine(FadeRoutine(dayTransitionColor, onMidFade, fadeInTime, holdTime, fadeOutTime));
    }
    
    // Versão com cor e tempos customizados
    public void TransitionToDream(System.Action onMidFade, Color transitionColor, float fadeInTime, float holdTime, float fadeOutTime)
    {
        StartCoroutine(FadeRoutine(transitionColor, onMidFade, fadeInTime, holdTime, fadeOutTime));
    }

    public void TransitionToReal(System.Action onMidFade, Color transitionColor, float fadeInTime, float holdTime, float fadeOutTime)
    {
        StartCoroutine(FadeRoutine(transitionColor, onMidFade, fadeInTime, holdTime, fadeOutTime));
    }

    // =========================
    // CORE
    // =========================

    private IEnumerator FadeRoutine(Color targetColor, System.Action onMidFade, float fadeInTime, float holdTime, float fadeOutTime)
    {
        // Fade IN (escurece)
        yield return Fade(0f, 1f, targetColor, fadeInTime);

        // 🔄 troca de mundo no meio (quando está completamente preto)
        onMidFade?.Invoke();

        // Mantém preto por um tempo
        yield return new WaitForSecondsRealtime(holdTime);

        // Fade OUT (clareia)
        yield return Fade(1f, 0f, targetColor, fadeOutTime);
    }

    private IEnumerator Fade(float from, float to, Color baseColor, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);

            fadeImage.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                alpha
            );

            yield return null;
        }

        fadeImage.color = new Color(
            baseColor.r,
            baseColor.g,
            baseColor.b,
            to
        );
    }
}
