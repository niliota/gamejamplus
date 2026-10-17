using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Build Map: Layout-dan divar, döşəmə, tavan, node-lar və jump-scare markerlərini yaradır.
// Yenidən işə salanda köhnə "Map" silinir və təzədən qurulur.
//   #  divar        .  döşəmə
//   N  node         P  başlanğıc node (oyunçu burada başlayır)
//   J  jump-scare markeri (döşəmədir, boş GameObject)
// Node-lar 4 istiqamətdə düz xətt üzrə (divara qədər) ilk rastlaşdıqları node-a bağlanır.
public static class MapBuilder
{
    const float Cell = 2f;        // bir xananın ölçüsü (m)
    const float WallHeight = 3f;

    static readonly string[] Layout =
    {
        "##################",
        "#.......##N#.....#",
        "#.......##N...N..#",
        "#...N.N.##J#.....#",
        "#.....J.##.#.....#",
        "######.###.#######",
        "#....#N..JN#.....#",
        "#....#.###.#.....#",
        "#.P...N###N.JN...#",
        "#....#####.#.....#",
        "######...#J#.....#",
        "######.NJ.N#######",
        "######...#########",
        "##################",
    };

    [MenuItem("Tools/Build Map")]
    static void Build()
    {
        int h = Layout.Length, w = Layout[0].Length;
        foreach (var row in Layout)
            if (row.Length != w) { Debug.LogError("MapBuilder: all layout rows must have the same length."); return; }

        var old = GameObject.Find("Map");
        if (old) Undo.DestroyObjectImmediate(old);

        var map = new GameObject("Map").transform;
        var geo = Child("Geometry", map);
        var nodesRoot = Child("Nodes", map);
        var scaresRoot = Child("JumpScares", map);

        // Döşəmə və tavan bütün grid-i örtür.
        var center = new Vector3((w - 1) * Cell / 2, 0, -(h - 1) * Cell / 2);
        // Materiallar ChernobylAbandoned pakından götürülür (Assets/Materials-da surəti saxlanır, tiling burada verilir).
        Box("Floor", geo, center + Vector3.down * 0.05f, new Vector3(w * Cell, 0.1f, h * Cell),
            Mat("M_Map_Floor", "SM_Floor_Terrazzo_2m", new Vector2(w * Cell / 2, h * Cell / 2)));

        // Tavan aşağı baxan Quad-dır: içəridən görünür, Scene view-da yuxarıdan baxanda görünmür.
        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(geo);
        ceiling.transform.SetPositionAndRotation(center + Vector3.up * WallHeight, Quaternion.Euler(-90, 0, 0));
        ceiling.transform.localScale = new Vector3(w * Cell, h * Cell, 1);
        var ceilingRenderer = ceiling.GetComponent<MeshRenderer>();
        ceilingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; // günəş içəri düşməsin
        // Pak teksturaları modelin UV atlasıdır; yalnız düz döşəmə teksturası təkrarlana bilir, tavana da boz tonla o verilir.
        var ceilingMat = Mat("M_Map_Ceiling_Concrete", "SM_Floor_Terrazzo_2m", new Vector2(w * Cell / 2, h * Cell / 2));
        ceilingMat.color = new Color(0.55f, 0.55f, 0.55f);
        ceilingRenderer.sharedMaterial = ceilingMat;

        // Divarlar: hər # xanası ayrıca Cube-dur (gövdə + collider). Döşəməyə toxunmayan xanalar qurulmur.
        var wallMat = Mat("M_Map_Wall", "SM_Wall_Segment_2m", Vector2.one);
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                if (Layout[r][c] == '#' && TouchesFloor(r, c, w, h))
                    Box("Wall", geo, new Vector3(c * Cell, WallHeight / 2, -r * Cell), new Vector3(Cell, WallHeight, Cell), wallMat);

        // Görünən üz: döşəməyə baxan hər divar üzünə pakın 2 m-lik divar seqmenti (3.3 m, üstü tavanın arxasında qalır).
        var segment = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath("SM_Wall_Segment_2m"));
        var faces = Child("WallFaces", geo);
        var sides = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
            {
                if (Layout[r][c] == '#') continue;
                foreach (var d in sides)
                {
                    int rr = r + d.y, cc = c + d.x;
                    if (rr < 0 || cc < 0 || rr >= h || cc >= w || Layout[rr][cc] != '#') continue;
                    var toWall = new Vector3(d.x, 0, -d.y);
                    var s = (GameObject)PrefabUtility.InstantiatePrefab(segment, faces);
                    s.transform.SetPositionAndRotation(new Vector3(c * Cell, 0, -r * Cell) + toWall * (Cell / 2 - 0.005f),
                        Quaternion.LookRotation(-toWall)); // modelin ön tərəfi (+Z) otağa baxır
                }
            }

