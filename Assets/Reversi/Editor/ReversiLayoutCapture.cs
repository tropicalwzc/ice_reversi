using System;
using System.IO;
using System.Reflection;
using IceReversi.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IceReversi.Editor
{
    public static class ReversiLayoutCapture
    {
        private const string OutputDirectory = "/private/tmp/ice_reversi_layouts";

        public static void CaptureFromCommandLine()
        {
            try
            {
                CaptureReferenceLayouts();
                Debug.Log("ICE_REVERSI_LAYOUT_CAPTURE_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Ice Reversi/Capture Reference Layouts")]
        public static void CaptureReferenceLayouts()
        {
            Directory.CreateDirectory(OutputDirectory);
            EditorSceneManager.OpenScene(ReversiProjectBuilder.ScenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var controller = UnityEngine.Object.FindAnyObjectByType<GameController>();
            if (camera == null || canvas == null || controller == null)
            {
                throw new InvalidOperationException("Game scene camera, Canvas, or GameController is missing.");
            }

            InvokeLifecycle(controller, "Awake");
            InvokeLifecycle(controller, "Start");

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Capture(camera, canvas, "portrait-9x16", 1080, 1920);
            Capture(camera, canvas, "landscape-16x10", 1920, 1200);
            Capture(camera, canvas, "widescreen-16x9", 1920, 1080);
            Capture(camera, canvas, "classic-4x3", 1440, 1080);
        }

        private static void Capture(Camera camera, Canvas canvas, string name, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.aspect = width / (float)height;
                camera.GetComponent<ResponsiveBoardCamera>()?.ApplySize();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                var path = Path.Combine(OutputDirectory, name + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log($"ICE_REVERSI_LAYOUT_CAPTURE {name} {width}x{height} {path}");
            }
            finally
            {
                camera.targetTexture = null;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void InvokeLifecycle(GameController controller, string methodName)
        {
            var method = typeof(GameController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(typeof(GameController).FullName, methodName);
            }

            method.Invoke(controller, null);
        }
    }
}
