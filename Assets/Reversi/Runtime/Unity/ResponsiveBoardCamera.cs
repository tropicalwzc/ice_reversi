using UnityEngine;

namespace IceReversi.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class ResponsiveBoardCamera : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float requiredHalfHeight = 6.5f;
        [SerializeField, Min(1f)] private float requiredHalfWidth = 4.45f;

        private Camera targetCamera;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ApplySize();
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size != lastScreenSize)
            {
                ApplySize();
            }
        }

        public void ApplySize()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            var aspect = Mathf.Max(0.1f, targetCamera.aspect);
            targetCamera.orthographicSize = Mathf.Max(requiredHalfHeight, requiredHalfWidth / aspect);
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