        // Node-lar və jump-scare markerləri.
        var nodes = new Dictionary<Vector2Int, Node>();
        Node startNode = null;
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
            {
                char ch = Layout[r][c];
                if (ch == 'N' || ch == 'P')
                {
                    var n = Child($"Node_{r}_{c}", nodesRoot).gameObject.AddComponent<Node>();
                    n.transform.position = new Vector3(c * Cell, 0, -r * Cell);
                    nodes[new Vector2Int(c, r)] = n;
                    if (ch == 'P') startNode = n;
                }
                else if (ch == 'J')
                {
                    var j = Child($"JumpScare_{r}_{c}", scaresRoot);
                    j.position = new Vector3(c * Cell, 0, -r * Cell);
                    EditorGUIUtility.SetIconForObject(j.gameObject, (Texture2D)EditorGUIUtility.IconContent("sv_icon_dot14_pix16_gizmo").image);
                }
            }

        // Qonşular: hər node-dan 4 istiqamətə divara qədər gedib ilk node-u tapır.
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var kv in nodes)
            foreach (var d in dirs)
                for (var p = kv.Key + d; p.x >= 0 && p.y >= 0 && p.x < w && p.y < h && Layout[p.y][p.x] != '#'; p += d)
                    if (nodes.TryGetValue(p, out var other)) { kv.Value.neighbors.Add(other); break; }

        Undo.RegisterCreatedObjectUndo(map.gameObject, "Build Map");
        SetupPlayer(startNode);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        Debug.Log($"MapBuilder: built {nodes.Count} nodes.");
    }

    // Oyunçu yoxdursa yaradır (CharacterController + baş/kamera + fənər), sonra P nöqtəsinə qoyur.
    static void SetupPlayer(Node start)
    {
        if (!start) { Debug.LogError("MapBuilder: layout has no P (player start)."); return; }

        var oldMover = Object.FindAnyObjectByType<PlayerNodeMover>(); // köhnə node-based oyunçu
        if (oldMover) Undo.DestroyObjectImmediate(oldMover.gameObject);

        var player = Object.FindAnyObjectByType<HorrorController>();
        if (!player)
        {
            var oldCam = GameObject.Find("Main Camera");
            if (oldCam) oldCam.SetActive(false);

            var go = new GameObject("Player");
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = Vector3.up * 0.9f;
            player = go.AddComponent<HorrorController>();

            var head = new GameObject("Head", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = Vector3.up * 1.6f;
            head.GetComponent<Camera>().nearClipPlane = 0.05f; // boylananda divara yaxın gəlir
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(head.GetComponent<Camera>()).renderPostProcessing = true; // LethalLook effekti
            player.head = head.transform;

            var flashlight = Child("Flashlight", head.transform).gameObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 20;
            flashlight.spotAngle = 60;
            flashlight.intensity = 8;
            Undo.RegisterCreatedObjectUndo(go, "Build Map");
        }

        player.transform.position = start.transform.position;
        var look = start.neighbors.Count > 0 ? start.neighbors[0].transform.position - start.transform.position : Vector3.forward;
        player.transform.rotation = Quaternion.LookRotation(look);
    }

    static Transform Child(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static void Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        t.name = name;
        t.SetParent(parent);
        t.position = pos;
        t.localScale = size;
        t.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static bool TouchesFloor(int r, int c, int w, int h)
    {
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                int rr = r + dr, cc = c + dc;
                if (rr >= 0 && cc >= 0 && rr < h && cc < w && Layout[rr][cc] != '#') return true;
            }
        return false;
    }

    // Pakdakı modelin daxili materialının surətini Assets/Materials-da yaradır (bir dəfə), tiling-i təyin edir.
    static Material Mat(string name, string model, Vector2 tiling)
    {
        string path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var src = AssetDatabase.LoadAllAssetsAtPath(ModelPath(model)).OfType<Material>().First();
            mat = new Material(src);
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.mainTextureScale = tiling;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Pakdakı FBX modelin yolunu adına görə tapır (məs. "SM_Chair_Wooden").
    internal static string ModelPath(string name)
    {
        foreach (var g in AssetDatabase.FindAssets(name + " t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return p;
        }
        Debug.LogError($"Model not found: {name}");
        return null;
    }
}
