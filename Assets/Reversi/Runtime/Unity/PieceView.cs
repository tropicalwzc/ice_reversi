using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using IceReversi.Core;
using UnityEngine;

namespace IceReversi.Unity
{
    public sealed class PieceView : MonoBehaviour
    {
        [SerializeField] private Renderer pieceRenderer;
        [SerializeField] private Material blackMaterial;
        [SerializeField] private Material whiteMaterial;
        [SerializeField, Min(0.05f)] private float placementDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float flipDuration = 0.27f;

        private Coroutine animationRoutine;
        private TaskCompletionSource<bool> animationCompletion;
        private PieceColor displayedColor = PieceColor.Empty;
        private Vector3 restingScale;

        public PieceColor DisplayedColor => displayedColor;
        public bool IsAnimating => animationRoutine != null;

        public void Configure(Renderer renderer, Material black, Material white)
        {
            pieceRenderer = renderer;
            blackMaterial = black;
            whiteMaterial = white;
            CaptureRestingScale();
        }

        private void Awake()
        {
            CaptureRestingScale();
        }

        public void SetColor(PieceColor color, bool animate)
        {
            if (!color.IsPlayer()) return;
            if (animationRoutine != null && displayedColor == color) return;

            if (animate && displayedColor.IsPlayer() && displayedColor != color && isActiveAndEnabled)
            {
                _ = PresentFlip(color, 0f, flipDuration, CancellationToken.None, null);
                return;
            }

            CancelAndSnap(color);
        }

        public Task PresentPlacement(
            PieceColor targetColor,
            float duration,
            CancellationToken cancellationToken,
            Action onVisibleStart)
        {
            if (!targetColor.IsPlayer() || !isActiveAndEnabled)
            {
                CancelAndSnap(targetColor);
                onVisibleStart?.Invoke();
                return Task.CompletedTask;
            }

            CancelAnimation(false);
            ApplyMaterial(targetColor);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.zero;
            onVisibleStart?.Invoke();
            return StartPresentation(PlacementRoutine(Mathf.Max(0.01f, duration), cancellationToken));
        }

        public Task PresentFlip(
            PieceColor targetColor,
            float delay,
            float duration,
            CancellationToken cancellationToken,
            Action onMidpoint)
        {
            if (!targetColor.IsPlayer()) return Task.CompletedTask;
            if (displayedColor == targetColor && animationRoutine == null)
            {
                SnapTransform();
                return Task.CompletedTask;
            }

            if (!isActiveAndEnabled)
            {
                CancelAndSnap(targetColor);
                return Task.CompletedTask;
            }

            CancelAnimation(false);
            return StartPresentation(FlipRoutine(
                targetColor,
                Mathf.Max(0f, delay),
                Mathf.Max(0.01f, duration),
                cancellationToken,
                onMidpoint));
        }

        public void CancelAndSnap(PieceColor authoritativeColor)
        {
            CancelAnimation(true);
            if (authoritativeColor.IsPlayer())
            {
                ApplyMaterial(authoritativeColor);
            }

            SnapTransform();
        }

        private Task StartPresentation(IEnumerator routine)
        {
            animationCompletion = new TaskCompletionSource<bool>();
            animationRoutine = StartCoroutine(routine);
            return animationCompletion.Task;
        }

        private IEnumerator PlacementRoutine(float duration, CancellationToken cancellationToken)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    FinishCancelled();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                float scale;
                if (progress < 0.72f)
                {
                    scale = Mathf.SmoothStep(0f, 1.08f, progress / 0.72f);
                }
                else
                {
                    scale = Mathf.Lerp(1.08f, 1f, (progress - 0.72f) / 0.28f);
                }

                transform.localScale = restingScale * scale;
                yield return null;
            }

            SnapTransform();
            FinishSuccessfully();
        }

        private IEnumerator FlipRoutine(
            PieceColor targetColor,
            float delay,
            float duration,
            CancellationToken cancellationToken,
            Action onMidpoint)
        {
            var delayed = 0f;
            while (delayed < delay)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    FinishCancelled();
                    yield break;
                }

                delayed += Time.unscaledDeltaTime;
                yield return null;
            }

            var elapsed = 0f;
            var materialChanged = false;
            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    FinishCancelled();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                transform.localRotation = Quaternion.Euler(0f, 0f, progress * 180f);
                if (!materialChanged && progress >= 0.5f)
                {
                    ApplyMaterial(targetColor);
                    materialChanged = true;
                    onMidpoint?.Invoke();
                }

                yield return null;
            }

            ApplyMaterial(targetColor);
            SnapTransform();
            FinishSuccessfully();
        }

        private void FinishSuccessfully()
        {
            animationRoutine = null;
            var completion = animationCompletion;
            animationCompletion = null;
            completion?.TrySetResult(true);
        }

        private void FinishCancelled()
        {
            animationRoutine = null;
            SnapTransform();
            var completion = animationCompletion;
            animationCompletion = null;
            completion?.TrySetCanceled();
        }

        private void CancelAnimation(bool cancelTask)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            var completion = animationCompletion;
            animationCompletion = null;
            if (cancelTask) completion?.TrySetCanceled();
            else completion?.TrySetResult(false);
        }

        private void SnapTransform()
        {
            CaptureRestingScale();
            transform.localScale = restingScale;
            transform.localRotation = Quaternion.identity;
        }

        private void CaptureRestingScale()
        {
            if (restingScale == Vector3.zero && transform.localScale != Vector3.zero)
            {
                restingScale = transform.localScale;
            }

            if (restingScale == Vector3.zero) restingScale = Vector3.one;
        }

        private void ApplyMaterial(PieceColor color)
        {
            displayedColor = color;
            if (pieceRenderer != null)
            {
                pieceRenderer.sharedMaterial = color == PieceColor.Black ? blackMaterial : whiteMaterial;
            }
        }

        private void OnDestroy()
        {
            CancelAnimation(true);
        }
    }
}
