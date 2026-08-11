using UnityEngine;

namespace IceReversi.Unity
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != lastSafeArea || size != lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            var safeArea = Screen.safeArea;
            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            rectTransform.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            rectTransform.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
