using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Tools > Build Chair MiniGame: (re)creates Assets/TuralTo/ChairMiniGame.unity — the floating chairs anomaly's
// Game Boy mini-game. Top-down arena built from Assets/Sprites, drawn 1:1 into RT_GameBoy (240x160).
// The scene is overwritten on every run, so tweak the layout here rather than in the scene.
public static class ChairMiniGameBuilder
{
    const string ScenePath = "Assets/TuralTo/ChairMiniGame.unity";
    const string SpriteDir = "Assets/Sprites/";
    const string RTPath = "Assets/GameBoy/RT_GameBoy.renderTexture";
    const float PPU = 16;   // 160px tall RT / ortho size 5 → 16px per unit, pixel-perfect
    const string ChairSprite = "lockerbarier.png"; // placeholder until a chair sprite exists

    // Tiles are 32px = 2 units. View is x -7.5..7.5, y -5..5.
    //   T  wall_top     W  wall_side (brick face)     .  floor
    // Row/column centres: x = -7 + 2*col, y = 5 - 2*row.
    static readonly string[] Layout =
    {
        "TTTTTTTT",
        "TWWWWWWT",
        "T......T",
        "T......T",
        "T......T",
        "TTTTTTTT",
    };

    [MenuItem("Tools/Build Chair MiniGame")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var floor = Sprite("floor.png");
        var wallTop = Sprite("wall_top.png");
        var wallSide = Sprite("wall_side.png", new Vector2(0.5f, 16f / 33)); // 33px tall: keep it on whole pixels
        var chair = Sprite(ChairSprite);
        Sprite[] Frames(string name) =>
            Enumerable.Range(0, 8).Select(i => $"Pingu/{name}_{i}.png")
                      .Where(f => System.IO.File.Exists(SpriteDir + f))
                      .Select(f => Sprite(f, new Vector2(0.5f, 0))).ToArray();

        // Built in its own additive scene so the open level isn't touched. If the mini-game scene itself is open,
        // it's cleared and rebuilt in place (it can't be overwritten while open).
        var active = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasOpen = scene.isLoaded;
        if (wasOpen)
            foreach (var go in scene.GetRootGameObjects()) Object.DestroyImmediate(go);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var root = new GameObject("ChairMiniGame");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(5000, -5000, 0); // far from the main level it's loaded next to

        // Camera → console screen
        var camGo = Child("MiniGameCamera", root.transform, new Vector3(0, 0, -10));
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPath);
        cam.allowHDR = false;
        cam.allowMSAA = false;
        // No post-processing / volumes: the main level's global volume would otherwise darken and dither the sprites.
        var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
        urp.renderPostProcessing = false;
        urp.dithering = false;
        urp.antialiasing = AntialiasingMode.None;
        urp.renderShadows = false;
        urp.volumeLayerMask = 0;
        urp.volumeTrigger = null;
        urp.SetRenderer(MiniGameRenderer()); // PC_Renderer's LethalLook + SSAO features would dither/darken it

        // Arena
        var arena = Child("Arena", root.transform, Vector3.zero).transform;
        for (int row = 0; row < Layout.Length; row++)
        for (int col = 0; col < Layout[row].Length; col++)
        {
            char c = Layout[row][col];
            var s = c == 'T' ? wallTop : c == 'W' ? wallSide : floor;
            Renderer($"{c}_{col}_{row}", arena, new Vector3(-7 + 2 * col, 5 - 2 * row, 0), s, c == '.' ? -1000 : -900);
        }

        // Player
        var playerSr = Renderer("Player", root.transform, new Vector3(0, -1.5f, 0), Frames("idle")[0], 0);
        playerSr.transform.localScale = Vector3.one * 2; // 16px art → 32px, same cell size as the tiles (integer scale only)
        var player = playerSr.gameObject.AddComponent<TopDownPlayer>();
        player.idle = Frames("idle");
        player.down = Frames("down");
        player.up = Frames("up");
        player.left = Frames("left");
        player.right = Frames("right");

        // Chair template (disabled; ChairDodgeGame clones it)
        var chairGo = Child("ChairTemplate", root.transform, new Vector3(0, 0, 0));
        var shadow = Renderer("Shadow", chairGo.transform, Vector3.zero, chair, -400);
        shadow.color = new Color(0, 0, 0, 0.4f);
        shadow.transform.localScale = new Vector3(1, 0.5f, 1);
        var body = Renderer("Body", chairGo.transform, Vector3.zero, chair, 0);
        var thrown = chairGo.AddComponent<ThrownChair>();
        thrown.body = body;
        thrown.shadow = shadow;
        chairGo.SetActive(false);

        // Countdown on the top wall
        var textGo = Child("Timer", root.transform, new Vector3(0, 4.55f, 0));
        var text = textGo.AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textGo.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        textGo.GetComponent<MeshRenderer>().sortingOrder = 1000;
        text.fontSize = 40;
        text.characterSize = 0.3f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.85f, 0.85f, 0.75f);
        text.text = "15";

        var game = root.AddComponent<ChairDodgeGame>();
        game.player = player;
        game.chairTemplate = thrown;
        game.cam = cam;
        game.timerText = text;

        EditorSceneManager.SaveScene(scene, ScenePath);
        if (!wasOpen)
        {
            if (active.IsValid()) SceneManager.SetActiveScene(active);
            EditorSceneManager.CloseScene(scene, true);
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        Debug.Log($"ChairMiniGameBuilder: saved {ScenePath}");
    }

    // A renderer without features (copy of Mobile_Renderer), registered on every URP asset at the same index.
    static int MiniGameRenderer()
    {
        const string path = "Assets/TuralTo/MiniGame_Renderer.asset";
        if (!AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)) AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset", path);
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        var rso = new SerializedObject(renderer);
        rso.FindProperty("m_RenderingMode").intValue = (int)RenderingMode.ForwardPlus; // Forward would disable the GPU Resident Drawer project-wide
        rso.ApplyModifiedProperties();

        int index = -1;
        foreach (var rpPath in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(rpPath));
            var list = so.FindProperty("m_RendererDataList");
            int i = Enumerable.Range(0, list.arraySize).FirstOrDefault(n => list.GetArrayElementAtIndex(n).objectReferenceValue == renderer);
            if (list.arraySize == 0 || list.GetArrayElementAtIndex(i).objectReferenceValue != renderer)
            {
                i = list.arraySize;
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
                so.ApplyModifiedProperties();
            }
            if (index >= 0 && i != index) Debug.LogWarning($"ChairMiniGameBuilder: MiniGame_Renderer has index {i} in {rpPath}, expected {index}");
            index = index < 0 ? i : index;
        }
        AssetDatabase.SaveAssets();
        return index;
    }

    // Imports a sprite pixel-perfect (16 PPU, point filter, no compression) and loads it.
    static Sprite Sprite(string file, Vector2? pivot = null)
    {
        string path = SpriteDir + file;
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PPU;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot ?? new Vector2(0.5f, 0.5f);
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static GameObject Child(string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go;
    }

    static SpriteRenderer Renderer(string name, Transform parent, Vector3 localPos, Sprite sprite, int order)
    {
        var sr = Child(name, parent, localPos).AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }
}
