using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gera as cenas, prefabs e configurações do jogo "Tanque Cheio".
// ATENÇÃO: rodar de novo sobrescreve as cenas Menu/Game/EndGame e os prefabs.
public static class TutorialSceneBuilder
{
    const string TemplateScene = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    const string MenuScene = "Assets/Scenes/Menu.unity";
    const string GameScene = "Assets/Scenes/Game.unity";
    const string EndScene = "Assets/Scenes/EndGame.unity";

    static readonly Color Background = new Color(0.09f, 0.09f, 0.11f);
    static readonly Color Yellow = new Color(1f, 0.8f, 0.15f);
    static readonly Color ButtonRed = new Color(0.85f, 0.17f, 0.17f);

    static Font font;

    [MenuItem("Tools/Tutorial/Gerar Cenas do Jogo")]
    public static void BuildAll()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        ConfigureImporters();
        foreach (var tag in new[] { "Coletavel", "Policia", "Oleo" }) AddTag(tag);

        PlayerSettings.productName = "Tanque Cheio";
        PlayerSettings.runInBackground = true;

        var player = CreatePlayerPrefab();
        var police = CreatePolicePrefab();
        var gas = CreateGasPrefab();
        var oil = CreateOilPrefab();

        BuildMenuScene();
        BuildGameScene(player, police, gas, oil);
        BuildEndScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(GameScene, true),
            new EditorBuildSettingsScene(EndScene, true),
        };

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
        var ppu = new Dictionary<string, float>
        {
            { "City", 64 }, { "CarRed", 128 }, { "CarPolice", 128 }, { "GasCan", 100 }, { "Oil", 100 },
            { "Glow", 128 }, { "Heart", 64 }, { "FuelIcon", 64 }, { "White", 8 },
        };
        foreach (var pair in ppu)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"Assets/Sprites/{pair.Key}.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pair.Value;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = pair.Key == "City" ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        foreach (var music in new[] { "music_game", "music_menu" })
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath($"Assets/Audio/{music}.wav");
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
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
    static AudioClip LoadClip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{name}.wav");

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

    static GameObject Child(GameObject parent, string name, Vector2 localPos = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        return go;
    }

    static AudioSource AddAudio(GameObject go, AudioClip clip, bool loop, bool playOnAwake, float volume = 1f)
    {
        var audio = go.AddComponent<AudioSource>();
        audio.clip = clip;
        audio.loop = loop;
        audio.playOnAwake = playOnAwake;
        audio.volume = volume;
        audio.spatialBlend = 0f;
        return audio;
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- Prefabs ----------

    static GameObject CreatePlayerPrefab()
    {
        var go = new GameObject("Player");
        go.tag = "Player";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<CircleCollider2D>().radius = 0.32f;
        AddAudio(go, LoadClip("pickup"), false, false);

        var body = Child(go, "Body");
        var bodyRenderer = AddSprite(body, "CarRed", Color.white, 10);

        var engine = Child(go, "Engine");
        var engineAudio = AddAudio(engine, LoadClip("engine"), true, true, 0.15f);

        var movement = go.AddComponent<PlayerMovement>();
        movement.speed = 5f;
        movement.body = body.transform;
        movement.bodyRenderer = bodyRenderer;
        movement.engineSource = engineAudio;
        movement.crashClip = LoadClip("crash");

        return SavePrefab(go, "Assets/Prefabs/Player.prefab");
    }

    static GameObject CreatePolicePrefab()
    {
        var go = new GameObject("Policia");
        go.tag = "Policia";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.36f;
        col.isTrigger = true;

        var body = Child(go, "Body");
        AddSprite(body, "CarPolice", Color.white, 8);

        var glowRed = Child(body, "Giroflex Vermelho", new Vector2(-0.14f, -0.04f));
        glowRed.transform.localScale = Vector3.one * 1.8f;
        var red = AddSprite(glowRed, "Glow", new Color(1f, 0.15f, 0.15f, 0.5f), 9);

        var glowBlue = Child(body, "Giroflex Azul", new Vector2(0.14f, -0.04f));
        glowBlue.transform.localScale = Vector3.one * 1.8f;
        var blue = AddSprite(glowBlue, "Glow", new Color(0.2f, 0.4f, 1f, 0.5f), 9);

        var sirenGo = Child(go, "Sirene");
        var siren = AddAudio(sirenGo, LoadClip("siren"), true, true, 0f);

        var police = go.AddComponent<PoliceCar>();
        police.body = body.transform;
        police.glowRed = red;
        police.glowBlue = blue;
        police.siren = siren;

        return SavePrefab(go, "Assets/Prefabs/Policia.prefab");
    }

    static GameObject CreateGasPrefab()
    {
        var go = new GameObject("Galao");
        go.tag = "Coletavel";
        AddSprite(go, "GasCan", Color.white, 4);

        var glow = Child(go, "Brilho");
        glow.transform.localScale = Vector3.one * 1.6f;
        AddSprite(glow, "Glow", new Color(1f, 0.85f, 0.2f, 0.45f), 3);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.32f;
        col.isTrigger = true;
        go.AddComponent<GasCan>();

        return SavePrefab(go, "Assets/Prefabs/Galao.prefab");
    }

    static GameObject CreateOilPrefab()
    {
        var go = new GameObject("Oleo");
        go.tag = "Oleo";
        AddSprite(go, "Oil", Color.white, -5);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.6f);
        col.isTrigger = true;
        return SavePrefab(go, "Assets/Prefabs/Oleo.prefab");
    }

    // ---------- Cenas ----------

    static Camera NewSceneFromTemplate(string path, float cameraSize)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(TemplateScene, path);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        cam.orthographicSize = cameraSize;
        return cam;
    }

    static void SaveActiveScene() => EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

    static GameObject CityBackground(Vector2 position, Color tint)
    {
        var city = new GameObject("Cidade");
        city.transform.position = position;
        AddSprite(city, "City", tint, -10);
        return city;
    }

    static void BuildGameScene(GameObject playerPrefab, GameObject policePrefab, GameObject gasPrefab, GameObject oilPrefab)
    {
        var cam = NewSceneFromTemplate(GameScene, 7.2f);
        cam.gameObject.AddComponent<CameraShake>();

        CityBackground(CityMap.Center, Color.white);

        // Colisores dos quarteirões, lidos de CityMap
        var buildings = new GameObject("Quarteiroes");
        foreach (var rect in CityBlocks())
        {
            var block = Child(buildings, $"Quarteirao {rect.x},{rect.y}");
            block.transform.position = CityMap.Origin + rect.center;
            block.AddComponent<BoxCollider2D>().size = rect.size;
        }

        // Bordas do mapa
        var borders = new GameObject("Bordas");
        Vector2 o = CityMap.Origin;
        float w = CityMap.Width, h = CityMap.Height;
        AddWall(borders, "Topo", o + new Vector2(w / 2, h + 0.5f), new Vector2(w + 2, 1));
        AddWall(borders, "Base", o + new Vector2(w / 2, -0.5f), new Vector2(w + 2, 1));
        AddWall(borders, "Esquerda", o + new Vector2(-0.5f, h / 2), new Vector2(1, h + 2));
        AddWall(borders, "Direita", o + new Vector2(w + 0.5f, h / 2), new Vector2(1, h + 2));

        // Cruzamentos (centro das ruas de 2 faixas)
        float[] xs = { -10f, -5f, 0f, 5f, 10f };
        float[] ys = { 4.4f, -0.6f, -5.6f };

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector2(xs[0], ys[2]);

        var policeParent = new GameObject("Policia");
        AddPatrol(policePrefab, policeParent, "Patrulha 1", new[]
        {
            new Vector2(xs[3], ys[0]), new Vector2(xs[4], ys[0]), new Vector2(xs[4], ys[1]), new Vector2(xs[3], ys[1]),
        });
        AddPatrol(policePrefab, policeParent, "Patrulha 2", new[]
        {
            new Vector2(xs[2], ys[0]), new Vector2(xs[2], ys[2]), new Vector2(xs[1], ys[2]), new Vector2(xs[1], ys[0]),
        });

        var spawnParent = new GameObject("Spawns Viaturas");
        var spawns = new[]
        {
            new Vector2(xs[4], ys[0]), new Vector2(xs[4], ys[2]), new Vector2(xs[0], ys[0]), new Vector2(xs[2], ys[2]),
        }.Select((p, i) =>
        {
            var s = Child(spawnParent, $"Spawn {i + 1}");
            s.transform.position = p;
            return s.transform;
        }).ToArray();

        var oilParent = new GameObject("Manchas de Oleo");
        foreach (var p in new[] { new Vector2(2.5f, ys[0]), new Vector2(-7.5f, ys[1]), new Vector2(7.5f, ys[2]), new Vector2(xs[3], 1.9f), new Vector2(xs[1], -3.1f) })
        {
            var oil = (GameObject)PrefabUtility.InstantiatePrefab(oilPrefab, oilParent.transform);
            oil.transform.position = p;
        }

        // ---- UI ----
        var canvas = CreateCanvas();
        var hud = canvas.AddComponent<HUD>();

        var topBar = CreateImage(canvas.transform, "Barra Superior", "White", new Color(0, 0, 0, 0.6f));
        var rt = topBar.rectTransform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 120);

        hud.hearts = new Image[GameController.MaxLives];
        for (int i = 0; i < hud.hearts.Length; i++)
        {
            var heart = CreateImage(topBar.transform, $"Vida {i + 1}", "Heart", Color.red);
            Anchor(heart.rectTransform, new Vector2(0, 0.5f), new Vector2(60 + i * 80, 0), new Vector2(64, 64));
            hud.hearts[i] = heart;
        }

        var fuelIcon = CreateImage(topBar.transform, "Icone Gasolina", "FuelIcon", Yellow);
        Anchor(fuelIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-245, 0), new Vector2(60, 60));
        var fuelBack = CreateImage(topBar.transform, "Gasolina Fundo", "White", new Color(0.2f, 0.2f, 0.22f));
        Anchor(fuelBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(400, 40));
        var fuelFill = CreateImage(fuelBack.transform, "Gasolina", "White", Color.green);
        Stretch(fuelFill.rectTransform, 4);
        fuelFill.type = Image.Type.Filled;
        fuelFill.fillMethod = Image.FillMethod.Horizontal;
        fuelFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.fuelFill = fuelFill;

        hud.gasText = CreateText(topBar.transform, "Galoes", "Galões: 0/15", 42, TextAnchor.MiddleRight);
        Anchor(hud.gasText.rectTransform, new Vector2(1, 0.5f), new Vector2(-690, 0), new Vector2(330, 60), new Vector2(0, 0.5f));
        hud.gasText.color = Yellow;
        hud.timeText = CreateText(topBar.transform, "Tempo", "Tempo: 00:00.0", 42, TextAnchor.MiddleLeft);
        Anchor(hud.timeText.rectTransform, new Vector2(1, 0.5f), new Vector2(-350, 0), new Vector2(330, 60), new Vector2(0, 0.5f));

        hud.messageText = CreateText(canvas.transform, "Mensagem", "", 60, TextAnchor.MiddleCenter, true);
        hud.messageText.color = Yellow;
        Anchor(hud.messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1800, 100));

        var hint = CreateText(canvas.transform, "Dica", "WASD / Setas / Analógico: dirigir   |   ESC / P / Start: pausar", 26, TextAnchor.MiddleCenter);
        hint.color = new Color(1, 1, 1, 0.65f);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1600, 40));

        // Painel de pausa
        var pause = CreateImage(canvas.transform, "Pausa", "White", new Color(0, 0, 0, 0.75f));
        Stretch(pause.rectTransform, 0);
        var pauseTitle = CreateText(pause.transform, "Titulo", "PAUSADO", 110, TextAnchor.MiddleCenter, true);
        pauseTitle.color = Yellow;
        Anchor(pauseTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1200, 150));

        var level = new GameObject("LevelManager").AddComponent<LevelManager>();
        var resume = CreateButton(pause.transform, "Continuar", "Continuar", new Vector2(0, 0), level.TogglePause);
        CreateButton(pause.transform, "Menu", "Menu principal", new Vector2(0, -130), level.BackToMenu);
        pause.gameObject.SetActive(false);

        level.player = player.GetComponent<PlayerMovement>();
        level.gasPrefab = gasPrefab;
        level.policePrefab = policePrefab;
        level.chaserSpawnPoints = spawns;
        level.pausePanel = pause.gameObject;
        level.pauseFirstButton = resume.gameObject;
        level.music = AddAudio(level.gameObject, LoadClip("music_game"), true, true, 0.35f);
        level.sfx = AddAudio(level.gameObject, null, false, false, 0.8f);
        level.lowFuelClip = LoadClip("lowfuel");
        level.victoryClip = LoadClip("victory");
        level.gameOverClip = LoadClip("gameover");

        CreateEventSystem(null);
        SaveActiveScene();
    }

    // Agrupa as células que não são rua em retângulos (em unidades, a partir de CityMap.Origin)
    static List<Rect> CityBlocks()
    {
        var seen = new HashSet<Vector2Int>();
        var result = new List<Rect>();
        for (int y = 0; y < CityMap.Height; y++)
            for (int x = 0; x < CityMap.Width; x++)
            {
                var c = new Vector2Int(x, y);
                if (CityMap.IsRoad(c) || seen.Contains(c)) continue;
                int w = 1;
                while (x + w < CityMap.Width && !CityMap.IsRoad(new Vector2Int(x + w, y)) && !seen.Contains(new Vector2Int(x + w, y))) w++;
                int h = 1;
                while (y + h < CityMap.Height && Enumerable.Range(x, w).All(xx => !CityMap.IsRoad(new Vector2Int(xx, y + h)))) h++;
                for (int yy = y; yy < y + h; yy++)
                    for (int xx = x; xx < x + w; xx++)
                        seen.Add(new Vector2Int(xx, yy));
                result.Add(new Rect(x, y, w, h));
            }
        return result;
    }

    static void AddWall(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var wall = Child(parent, name);
        wall.transform.position = pos;
        wall.AddComponent<BoxCollider2D>().size = size;
    }

    static void AddPatrol(GameObject prefab, GameObject parent, string name, Vector2[] points)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.name = name;
        go.transform.position = points[0];
        var police = go.GetComponent<PoliceCar>();
        police.mode = PoliceMode.Patrol;
        police.patrolPoints = points;
    }

    static void BuildMenuScene()
    {
        NewSceneFromTemplate(MenuScene, 6.5f);
        CityBackground(Vector2.zero, new Color(0.42f, 0.42f, 0.47f));

        // Carro fugindo da polícia no fundo
        var cars = new GameObject("Carros Decorativos");
        AddMenuCar(cars, "CarRed", new Vector2(-2f, -5.3f));
        AddMenuCar(cars, "CarPolice", new Vector2(-5f, -5.3f));
        AddMenuCar(cars, "CarPolice", new Vector2(-8f, -4.7f));

        var canvas = CreateCanvas();
        var controller = new GameObject("MenuController").AddComponent<MainMenu>();

        // Painel principal
        var main = new GameObject("Painel Principal", typeof(RectTransform));
        main.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)main.transform, 0);

        var title = CreateText(main.transform, "Titulo", "TANQUE CHEIO", 150, TextAnchor.MiddleCenter, true);
        title.color = Yellow;
        title.fontStyle = FontStyle.Bold;
        Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 340), new Vector2(1800, 200));

        var subtitle = CreateText(main.transform, "Subtitulo", "Colete gasolina. Fuja da polícia.", 48, TextAnchor.MiddleCenter, true);
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1400, 70));
        var play = CreateButton(main.transform, "Jogar", "Jogar", new Vector2(0, 50), controller.Play);
        CreateButton(main.transform, "Como Jogar", "Como jogar", new Vector2(0, -80), controller.ShowHowTo);
        var quit = CreateButton(main.transform, "Sair", "Sair", new Vector2(0, -210), controller.Quit);
        var best = CreateText(main.transform, "Recorde", "", 36, TextAnchor.MiddleCenter, true);
        Anchor(best.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -320), new Vector2(800, 50));

        // Painel "Como jogar"
        var howTo = CreateImage(canvas.transform, "Painel Como Jogar", "White", new Color(0.05f, 0.05f, 0.07f, 0.92f));
        Anchor(howTo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1500, 860));
        var howToText = CreateText(howTo.transform, "Texto",
            "<b><color=#FFCC26>OBJETIVO</color></b>\n" +
            $"Colete {GameController.GasGoal} galões de gasolina antes que o tanque esvazie.\n" +
            "Cada galão enche 35% do tanque. Andando, a gasolina acaba mais rápido!\n\n" +
            "<b><color=#FFCC26>POLÍCIA</color></b>\n" +
            "Viaturas patrulham a cidade e perseguem você se te virem.\n" +
            "Conforme você coleta galões, mais viaturas entram na perseguição.\n" +
            "Você tem 3 vidas. Cuidado com as manchas de óleo: o carro escorrega!\n\n" +
            "<b><color=#FFCC26>CONTROLES</color></b>\n" +
            "Teclado: WASD ou setas  |  ESC ou P: pausar\n" +
            "Controle: analógico ou direcional  |  Start: pausar  |  A: confirmar",
            36, TextAnchor.UpperCenter);
        howToText.supportRichText = true;
        howToText.lineSpacing = 1.1f;
        Anchor(howToText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1400, 600), new Vector2(0.5f, 1f));
        var back = CreateButton(howTo.transform, "Voltar", "Voltar", new Vector2(0, -340), controller.ShowMain);

        controller.mainPanel = main;
        controller.howToPanel = howTo.gameObject;
        controller.playButton = play.gameObject;
        controller.howToBackButton = back.gameObject;
        controller.quitButton = quit.gameObject;
        controller.bestTimeText = best;

        AddAudio(new GameObject("Musica"), LoadClip("music_menu"), true, true, 0.45f);
        CreateEventSystem(play.gameObject);
        SaveActiveScene();
    }

    static void AddMenuCar(GameObject parent, string sprite, Vector2 pos)
    {
        var car = Child(parent, sprite);
        car.transform.position = pos;
        car.transform.rotation = Quaternion.Euler(0, 0, -90);
        AddSprite(car, sprite, Color.white, 5);
        if (sprite == "CarPolice")
        {
            var red = Child(car, "Giroflex Vermelho", new Vector2(-0.14f, -0.04f));
            red.transform.localScale = Vector3.one * 1.8f;
            AddSprite(red, "Glow", new Color(1f, 0.15f, 0.15f, 0.6f), 6);
            var blue = Child(car, "Giroflex Azul", new Vector2(0.14f, -0.04f));
            blue.transform.localScale = Vector3.one * 1.8f;
            AddSprite(blue, "Glow", new Color(0.2f, 0.4f, 1f, 0.6f), 6);
        }
        var loop = car.AddComponent<MenuCarLoop>();
        loop.speed = 4.5f;
        loop.minX = -13f;
        loop.maxX = 13f;
    }

    static void BuildEndScene()
    {
        NewSceneFromTemplate(EndScene, 6.5f);
        CityBackground(Vector2.zero, new Color(0.35f, 0.35f, 0.4f));

        var canvas = CreateCanvas();
        var controller = new GameObject("EndGameController").AddComponent<EndGameMenu>();

        var title = CreateText(canvas.transform, "Titulo", "Fim de Jogo", 130, TextAnchor.MiddleCenter, true);
        title.fontStyle = FontStyle.Bold;
        Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 340), new Vector2(1800, 180));

        var reason = CreateText(canvas.transform, "Motivo", "", 46, TextAnchor.MiddleCenter, true);
        Anchor(reason.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1600, 70));

        var stats = CreateText(canvas.transform, "Estatisticas", "", 42, TextAnchor.MiddleCenter, true);
        stats.lineSpacing = 1.2f;
        Anchor(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1400, 200));

        var again = CreateButton(canvas.transform, "Jogar Novamente", "Jogar novamente", new Vector2(0, -130), controller.PlayAgain);
        CreateButton(canvas.transform, "Menu", "Menu principal", new Vector2(0, -260), controller.BackToMenu);

        controller.titleText = title;
        controller.reasonText = reason;
        controller.statsText = stats;
        controller.firstButton = again.gameObject;

        AddAudio(new GameObject("Musica"), LoadClip("music_menu"), true, true, 0.45f);
        CreateEventSystem(again.gameObject);
        SaveActiveScene();
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

    static GameObject UIObject(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    static Text CreateText(Transform parent, string name, string content, int size, TextAnchor anchor, bool outline = false)
    {
        var go = UIObject(parent, name);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        if (outline)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.85f);
            o.effectDistance = new Vector2(3, -3);
        }
        return text;
    }

    static Image CreateImage(Transform parent, string name, string sprite, Color color)
    {
        var go = UIObject(parent, name);
        var image = go.AddComponent<Image>();
        image.sprite = LoadSprite(sprite);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos, UnityAction onClick)
    {
        var go = UIObject(parent, name);
        Anchor((RectTransform)go.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(560, 100));

        var image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = ButtonRed;

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.8f, 0.5f);
        colors.selectedColor = new Color(1f, 0.8f, 0.5f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        go.AddComponent<ButtonSound>();

        var text = CreateText(go.transform, "Label", label, 48, TextAnchor.MiddleCenter);
        text.fontStyle = FontStyle.Bold;
        Stretch(text.rectTransform, 0);
        return button;
    }

    static void CreateEventSystem(GameObject firstSelected)
    {
        var go = new GameObject("EventSystem");
        var es = go.AddComponent<EventSystem>();
        es.firstSelectedGameObject = firstSelected;
        // Sem actions atribuídas, o módulo usa as DefaultInputActions (teclado, mouse e controle)
        ObjectFactory.AddComponent<InputSystemUIInputModule>(go);
        go.AddComponent<SelectionKeeper>();
        AddAudio(go, null, false, false, 0.6f);
        go.AddComponent<UIAudio>().clickClip = LoadClip("click");
    }
}
