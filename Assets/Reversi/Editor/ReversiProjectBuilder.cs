using System;
using System.IO;
using IceReversi.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IceReversi.Editor
{
    public static class ReversiProjectBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string PiecePrefabPath = "Assets/Reversi/Prefabs/Piece.prefab";
        public const string HintPrefabPath = "Assets/Reversi/Prefabs/MoveHint.prefab";

        private const string GeneratedRoot = "Assets/Reversi/Generated";
        private const string MaterialsRoot = GeneratedRoot + "/Materials";

        [MenuItem("Ice Reversi/Rebuild Game Scene")]
        public static void BuildFromMenu()
        {
            BuildProject();
            Debug.Log("Ice Reversi Game scene rebuilt successfully.");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildProject();
                Debug.Log("ICE_REVERSI_BUILD_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildProject()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Reversi/Prefabs");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(MaterialsRoot);

            var materials = CreateMaterials();
            var piecePrefab = CreatePiecePrefab(materials);
            var hintPrefab = CreateHintPrefab(materials.Hint);
            BuildScene(materials, piecePrefab, hintPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static MaterialSet CreateMaterials()
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (litShader == null || unlitShader == null)
            {
                throw new InvalidOperationException("Required URP-compatible shaders are unavailable.");
            }

            var blackWood = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Reversi/Art/Textures/blackwood.jpg");
            var boardWood = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Reversi/Art/Textures/zhuban.jpg");
            return new MaterialSet
            {
                BoardBase = CreateOrUpdateMaterial("BoardBase", litShader, new Color(0.16f, 0.09f, 0.055f), blackWood, 0.22f),
                BoardCell = CreateOrUpdateMaterial("BoardCell", litShader, new Color(0.08f, 0.34f, 0.22f), boardWood, 0.18f),
                BoardCellAlternate = CreateOrUpdateMaterial("BoardCellAlternate", litShader, new Color(0.075f, 0.30f, 0.20f), boardWood, 0.18f),
                Grid = CreateOrUpdateMaterial("Grid", unlitShader, new Color(0.025f, 0.065f, 0.05f), null, 0f),
                Black = CreateOrUpdateMaterial("BlackPiece", litShader, new Color(0.018f, 0.022f, 0.03f), null, 0.82f, 0.65f),
                White = CreateOrUpdateMaterial("WhitePiece", litShader, new Color(0.92f, 0.95f, 0.98f), null, 0.82f, 0.08f),
                AlternateBlack = CreateOrUpdateMaterial("AlternateBlackPiece", litShader, new Color(0.035f, 0.10f, 0.16f), null, 0.72f, 0.55f),
                AlternateWhite = CreateOrUpdateMaterial("AlternateWhitePiece", litShader, new Color(0.78f, 0.93f, 1f), null, 0.72f, 0.12f),
                Hint = CreateOrUpdateMaterial("MoveHint", unlitShader, new Color(0.35f, 0.95f, 0.72f, 0.86f), null, 0f)
            };
        }

        private static Material CreateOrUpdateMaterial(
            string name,
            Shader shader,
            Color color,
            Texture texture,
            float smoothness,
            float metallic = 0f)
        {
            var path = $"{MaterialsRoot}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PieceView CreatePiecePrefab(MaterialSet materials)
        {
            var root = new GameObject("Piece");
            try
            {
                var pieceView = root.AddComponent<PieceView>();
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.82f, 0.10f, 0.82f);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                var renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = materials.Black;
                pieceView.Configure(renderer, materials.Black, materials.White);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PiecePrefabPath);
                return saved.GetComponent<PieceView>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateHintPrefab(Material material)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            try
            {
                root.name = "MoveHint";
                root.transform.localScale = new Vector3(0.16f, 0.025f, 0.16f);
                Object.DestroyImmediate(root.GetComponent<Collider>());
                root.GetComponent<MeshRenderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root, HintPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildScene(MaterialSet materials, PieceView piecePrefab, GameObject hintPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gameRoot = new GameObject("GameRoot");
            var environment = CreateChild("Environment", gameRoot.transform);
            var camera = CreateCamera(environment.transform);
            CreateLighting(environment.transform);
            var board = CreateBoard(environment.transform, camera, materials, piecePrefab, hintPrefab);

            var systems = CreateChild("Systems", gameRoot.transform);
            var audio = CreateAudioController(systems.transform);
            var controller = CreateChild("GameController", systems.transform).AddComponent<GameController>();
            var hud = CreateUi(gameRoot.transform, controller);
            controller.Configure(board, hud, audio);
            CreateEventSystem(gameRoot.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = gameRoot;
        }

        private static Camera CreateCamera(Transform parent)
        {
            var cameraObject = CreateChild("MainCamera", parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(0f, 10f, 0f);
            cameraObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.25f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.07f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ResponsiveBoardCamera>();
            return camera;
        }

        private static void CreateLighting(Transform parent)
        {
            var lightObject = CreateChild("DirectionalLight", parent);
            lightObject.transform.localRotation = Quaternion.Euler(50f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.shadows = LightShadows.Soft;
        }

        private static BoardView CreateBoard(
            Transform parent,
            Camera camera,
            MaterialSet materials,
            PieceView piecePrefab,
            GameObject hintPrefab)
        {
            var boardObject = CreateChild("Board", parent);
            var boardView = boardObject.AddComponent<BoardView>();
            var surfaceAndGrid = CreateChild("SurfaceAndGrid", boardObject.transform);

            var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObject.name = "BoardBase";
            baseObject.transform.SetParent(surfaceAndGrid.transform, false);
            baseObject.transform.localPosition = new Vector3(0f, -0.17f, 0f);
            baseObject.transform.localScale = new Vector3(8.55f, 0.28f, 8.55f);
            baseObject.GetComponent<MeshRenderer>().sharedMaterial = materials.BoardBase;
            var boardCollider = baseObject.GetComponent<BoxCollider>();

            var cells = CreateChild("Cells", boardObject.transform);
            for (var row = 0; row < 8; row++)
            {
                for (var column = 0; column < 8; column++)
                {
                    var cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.name = $"Cell_{row}_{column}";
                    cell.transform.SetParent(cells.transform, false);
                    cell.transform.localPosition = new Vector3(column - 3.5f, 0f, 3.5f - row);
                    cell.transform.localScale = new Vector3(0.94f, 0.08f, 0.94f);
                    cell.GetComponent<MeshRenderer>().sharedMaterial =
                        ((row + column) & 1) == 0 ? materials.BoardCell : materials.BoardCellAlternate;
                    Object.DestroyImmediate(cell.GetComponent<Collider>());
                }
            }

            var grid = CreateChild("Grid", surfaceAndGrid.transform);
            for (var line = 0; line <= 8; line++)
            {
                CreateGridLine(grid.transform, materials.Grid, true, line - 4f);
                CreateGridLine(grid.transform, materials.Grid, false, line - 4f);
            }

            var pieces = CreateChild("Pieces", boardObject.transform);
            var hints = CreateChild("MoveHints", boardObject.transform);
            boardView.Configure(camera, boardCollider, pieces.transform, hints.transform, piecePrefab, hintPrefab);
            boardView.ConfigurePresentation(0.18f, 0.27f, 0.035f, 0.5f);
            return boardView;
        }

        private static void CreateGridLine(Transform parent, Material material, bool vertical, float offset)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = vertical ? $"Vertical_{offset:0.0}" : $"Horizontal_{offset:0.0}";
            line.transform.SetParent(parent, false);
            line.transform.localPosition = vertical ? new Vector3(offset, 0.055f, 0f) : new Vector3(0f, 0.055f, offset);
            line.transform.localScale = vertical ? new Vector3(0.035f, 0.018f, 8.04f) : new Vector3(8.04f, 0.018f, 0.035f);
            line.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }

        private static AudioController CreateAudioController(Transform parent)
        {
            var audioObject = CreateChild("AudioController", parent);
            var source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var controller = audioObject.AddComponent<AudioController>();
            controller.Configure(
                source,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Reversi/Audio/place.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Reversi/Audio/flip.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Reversi/Audio/action.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Reversi/Audio/game-over.wav"));
            return controller;
        }

        private static GameHud CreateUi(Transform parent, GameController controller)
        {
            var canvasObject = CreateChild("UI", parent);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var safeArea = CreateRect("SafeArea", canvasObject.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var hud = safeArea.gameObject.AddComponent<GameHud>();
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Reversi/Art/Fonts/segoe-script.ttf") ??
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var topPanel = CreatePanel("ScoresAndTurn", safeArea, new Color(0.015f, 0.025f, 0.045f, 0.84f));
            SetRect(topPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(18f, -158f), new Vector2(-18f, -18f));
            var blackScore = CreateText("BlackScore", topPanel, font, 38, TextAnchor.MiddleLeft, Color.white);
            SetRect(blackScore.rectTransform, new Vector2(0f, 0f), new Vector2(0.28f, 1f), new Vector2(0f, 0.5f),
                new Vector2(28f, 0f), Vector2.zero);
            var status = CreateText("Status", topPanel, font, 34, TextAnchor.MiddleCenter, new Color(0.68f, 1f, 0.84f));
            SetRect(status.rectTransform, new Vector2(0.26f, 0f), new Vector2(0.74f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var whiteScore = CreateText("WhiteScore", topPanel, font, 38, TextAnchor.MiddleRight, Color.white);
            SetRect(whiteScore.rectTransform, new Vector2(0.72f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(-28f, 0f));

            var actions = CreatePanel("Actions", safeArea, new Color(0.015f, 0.025f, 0.045f, 0.88f));
            SetRect(actions, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(18f, 18f), new Vector2(-18f, 178f));
            var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var restart = CreateButton("Restart", actions, font, "Restart");
            var undo = CreateButton("Undo", actions, font, "Undo");
            var side = CreateButton("Side", actions, font, "Play White");
            var spectate = CreateButton("Spectate", actions, font, "Watch AI");
            var difficulty = CreateButton("Difficulty", actions, font, "AI: Normal");
            var language = CreateButton("Language", actions, font, "中文");
            var exit = CreateButton("Exit", actions, font, "Exit");

            var resultPanel = CreatePanel("ResultPanel", safeArea, new Color(0.015f, 0.025f, 0.045f, 0.95f));
            SetRect(resultPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300f, -150f), new Vector2(300f, 150f));
            var result = CreateText("Result", resultPanel, font, 60, TextAnchor.MiddleCenter, Color.white);
            Stretch(result.rectTransform);
            resultPanel.gameObject.SetActive(false);

            hud.Configure(
                blackScore, whiteScore, status, result,
                side.GetComponentInChildren<Text>(), spectate.GetComponentInChildren<Text>(),
                difficulty.GetComponentInChildren<Text>(), language.GetComponentInChildren<Text>(),
                restart, undo, side, spectate, difficulty, language, exit, resultPanel.gameObject);
            hud.Bind(controller);
            return hud;
        }

        private static void CreateEventSystem(Transform parent)
        {
            var eventSystemObject = CreateChild("EventSystem", parent);
            eventSystemObject.AddComponent<EventSystem>();
            var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label)
        {
            var rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.34f, 0.28f, 0.96f);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 1f, 0.91f);
            colors.pressedColor = new Color(0.56f, 0.82f, 0.68f);
            colors.disabledColor = new Color(0.35f, 0.4f, 0.4f, 0.55f);
            button.colors = colors;
            var text = CreateText("Label", rect, font, 30, TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 30;
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = name;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private sealed class MaterialSet
        {
            public Material BoardBase;
            public Material BoardCell;
            public Material BoardCellAlternate;
            public Material Grid;
            public Material Black;
            public Material White;
            public Material AlternateBlack;
            public Material AlternateWhite;
            public Material Hint;
        }
    }
}
