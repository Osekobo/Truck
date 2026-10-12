using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeliveryFeedback : MonoBehaviour
{
    public CanvasGroup group;
    public TextMeshProUGUI checkText;
    public TextMeshProUGUI rewardText;
    public Image flashOverlay;

    public float displayDuration = 1.5f;
    public float fadeDuration = 0.3f;

    void Awake()
    {
        if (group != null) group.alpha = 0f;
        if (flashOverlay != null)
        {
            var c = flashOverlay.color;
            c.a = 0f;
            flashOverlay.color = c;
        }
    }

    public void Show(int reward)
    {
        StartCoroutine(PlayFeedback(reward));
    }

    IEnumerator PlayFeedback(int reward)
    {
        if (checkText != null) checkText.text = "Delivered!";
        if (rewardText != null) rewardText.text = "+$" + reward;

        if (group != null) group.alpha = 0f;

        yield return FadeIn();
        yield return new WaitForSeconds(displayDuration);
        yield return FadeOut();
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            if (group != null) group.alpha = a;
            if (flashOverlay != null)
            {
                var c = flashOverlay.color;
                c.a = Mathf.Lerp(0f, 0.35f, a);
                flashOverlay.color = c;
            }
            yield return null;
        }
        if (group != null) group.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float t = 0f;
        float startAlpha = group != null ? group.alpha : 1f;
        float startFlash = flashOverlay != null ? flashOverlay.color.a : 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            if (group != null) group.alpha = Mathf.Lerp(startAlpha, 0f, a);
            if (flashOverlay != null)
            {
                var c = flashOverlay.color;
                c.a = Mathf.Lerp(startFlash, 0f, a);
                flashOverlay.color = c;
            }
            yield return null;
        }
        if (group != null) group.alpha = 0f;
        if (flashOverlay != null)
        {
            var c = flashOverlay.color;
            c.a = 0f;
            flashOverlay.color = c;
        }
    }
}
