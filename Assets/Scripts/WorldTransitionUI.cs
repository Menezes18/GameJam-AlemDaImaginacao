using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WorldTransitionUI : MonoBehaviour
{
    public static WorldTransitionUI Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.25f;

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
        StartCoroutine(FadeRoutine(Color.white, onMidFade));
    }

    public void TransitionToReal(System.Action onMidFade)
    {
        StartCoroutine(FadeRoutine(Color.black, onMidFade));
    }

    // =========================
    // CORE
    // =========================

    private IEnumerator FadeRoutine(Color targetColor, System.Action onMidFade)
    {
        // Fade OUT
        yield return Fade(0f, 1f, targetColor);

        // 🔄 troca de mundo no meio
        onMidFade?.Invoke();

        // Fade IN
        yield return Fade(1f, 0f, targetColor);
    }

    private IEnumerator Fade(float from, float to, Color baseColor)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, t / fadeDuration);

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
