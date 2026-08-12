using System;
using System.Collections.Generic;
using IceReversi.Core;
using IceReversi.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IceReversi.Editor
{
    public static class ReversiProjectValidator
    {
        [MenuItem("Ice Reversi/Validate Project")]
        public static void ValidateFromMenu()
        {
            ValidateProject();
            Debug.Log("Ice Reversi project validation passed.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            try
            {
                ReversiProjectBuilder.BuildProject();
                ValidateProject();
                Debug.Log("ICE_REVERSI_VALIDATION_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                ValidateProject();
                Debug.Log("ICE_REVERSI_VALIDATION_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateProject()
        {
            var errors = new List<string>();
            if (Application.unityVersion != "6000.5.7f1")
            {
                errors.Add($"Expected Unity 6000.5.7f1, found {Application.unityVersion}.");
            }

            var buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length != 1 || !buildScenes[0].enabled || buildScenes[0].path != ReversiProjectBuilder.ScenePath)
            {
                errors.Add("Game.unity must be the only enabled startup scene.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ReversiProjectBuilder.ScenePath) == null)
            {
                errors.Add("Game scene asset is missing.");
            }

            if (errors.Count == 0)
            {
                var scene = EditorSceneManager.OpenScene(ReversiProjectBuilder.ScenePath, OpenSceneMode.Single);
                ValidateScene(scene, errors);
            }

            var opening = new GameSession().Snapshot();
            if (opening.BlackScore != 2 || opening.WhiteScore != 2 || opening.LegalMoves.Count != 4 ||
                opening.ActiveColor != PieceColor.Black)
            {
                errors.Add("Core session does not produce the standard initial game state.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Ice Reversi validation failed:\n- " + string.Join("\n- ", errors));
            }
        }

        private static void ValidateScene(Scene scene, ICollection<string> errors)
        {
            var controllers = UnityEngine.Object.FindObjectsByType<GameController>(FindObjectsInactive.Include);
            var boards = UnityEngine.Object.FindObjectsByType<BoardView>(FindObjectsInactive.Include);
            var huds = UnityEngine.Object.FindObjectsByType<GameHud>(FindObjectsInactive.Include);
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            if (controllers.Length != 1) errors.Add($"Expected one GameController, found {controllers.Length}.");
            if (boards.Length != 1) errors.Add($"Expected one BoardView, found {boards.Length}.");
            if (huds.Length != 1) errors.Add($"Expected one GameHud, found {huds.Length}.");
            if (canvases.Length != 1) errors.Add($"Expected one Canvas, found {canvases.Length}.");
            if (eventSystems.Length != 1) errors.Add($"Expected one EventSystem, found {eventSystems.Length}.");

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var gameObject = transform.gameObject;
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0)
                    {
                        errors.Add($"Missing script on {GetPath(transform)}.");
                    }

                    foreach (var renderer in gameObject.GetComponents<Renderer>())
                    {
                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null)
                            {
                                errors.Add($"Missing material on {GetPath(transform)}.");
                            }
                        }
                    }
                }
            }

            ValidateObjectReference(controllers.Length == 1 ? controllers[0] : null, "boardView", errors);
            ValidateObjectReference(controllers.Length == 1 ? controllers[0] : null, "hud", errors);
            ValidateObjectReference(controllers.Length == 1 ? controllers[0] : null, "audioController", errors);
            ValidateObjectReference(boards.Length == 1 ? boards[0] : null, "inputCamera", errors);
            ValidateObjectReference(boards.Length == 1 ? boards[0] : null, "inputSurface", errors);
            ValidateObjectReference(boards.Length == 1 ? boards[0] : null, "piecePrefab", errors);
            ValidateObjectReference(boards.Length == 1 ? boards[0] : null, "hintPrefab", errors);
            ValidatePositiveFloat(boards.Length == 1 ? boards[0] : null, "placementDuration", errors);
            ValidatePositiveFloat(boards.Length == 1 ? boards[0] : null, "flipDuration", errors);
            ValidatePositiveFloat(boards.Length == 1 ? boards[0] : null, "maximumPresentationDuration", errors);
            ValidateObjectReference(huds.Length == 1 ? huds[0] : null, "difficultyButton", errors);
            ValidateObjectReference(huds.Length == 1 ? huds[0] : null, "difficultyButtonText", errors);
            ValidateObjectReference(huds.Length == 1 ? huds[0] : null, "languageButton", errors);
            ValidateObjectReference(huds.Length == 1 ? huds[0] : null, "languageButtonText", errors);

            foreach (var dependency in AssetDatabase.GetDependencies(ReversiProjectBuilder.ScenePath, true))
            {
                if (!dependency.StartsWith("Assets/", StringComparison.Ordinal) &&
                    !dependency.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    errors.Add($"Scene dependency is outside the project: {dependency}");
                }
            }
        }

        private static void ValidateObjectReference(UnityEngine.Object target, string propertyName, ICollection<string> errors)
        {
            if (target == null) return;
            var property = new SerializedObject(target).FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                errors.Add($"{target.GetType().Name}.{propertyName} is not assigned.");
            }
        }

        private static void ValidatePositiveFloat(UnityEngine.Object target, string propertyName, ICollection<string> errors)
        {
            if (target == null) return;
            var property = new SerializedObject(target).FindProperty(propertyName);
            if (property == null || property.floatValue <= 0f)
            {
                errors.Add($"{target.GetType().Name}.{propertyName} must be configured above zero.");
            }
        }

        private static string GetPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
