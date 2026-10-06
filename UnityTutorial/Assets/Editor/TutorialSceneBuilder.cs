using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Gera as cenas, prefabs e configurações do tutorial "Introdução Rápida".
// ATENÇÃO: rodar de novo sobrescreve as cenas Menu/Game/EndGame e os prefabs.
public static class TutorialSceneBuilder
{
    const string TemplateScene = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    const string MenuScene = "Assets/Scenes/Menu.unity";
    const string GameScene = "Assets/Scenes/Game.unity";
    const string EndScene = "Assets/Scenes/EndGame.unity";
    const string CollectibleTag = "Coletavel";

    static Font font;

    [MenuItem("Tools/Tutorial/Gerar Cenas do Tutorial")]
    public static void BuildAll()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        ConfigureImporters();
        AddTag(CollectibleTag);

        GameObject playerPrefab = CreatePlayerPrefab();
        GameObject collectiblePrefab = CreateCollectiblePrefab();

        BuildMenuScene();
        BuildGameScene(playerPrefab, collectiblePrefab);
        BuildEndScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(GameScene, true),
            new EditorBuildSettingsScene(EndScene, true),
        };

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null)
            AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScene);
        Debug.Log("[TutorialSceneBuilder] Cenas geradas com sucesso.");
    }

    [MenuItem("Tools/Tutorial/Build WebGL")]
    public static void BuildWebGL()
    {
        // itch.io não envia os headers de Content-Encoding, então sem compressão evita erro no navegador
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[TutorialSceneBuilder] Build WebGL: {report.summary.result} ({report.summary.totalSize} bytes)");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // ---------- Assets ----------

    static void ConfigureImporters()
    {
        foreach (var name in new[] { "Circle", "Square", "Triangle" })
        {
            string path = $"Assets/Sprites/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256; // 256px = 1 unidade
            importer.SaveAndReimport();
        }
    }

    static void AddTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");

    static SpriteRenderer AddSprite(GameObject go, string spriteName, Color color, int order = 0)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.color = color;
        sr.sortingOrder = order;
        var mat = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.default2DMaterial : null;
        if (mat != null) sr.sharedMaterial = mat;
        return sr;
    }

    // ---------- Prefabs ----------

    static GameObject CreatePlayerPrefab()
    {
        var go = new GameObject("Player");
        go.transform.localScale = Vector3.one * 0.8f;
        AddSprite(go, "Circle", new Color(0.9f, 0.2f, 0.2f), 10);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<CircleCollider2D>();

        var audio = go.AddComponent<AudioSource>();
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pickup.wav");
        audio.playOnAwake = false;
        audio.loop = false;

        go.AddComponent<PlayerMovement>().speed = 5f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Player.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateCollectiblePrefab()
    {
        var go = new GameObject("Coletavel");
        go.tag = CollectibleTag;
        go.transform.localScale = Vector3.one * 0.5f;
        AddSprite(go, "Triangle", new Color(1f, 0.85f, 0.2f), 5);

        var col = go.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Coletavel.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- Cenas ----------

    static void NewSceneFromTemplate(string path, Color background)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(TemplateScene, path);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
    }

    static void BuildGameScene(GameObject playerPrefab, GameObject collectiblePrefab)
    {
        NewSceneFromTemplate(GameScene, new Color(0.08f, 0.09f, 0.13f));

        // Paredes (arena de 16x10 unidades, igual ao tamanho da câmera em 960x600)
        var walls = new GameObject("Walls");
        var wallColor = new Color(0.45f, 0.47f, 0.52f);
        CreateWall(walls.transform, "Wall Top", new Vector2(0, 4.75f), new Vector2(16f, 0.5f), wallColor);
        CreateWall(walls.transform, "Wall Bottom", new Vector2(0, -4.75f), new Vector2(16f, 0.5f), wallColor);
        CreateWall(walls.transform, "Wall Left", new Vector2(-7.75f, 0), new Vector2(0.5f, 10f), wallColor);
        CreateWall(walls.transform, "Wall Right", new Vector2(7.75f, 0), new Vector2(0.5f, 10f), wallColor);

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = Vector3.zero;

        var collectibles = new GameObject("Coletaveis");
        Vector2[] positions =
        {
            new(-6f, 3f), new(-3f, 2.5f), new(0f, 3.5f), new(3.5f, 2f),
            new(6f, 3.2f), new(6f, -3.2f), new(2.5f, -2.5f), new(-2f, -3f), new(-6f, -3.2f),
        };
        foreach (var pos in positions)
        {
            var c = (GameObject)PrefabUtility.InstantiatePrefab(collectiblePrefab, collectibles.transform);
            c.transform.position = pos;
        }

        // UI de pontuação
        var canvas = CreateCanvas();
        var scoreText = CreateText(canvas.transform, "Score Text", "Coletáveis: 0/0", 40, TextAnchor.UpperLeft);
        var rt = scoreText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(40, -30);
        rt.sizeDelta = new Vector2(600, 60);
        canvas.AddComponent<ScoreUI>().scoreText = scoreText;

        var hint = CreateText(canvas.transform, "Hint Text", "WASD / Setas para mover  |  ESC para o menu", 26, TextAnchor.LowerCenter);
        hint.color = new Color(1, 1, 1, 0.6f);
        rt = hint.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 15);
        rt.sizeDelta = new Vector2(1200, 40);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = new Vector3(size.x, size.y, 1);
        AddSprite(wall, "Square", color);
        wall.AddComponent<BoxCollider2D>();
    }

    static void BuildMenuScene()
    {
        NewSceneFromTemplate(MenuScene, new Color(0.08f, 0.09f, 0.13f));

        var canvas = CreateCanvas();
        var title = CreateText(canvas.transform, "Title", "Coleta de Triângulos", 110, TextAnchor.MiddleCenter);
        title.color = new Color(1f, 0.85f, 0.2f);
        Place(title.rectTransform, new Vector2(0, 220), new Vector2(1600, 160));

        var subtitle = CreateText(canvas.transform, "Subtitle", "Pegue todos os triângulos!", 40, TextAnchor.MiddleCenter);
        Place(subtitle.rectTransform, new Vector2(0, 110), new Vector2(1200, 60));

        var controller = new GameObject("MenuController").AddComponent<MainMenu>();

        var play = CreateButton(canvas.transform, "Play Button", "Jogar", new Vector2(0, -60));
        UnityEventTools.AddPersistentListener(play.onClick, controller.Play);

        var quit = CreateButton(canvas.transform, "Quit Button", "Sair", new Vector2(0, -190));
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);
        controller.quitButton = quit.gameObject;

        CreateEventSystem(play.gameObject);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static void BuildEndScene()
    {
        NewSceneFromTemplate(EndScene, new Color(0.08f, 0.09f, 0.13f));

        var canvas = CreateCanvas();
        var title = CreateText(canvas.transform, "Title", "Fim de Jogo!", 110, TextAnchor.MiddleCenter);
        title.color = new Color(1f, 0.85f, 0.2f);
        Place(title.rectTransform, new Vector2(0, 220), new Vector2(1600, 160));

        var result = CreateText(canvas.transform, "Result Text", "", 44, TextAnchor.MiddleCenter);
        Place(result.rectTransform, new Vector2(0, 100), new Vector2(1400, 70));

        var controller = new GameObject("EndGameController").AddComponent<EndGameMenu>();
        controller.resultText = result;

        var again = CreateButton(canvas.transform, "Play Again Button", "Jogar novamente", new Vector2(0, -60));
        UnityEventTools.AddPersistentListener(again.onClick, controller.PlayAgain);

        var menu = CreateButton(canvas.transform, "Menu Button", "Menu", new Vector2(0, -190));
        UnityEventTools.AddPersistentListener(menu.onClick, controller.BackToMenu);

        CreateEventSystem(again.gameObject);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    // ---------- UI helpers ----------

    static GameObject CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static Text CreateText(Transform parent, string name, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, new Vector2(520, 100));

        var image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.9f, 0.2f, 0.2f);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.selectedColor = new Color(1f, 0.85f, 0.6f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;

        var text = CreateText(go.transform, "Label", label, 48, TextAnchor.MiddleCenter);
        var rt = text.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return button;
    }

    static void CreateEventSystem(GameObject firstSelected)
    {
        var go = new GameObject("EventSystem");
        var es = go.AddComponent<EventSystem>();
        es.firstSelectedGameObject = firstSelected;
        // Sem actions atribuídas, o módulo usa as DefaultInputActions (teclado, mouse e controle)
        ObjectFactory.AddComponent<InputSystemUIInputModule>(go);
    }
}
