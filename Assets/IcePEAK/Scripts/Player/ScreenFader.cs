using System.Collections;
using UnityEngine;

namespace IcePEAK.Player
{
    /// <summary>
    /// Fade-to-black overlay driven by a CanvasGroup. For VR, assign a
    /// CanvasGroup on a world-space Canvas parented to the HMD camera so both
    /// eyes are covered during the fade.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float defaultFadeDuration = 0.5f;

        private Coroutine _running;

        public bool IsBlack => canvasGroup != null && canvasGroup.alpha >= 0.999f;

        public void SetBlackInstant()
        {
            StopRunning();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        public void SetClearInstant()
        {
            StopRunning();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        public void FadeFromBlack(float duration = -1f) => Fade(0f, duration);
        public void FadeToBlack(float duration = -1f) => Fade(1f, duration);

        private void Fade(float target, float duration)
        {
            if (canvasGroup == null) return;
            StopRunning();
            float d = duration < 0f ? defaultFadeDuration : duration;
            _running = StartCoroutine(FadeRoutine(canvasGroup.alpha, target, d));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (duration <= 0f) { canvasGroup.alpha = to; _running = null; yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
            _running = null;
        }

        private void StopRunning()
        {
            if (_running != null) { StopCoroutine(_running); _running = null; }
        }
    }
}
