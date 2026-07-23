using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class AetheriaGame
{
    private const float StandardScreenFadeSeconds = 0.25f;
    private const float ResultScreenFadeSeconds = 0.40f;
    private const float StandardScreenSettleSeconds = 0.29f;
    private const float ResultScreenSettleSeconds = 0.46f;

    private Coroutine screenMotionCoroutine;
    private CanvasGroup screenMotionCanvasGroup;
    private RectTransform screenMotionPage;
    private Vector2 screenMotionPageRestPosition;
    private Vector3 screenMotionPageRestScale;
    private int screenMotionVersion;
    private AetheriaScreen lastPresentedScreen;
    private bool hasPresentedScreen;

    private void QueueScreenMotionEntrance()
    {
        screenMotionVersion++;
        if (screenMotionCoroutine != null)
        {
            StopCoroutine(screenMotionCoroutine);
            screenMotionCoroutine = null;
        }

        RestoreScreenMotionState();
        if (root == null)
        {
            return;
        }

        screenMotionCanvasGroup = root.GetComponent<CanvasGroup>();
        if (screenMotionCanvasGroup == null)
        {
            screenMotionCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        }

        // The group also rejects input during a screen-changing fade so a fast
        // double click cannot activate a newly created, still-invisible control.
        screenMotionCanvasGroup.interactable = true;
        screenMotionCanvasGroup.blocksRaycasts = true;
        screenMotionCanvasGroup.ignoreParentGroups = false;

        var screenChanged = !hasPresentedScreen || lastPresentedScreen != currentScreen;
        lastPresentedScreen = currentScreen;
        hasPresentedScreen = true;
        if (!screenChanged || Application.isBatchMode || !isActiveAndEnabled)
        {
            screenMotionCanvasGroup.alpha = 1f;
            return;
        }

        screenMotionCanvasGroup.alpha = 0f;
        screenMotionCanvasGroup.interactable = false;
        screenMotionCanvasGroup.blocksRaycasts = false;
        var version = screenMotionVersion;
        var resultScreen = IsResultScreenMotion();
        screenMotionCoroutine = StartCoroutine(PlayScreenMotionEntrance(version, resultScreen));
    }

    private IEnumerator PlayScreenMotionEntrance(int version, bool resultScreen)
    {
        // ClearRoot builds the new page immediately after returning. Waiting one
        // frame also lets deferred Destroy calls release the previous hierarchy.
        yield return null;
        if (!IsScreenMotionCurrent(version))
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        screenMotionPage = FindCurrentScreenMotionPage();
        if (screenMotionPage != null)
        {
            screenMotionPageRestPosition = screenMotionPage.anchoredPosition;
            screenMotionPageRestScale = screenMotionPage.localScale;
        }

        var fadeDuration = resultScreen ? ResultScreenFadeSeconds : StandardScreenFadeSeconds;
        var settleDuration = resultScreen ? ResultScreenSettleSeconds : StandardScreenSettleSeconds;
        var startOffset = resultScreen ? new Vector2(0f, -22f) : new Vector2(0f, -13f);
        var startScale = resultScreen ? 0.978f : 0.991f;
        var elapsed = 0f;
        var framesWithoutTime = 0;

        while (elapsed < Mathf.Max(fadeDuration, settleDuration))
        {
            if (!IsScreenMotionCurrent(version))
            {
                yield break;
            }

            // Let a slow frame catch the transition up instead of stretching a
            // quarter-second settle into several seconds on a struggling device.
            var deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (deltaTime <= 0.000001f)
            {
                framesWithoutTime++;
                if (framesWithoutTime >= 8)
                {
                    break;
                }
            }
            else
            {
                framesWithoutTime = 0;
                elapsed += deltaTime;
            }

            if (screenMotionCanvasGroup != null)
            {
                var fadeT = EaseOutCubic(Mathf.Clamp01(elapsed / fadeDuration));
                screenMotionCanvasGroup.alpha = fadeT;
            }

            if (screenMotionPage != null)
            {
                var settleT = EaseOutCubic(Mathf.Clamp01(elapsed / settleDuration));
                screenMotionPage.anchoredPosition = Vector2.LerpUnclamped(
                    screenMotionPageRestPosition + startOffset,
                    screenMotionPageRestPosition,
                    settleT);
                screenMotionPage.localScale = screenMotionPageRestScale * Mathf.LerpUnclamped(startScale, 1f, settleT);
            }

            yield return null;
        }

        if (IsScreenMotionCurrent(version))
        {
            RestoreScreenMotionState();
            screenMotionCoroutine = null;
        }
    }

    private bool IsScreenMotionCurrent(int version)
    {
        return version == screenMotionVersion && root != null && isActiveAndEnabled;
    }

    private void OnDisable()
    {
        screenMotionVersion++;
        if (screenMotionCoroutine != null)
        {
            StopCoroutine(screenMotionCoroutine);
            screenMotionCoroutine = null;
        }
        RestoreScreenMotionState();
    }

    private bool IsResultScreenMotion()
    {
        return currentScreen == AetheriaScreen.Victory
            || currentScreen == AetheriaScreen.Defeat
            || currentScreen == AetheriaScreen.GameClear;
    }

    private RectTransform FindCurrentScreenMotionPage()
    {
        if (root == null)
        {
            return null;
        }

        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var candidate = root.GetChild(i) as RectTransform;
            if (candidate != null && IsGeneratedScreenPage(candidate.name, root))
            {
                return candidate;
            }
        }

        return null;
    }

    private void RestoreScreenMotionState()
    {
        if (screenMotionCanvasGroup != null)
        {
            screenMotionCanvasGroup.alpha = 1f;
            screenMotionCanvasGroup.interactable = true;
            screenMotionCanvasGroup.blocksRaycasts = true;
        }

        if (screenMotionPage != null)
        {
            screenMotionPage.anchoredPosition = screenMotionPageRestPosition;
            screenMotionPage.localScale = screenMotionPageRestScale;
        }

        screenMotionPage = null;
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        var inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private void AttachScreenParallax(RectTransform target, float maximumOffset, float direction)
    {
        if (target == null || Application.isBatchMode)
        {
            return;
        }

        var presenter = target.gameObject.AddComponent<ScreenParallaxPresenter>();
        presenter.Configure(maximumOffset, direction);
    }

    private sealed class ScreenParallaxPresenter : MonoBehaviour
    {
        private RectTransform target;
        private Vector2 restPosition;
        private Vector2 appliedOffset;
        private Vector3 restScale;
        private float maximumOffset;
        private float direction;
        private bool initialized;

        public void Configure(float maximumOffsetValue, float directionValue)
        {
            target = transform as RectTransform;
            maximumOffset = Mathf.Clamp(maximumOffsetValue, 0f, 10f);
            direction = Mathf.Clamp(directionValue, -1f, 1f);
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            if (target == null)
            {
                target = transform as RectTransform;
            }
            if (target == null)
            {
                return;
            }

            restPosition = target.anchoredPosition;
            restScale = target.localScale;
            // A small overscan prevents a full-screen backdrop from exposing an
            // edge when it reaches the parallax limit on a matching aspect ratio.
            target.localScale = restScale * 1.025f;
            initialized = true;
        }

        private void LateUpdate()
        {
            InitializeIfNeeded();
            if (!initialized || target == null)
            {
                return;
            }

            // AspectRatioFitter and resolution changes may rewrite the anchored
            // position. Treat any value other than our last output as a new rest.
            var expectedPosition = restPosition + appliedOffset;
            if ((target.anchoredPosition - expectedPosition).sqrMagnitude > 0.01f)
            {
                restPosition = target.anchoredPosition;
            }

            var desiredOffset = Vector2.zero;
            if (Application.isFocused && TryReadNormalizedPointer(out var pointer))
            {
                desiredOffset = pointer * (maximumOffset * direction);
            }

            var deltaTime = Mathf.Min(Mathf.Max(0f, Time.unscaledDeltaTime), 0.1f);
            var blend = deltaTime <= 0f ? 1f : 1f - Mathf.Exp(-8.5f * deltaTime);
            appliedOffset = Vector2.Lerp(appliedOffset, desiredOffset, blend);
            if (desiredOffset.sqrMagnitude <= 0.0001f && appliedOffset.sqrMagnitude <= 0.0025f)
            {
                appliedOffset = Vector2.zero;
            }

            target.anchoredPosition = restPosition + appliedOffset;
        }

        private static bool TryReadNormalizedPointer(out Vector2 normalized)
        {
            normalized = Vector2.zero;
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return false;
            }

            Vector2 pointerPosition;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }
            pointerPosition = mouse.position.ReadValue();
#else
            if (!Input.mousePresent)
            {
                return false;
            }
            pointerPosition = Input.mousePosition;
#endif
            normalized = new Vector2(
                Mathf.Clamp((pointerPosition.x / Screen.width - 0.5f) * 2f, -1f, 1f),
                Mathf.Clamp((pointerPosition.y / Screen.height - 0.5f) * 2f, -1f, 1f));
            return true;
        }

        private void OnDisable()
        {
            RestoreTransform();
        }

        private void OnDestroy()
        {
            RestoreTransform();
        }

        private void RestoreTransform()
        {
            if (!initialized || target == null)
            {
                return;
            }

            target.anchoredPosition = restPosition;
            target.localScale = restScale;
            appliedOffset = Vector2.zero;
            initialized = false;
        }
    }
}
