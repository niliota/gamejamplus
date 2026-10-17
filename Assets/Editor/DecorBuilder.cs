using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Build Decor: otaqları ChernobylAbandoned pakının modelləri ilə bəzəyir.
// Hər şey "Decor" altına gedir; yenidən işə salanda köhnə Decor silinir. Map-ə toxunmur.
// Koordinatlar metrdir (MapBuilder: x = sütun*2, z = -sətir*2). Qapıdan otağa gedən yol (node xətləri) boş saxlanılıb.
// Pakda modellərin ön tərəfi +Z-dir; yerləşdirmə bounds-a görədir, pivot fərqləri nəzərə alınır.
public static class DecorBuilder
{
    const float CeilingY = 3f;
    enum Side { N, S, E, W }

    static Transform parent;
    static float x0, x1, z0, z1; // cari otağın iç sərhədləri

    [MenuItem("Tools/Build Decor")]
    static void Build()
    {
        var old = GameObject.Find("Decor");
        if (old) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Decor").transform;

        Lab(root);
        Storage(root);
        Power(root);
        Archive(root);
        Security(root);
        Corridors(root);
        Doors(root);
        SecurityCameras(root);
        SecurityMonitor();
        Anomalies(root);
        Enemy(root);
        Lives(root);
        Audio(root);
        BakeNavMesh(root);
        EnemySpawns(root);

        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Decor");
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
    }

    // ---------- Otaqlar ----------

    // Əsas Laboratoriya (14×8 m, qapı cənubda x 11-13): şimalda panel divarı və pultlar, qərbdə terminallar.
    static void Lab(Transform root)
    {
        Room(root, "Lab_MainLaboratory", 1, 15, -9, -1);

        WallRow(Side.N, 2, 0, "SM_WallPanel_Annunciator", "SM_WallPanel_Gauges", "SM_WallPanel_RodDisplay",
            "SM_WallPanel_Recorders", "SM_WallPanel_PipeDiagram", "SM_WallPanel_KnifeSwitches");
        At("SM_ControlConsole_A", 3.0f, -3.0f, 180);
        At("SM_ControlConsole_B_AZ5", 4.95f, -3.0f, 180);
        At("SM_ControlConsole_Short", 6.6f, -3.0f, 180);
        var chairA = At("SM_OperatorChair", 3.0f, -4.3f, 0);   // anomaliya 1 (havaya qalxan stullar)
        At("SM_OperatorChair", 4.95f, -4.4f, 15);
        At("SM_OperatorChair", 6.6f, -4.4f, 75, tilt: 90); // aşılmış
        Wall("SM_WallClock_Stopped_0123", Side.N, 11, 2.2f);
        Wall("SM_Lockers_Triple", Side.N, 13.5f);
        for (float x = 2; x <= 14; x += 2) Wall("SM_CableTray_2m", Side.N, x, 2.75f);
        Ceil("SM_Cable_Hanging", 10, -1.6f, 0);

        WallRow(Side.E, -4.6f, 0, "SM_RelayCabinet_Closed", "SM_RelayCabinet_Open", "SM_RelayCabinet_Closed");

        Wall("SM_Shelf_Metal_Stocked", Side.W, -3.0f);
        var d1 = Wall("SM_OfficeDesk_Soviet", Side.W, -6.0f);
        var d2 = Wall("SM_OfficeDesk_Soviet", Side.W, -7.4f);
        Top(d1, "SM_ComputerTerminal_CRT", 0, 0, 90);
        Top(d2, "SM_ComputerTerminal_CRT", 0, 0.1f, 90);
        var chairB = At("SM_OperatorChair", 2.9f, -6.0f, 270); // anomaliya 1
        var chairC = At("SM_OperatorChair", 2.9f, -7.4f, 250); // anomaliya 1

        var bench = At("SM_OfficeDesk_Soviet", 5.5f, -6.8f, 180);
        Top(bench, "SM_DeskLamp", -0.4f, 0.2f, 200);
        Top(bench, "SM_Dosimeter_DP5", 0.05f, -0.05f, 160);
        Top(bench, "SM_Clipboard", 0.4f, -0.1f, 30);
        Top(bench, "SM_GlassJar_3L", 0.45f, 0.25f, 0);
        At("SM_OperatorChair", 5.5f, -7.8f, 10);
        labFloatingChairs = new[] { chairA.transform, chairB.transform, chairC.transform }; // CAM 1-də ən aydın görünən 3 stul

        Wall("SM_Lockers_Triple", Side.S, 4);
        Wall("SM_FireHoseCabinet", Side.S, 6.5f);
        Wall("SM_Poster_PeacefulAtom", Side.S, 7.8f, 1.3f);
        Wall("SM_Radiator_CastIron", Side.S, 9, 0.12f);
        Wall("SM_FireExtinguisher_OU", Side.S, 14.2f);
        Wall("SM_Sign_Radiation", Side.S, 14.2f, 1.6f);

        At("SM_Debris_Plaster_A", 9.8f, -4.2f, 30);
        At("SM_Debris_Plaster_B", 10.2f, -3.9f, 80);
        At("SM_CeilingTile_Broken", 9.4f, -3.4f, 25);
        At("SM_Paper_Sheet", 7.8f, -4.6f, 110);
        At("SM_Cable_Coil", 8.2f, -2.2f, 0);

        // Əlavə: ortada ikinci iş masası, şimal divarında rəf, şərqdə qutular; hamısı iki kameradan görünür
        Wall("SM_Shelf_Metal_Stocked", Side.N, 10.0f);
        var desk2 = At("SM_SchoolDesk_Soviet", 10.5f, -5.6f, 180);
        Top(desk2, "SM_Books_Stack", -0.3f, 0, 15);
        Top(desk2, "SM_DeskLamp", 0.3f, 0.1f, 200);
        Top(desk2, "SM_Bottle_Glass", 0.05f, -0.15f, 0);
        At("SM_Stool_Wooden", 10.5f, -6.6f, 0);
        Wall("SM_Bookshelf_Wooden", Side.W, -4.3f);
        Top(d1, "SM_Paper_Stack", 0, 0.42f, 80);
        Top(d2, "SM_Mug_Enamel", 0.1f, -0.45f, 0);
        var crate = Wall("SM_Crate_Military_Large", Side.S, 7.6f); // LAB EAST kamerasının qarşısını kəsməsin
        Top(crate, "SM_Crate_Military_Small", 0, 0.03f, 10);
        At("SM_OilDrum_Rusty", 11.9f, -1.9f, 0);
        At("SM_Bucket_Enamel", 12.9f, -2.5f, 0);
        Wall("SM_Poster_GloryToLabour", Side.E, -7.0f, 1.3f);
        Wall("SM_Calendar_1986_April", Side.W, -6.7f, 1.5f);
        At("SM_Paper_Sheet", 9f, -6.9f, 30);
        At("SM_Paper_Sheet", 12.8f, -3.6f, 140);

        Ceil("SM_FluorescentLight_Hanging", 4, -5.5f, 0);
        Ceil("SM_FluorescentLight_Hanging", 10, -5.5f, 0);
        Lamp(6, -5);
    }

    // Anbar (10×8 m, qapı qərbdə z -5..-3): rəflər, qutular, çəlləklər.
    static void Storage(Transform root)
    {
        Room(root, "Storage_Anbar", 23, 33, -9, -1);

        WallRow(Side.N, 24, 0, "SM_Shelf_Metal_Stocked", "SM_Shelf_Metal_Stocked", "SM_Shelf_Metal_Stocked");
        WallRow(Side.N, 29.5f, 0, "SM_Shelf_Metal_Stocked", "SM_Shelf_Metal_Stocked");
        WallRow(Side.E, -8.4f, 0, "SM_Shelf_Metal_Stocked", "SM_Shelf_Metal_Stocked");
        WallRow(Side.E, -4.2f, 0, "SM_Shelf_Metal_Stocked", "SM_Shelf_Metal_Stocked");

        var c1 = Wall("SM_Crate_Military_Large", Side.S, 24.5f);
        var c2 = Wall("SM_Crate_Military_Large", Side.S, 25.55f);
        Top(c1, "SM_Crate_Military_Large", 0, 0, 3);
        Top(c2, "SM_Crate_Military_Small", 0.1f, 0, -8);
        At("SM_Crate_Military_Small", 26.8f, -8.4f, 15);
        Wall("SM_GasCylinder_Propane", Side.S, 28.3f);
        Wall("SM_GasCylinder_Propane", Side.S, 28.58f);
        At("SM_GlassJar_3L", 27.6f, -8.6f, 0);
        At("SM_Bottle_Glass", 27.85f, -8.5f, 0);

        At("SM_OilDrum_Yellow_Radioactive", 32.1f, -8.3f, 40);
        At("SM_OilDrum_Rusty", 31.3f, -8.4f, 0);
        At("SM_OilDrum_Green", 31.9f, -7.5f, 10);
        At("SM_OilDrum_Rusty", 29.8f, -7.0f, 0, tilt: 90); // yıxılmış
        Wall("SM_Sign_Radiation", Side.S, 32, 1.7f);

        var c3 = At("SM_Crate_Military_Large", 28.5f, -6.2f, 5);
        var c4 = Top(c3, "SM_Crate_Military_Large", 0, 0, -10);
        Top(c4, "SM_HardHat", 0, 0, 30);
        At("SM_Crate_Military_Small", 29.6f, -6.0f, 20);

        Wall("SM_Lockers_Triple", Side.W, -7.5f);
        Wall("SM_Lockers_Triple", Side.W, -8.42f);
        Wall("SM_FireExtinguisher_OU", Side.W, -2);
        At("SM_Bucket_Enamel", 26.5f, -7.6f, 0);
        At("SM_Cable_Coil", 27f, -2.5f, 60);

        // Əlavə: rəflər arasında qutu qalağı, şərqdə çəllək, cənub-qərbdə qutu, divarlarda poster və saat
        var c5 = Wall("SM_Crate_Military_Large", Side.N, 28.4f);
        Top(c5, "SM_Crate_Military_Small", 0, 0, -12);
        At("SM_OilDrum_Green", 31.8f, -5.3f, 0);
        At("SM_Crate_Military_Small", 31.0f, -5.6f, 35);
        var c6 = At("SM_Crate_Military_Large", 25.0f, -6.5f, 20);
        Top(c6, "SM_Binder_Folder", 0.1f, 0, 50);
        Wall("SM_Poster_PeacefulAtom", Side.W, -6.3f, 1.3f);
        Wall("SM_WallClock_Stopped_0123", Side.S, 29.9f, 2.1f);
        At("SM_Paper_Sheet", 30.6f, -3.2f, 60);
        At("SM_GlassJar_3L", 30.2f, -2.3f, 0);

        // Döşəmə saatı (CLOCK qovluğu): otağın ortasında, üzü STORAGE kamerasına; 3-cü anomaliya
        GrandfatherClock(new Vector3(28.6f, 0, -4.6f), 50.7f);

        Ceil("SM_FluorescentLight", 26, -5, 0);
        Ceil("SM_FluorescentLight", 30, -5, 0);
        Lamp(28, -5);
    }

    // Güc/Reaktor (10×10 m, qapı qərbdə z -17..-15): mərkəzdə placeholder reaktor, divarlarda panellər və borular.
    static void Power(Transform root)
    {
        Room(root, "Power_Reactor", 23, 33, -21, -11);

        Reactor(30, -14.5f);
        foreach (var z in new[] { -14f, -15f })
        {
            var p = At("SM_Pipe_Straight_2m", 32.15f, z, 0, 2.07f);
            p.transform.localScale = new Vector3(0.85f, 1, 1); // reaktordan divara qədər
        }

        WallRow(Side.N, 23.8f, 0, "SM_WallPanel_KnifeSwitches", "SM_RelayCabinet_Closed", "SM_RelayCabinet_Open");
        Wall("SM_WallPanel_Looted", Side.N, 28);
        Wall("SM_JunctionBox", Side.N, 29.3f, 0.9f);
        Wall("SM_Sign_HighVoltage", Side.N, 30.2f, 1.6f);
        Wall("SM_WallPanel_Gauges", Side.N, 31.9f);
        for (float x = 24; x <= 32; x += 2) Ceil("SM_Duct_Round_2m", x, -12, 0);

        Wall("SM_FireExtinguisher_OU", Side.W, -13);
        Wall("SM_Sign_Radiation", Side.W, -17.8f, 1.6f);
        Wall("SM_ControlConsole_B_AZ5", Side.W, -19.6f);
        At("SM_OperatorChair", 25.2f, -19.3f, 250); // kameranın altında 4-cü anomaliyanın (varlıq) yeri boş qalsın

        WallRow(Side.S, 25.2f, 0.35f, "SM_Pipe_Straight_2m", "SM_Pipe_Valve_Handwheel", "SM_Pipe_Straight_2m", "SM_Pipe_Straight_2m");
        WallRow(Side.S, 23.2f, 2.3f, "SM_Pipe_Straight_2m", "SM_Pipe_Straight_2m", "SM_Pipe_Straight_2m", "SM_Pipe_Straight_2m");

        WallRow(Side.E, -20.9f, 0, "SM_RelayCabinet_Closed", "SM_RelayCabinet_Open", "SM_RelayCabinet_Closed");
        foreach (var z in new[] { -20f, -18f, -16f }) Wall("SM_CableTray_2m", Side.E, z, 2.75f);
        At("SM_GasCylinder_Propane", 32.6f, -12.0f, 0);
        At("SM_GasCylinder_Propane", 32.3f, -12.4f, 0);

        At("SM_OilDrum_Yellow_Radioactive", 31.2f, -20.2f, 0);
        At("SM_OilDrum_Yellow_Radioactive", 30.5f, -20.3f, 30);
        At("SM_OilDrum_Yellow_Radioactive", 28.3f, -19.8f, 70, tilt: 90);
        At("SM_Debris_Plaster_A", 25.5f, -13.5f, 0);
        At("SM_Debris_Plaster_B", 25.8f, -13.2f, 50);
        At("SM_CeilingTile_Broken", 26.2f, -13.9f, 15);
        At("SM_Cable_Coil", 25.6f, -18.4f, 0);

        // Əlavə (kameranın gördüyü cənub tərəfdə): qutu, poster, saat, nişan; şərq tərəfi reaktorun arxasında qalır
        At("SM_Crate_Military_Large", 29.6f, -19.0f, 10);
        Wall("SM_Poster_PeacefulAtom", Side.S, 26, 1.2f);
        Wall("SM_WallClock_Stopped_0123", Side.S, 29, 1.85f);
        Wall("SM_Sign_Radiation", Side.S, 31.6f, 1.5f);
        At("SM_HardHat", 25.6f, -20.1f, 40);
        At("SM_Paper_Sheet", 30.8f, -16.8f, 20);
        At("SM_Paper_Sheet", 25f, -14.2f, 110);

        Ceil("SM_FluorescentLight_Hanging", 26, -13.5f, 0);
        Lamp(26, -13.5f);
        Lamp(28, -17.5f, 1f, new Color(0.6f, 1f, 0.6f), 1.2f); // reaktorun yaşılımtıl parıltısı
    }

    // Arxiv/Ofis (8×8 m, qapı şərqdə z -17..-15). Oyunçu burada başlayır (4, -16), şərqə baxır.
    static void Archive(Transform root)
    {
        Room(root, "Archive_Office", 1, 9, -19, -11);

        WallRow(Side.W, -18.6f, 0, Repeat("SM_FilingCabinet", 6));
        WallRow(Side.W, -14.6f, 0, Repeat("SM_FilingCabinet", 6));
        WallRow(Side.N, 2.6f, 0, Repeat("SM_Bookshelf_Wooden", 3));
        Wall("SM_Wardrobe_Wooden", Side.N, 7);

        Wall("SM_Calendar_1986_April", Side.E, -12.8f, 1.4f);
        WallRow(Side.E, -14.4f, 0, Repeat("SM_FilingCabinet", 2));
        Wall("SM_FireExtinguisher_OU", Side.E, -17.5f);
        Wall("SM_WallClock_Stopped_0123", Side.E, -18.2f, 2.1f);

        WallRow(Side.S, 2.6f, 0, Repeat("SM_Bookshelf_Wooden", 2));
        Wall("SM_Radiator_CastIron", Side.S, 6, 0.12f);
        Wall("SM_Poster_GloryToLabour", Side.S, 7.6f, 1.3f);

        var desk = At("SM_OfficeDesk_Soviet", 5.5f, -13.0f, 0);
        Top(desk, "SM_DeskLamp", -0.45f, 0.1f, 160);
        Top(desk, "SM_Telephone_Rotary", 0.35f, 0.1f, 190);
        Top(desk, "SM_Paper_Stack", 0, -0.15f, 5);
        Top(desk, "SM_Binder_Folder", -0.15f, 0.25f, 60);
        Top(desk, "SM_Mug_Enamel", 0.45f, -0.25f, 0);
        At("SM_Chair_Wooden", 5.5f, -12.2f, 180);
        At("SM_Chair_Wooden", 6.8f, -13.9f, 40, tilt: 90);

        At("SM_Paper_Sheet", 3f, -17.8f, 20);
        At("SM_Paper_Sheet", 6.5f, -17.5f, 75);
        At("SM_Paper_Sheet", 2.6f, -13.8f, 140);
        At("SM_Paper_Sheet", 5f, -18.3f, 200);
        At("SM_Paper_Stack", 2.4f, -15.2f, 30);
        At("SM_Books_Stack", 2.3f, -18.2f, 15);
        At("SM_Binder_Folder", 7.2f, -18.0f, 60);

        // Əlavə: cənubda oxu masası, şimalda stul və kitab qalağı, şərqdə kitab rəfi, şimal divarında poster
        var reading = At("SM_SchoolDesk_Soviet", 5.0f, -17.9f, 0);
        Top(reading, "SM_Books_Stack", -0.25f, 0.05f, 10);
        Top(reading, "SM_DeskLamp", 0.3f, 0.05f, 160);
        Top(reading, "SM_Paper_Stack", 0.05f, -0.1f, 95);
        At("SM_Chair_Wooden", 6.0f, -18.0f, 300);
        At("SM_Chair_Wooden", 3.3f, -12.0f, 200);
        At("SM_Books_Stack", 2.7f, -11.5f, 40);
        Wall("SM_Bookshelf_Wooden", Side.E, -12.0f);
        Wall("SM_Poster_PeacefulAtom", Side.N, 3.8f, 1.7f);

        Ceil("SM_FluorescentLight", 5, -13, 0);
        Ceil("SM_FluorescentLight", 5, -17, 0);
        Lamp(5, -15.5f);
    }

    // Mühafizə (6×6 m, qapı şərqdə z -23..-21): gözətçi masası, monitor, şkaflar, çarpayı.
    static void Security(Transform root)
    {
        Room(root, "Security_Guard", 11, 17, -25, -19);

        var desk = Wall("VintageDesk", Side.W, -22);                      // SECURITY DESK qovluğu
        var pc = Top(desk, "Computer Monitor OLD", 0, 0.25f, 90, 0.85f);  // COMPUTER qovluğu; masaya sığsın deyə 0.85
        Top(desk, "SM_Telephone_Rotary", 0.1f, -0.75f, 100);
        Top(desk, "SM_Mug_Enamel", 0.25f, -0.4f, 0);
        Top(desk, "SM_DeskLamp", 0, 0.95f, 250);
        pc.transform.SetParent(desk.transform, true); // kompüterə klik də masaya çatsın (SecurityDesk)
        securityDesk = desk;
        securityPc = pc;
        At("SM_OperatorChair", 12.55f, -22, 270);
        At("SM_TV_CRT_Soviet", 11.55f, -19.6f, 135);

        WallRow(Side.N, 13.4f, 0, "SM_Lockers_Triple", "SM_Lockers_Triple");
        Wall("SM_Radio_WallSpeaker", Side.N, 12.6f, 2.2f);
        Wall("SM_WallClock_Stopped_0123", Side.N, 16.2f, 2.2f);

        Wall("SM_KindergartenBed_Metal", Side.S, 14.4f);
        Wall("SM_FirstAidBox", Side.S, 12.3f, 1.3f);
        var stool = At("SM_Stool_Wooden", 16.2f, -24.5f, 0);
        Top(stool, "SM_Kettle_Enamel", 0, 0, 30);
        Wall("SM_FireExtinguisher_OU", Side.S, 16.6f);
        At("SM_Paper_Sheet", 13.5f, -20.3f, 50);

        // Əlavə: şimal-şərqdə soyuducu və kartoteka, cənubda qutu + dozimetr, masada lampa, divarlarda təqvim və poster
        Wall("SM_FilingCabinet", Side.N, 15.8f);
        Wall("SM_Fridge_Soviet", Side.N, 16.6f);
        var c7 = At("SM_Crate_Military_Small", 13.1f, -24.6f, 15);
        Top(c7, "SM_Dosimeter_DP5", 0, 0, 200);
        Wall("SM_Calendar_1986_April", Side.W, -21.3f, 1.6f);
        Wall("SM_Poster_GloryToLabour", Side.N, 12.4f, 1.2f);

        Ceil("SM_CeilingLamp_Shade", 14, -22, 0);
        Lamp(14, -22, 1.2f, new Color(1f, 0.85f, 0.6f));
    }

    // Koridorlar: tavanda boru və kabel yolu, lampalar, bir neçə nişan.
    static void Corridors(Transform root)
    {
        Room(root, "Corridor_Main", 19, 21, -23, -1);
        for (float z = -2; z >= -22; z -= 2)
        {
            Wall("SM_Pipe_Straight_2m", Side.E, z, 2.5f);
            Wall("SM_CableTray_2m", Side.W, z, 2.8f);
        }
        foreach (var z in new[] { -4f, -10f, -16f, -21f }) Ceil("SM_FluorescentLight", 20, z, 90);
        Wall("SM_VentGrille", Side.N, 20, 2.3f);
        Wall("SM_Sign_Radiation", Side.E, -13.6f, 1.6f);
        Wall("SM_Sign_HighVoltage", Side.E, -18.6f, 1.5f);
        Wall("SM_Radio_WallSpeaker", Side.W, -3, 2.3f);
        Wall("SM_FireExtinguisher_OU", Side.W, -8);
        Wall("SM_Radiator_CastIron", Side.W, -17.5f, 0.12f);
        Lamp(20, -6, 1.2f);
        Lamp(20, -19, 1.2f);

        Room(root, "Corridor_Junction", 11, 19, -17, -9);
        Ceil("SM_FluorescentLight", 12, -14, 90);
        Ceil("SM_FluorescentLight", 16, -12, 0);
        Lamp(14, -13, 1.2f);
    }

    // Qapılar (DOOR qovluğu): hər 2 m-lik otaq qapısına iki qanad (hərəsi 1 m) və üstünə 1 m-lik tir.
    // a, b = açılışın kənarları (otaq tərəfdəki divar müstəvisində), d = otağa doğru. Sol klik ilə açılır (Door.cs).
    static void Doors(Transform root)
    {
        Room(root, "Doors", 0, 0, 0, 0);
        // son iki rəqəm: qanadların başlanğıc açılma bucağı (0 = bağlı)
        Door(new Vector3(11, 0, -9), new Vector3(13, 0, -9), Vector3.forward, 0, 0);  // Laboratoriya
        Door(new Vector3(23, 0, -5), new Vector3(23, 0, -3), Vector3.right, 0, 0);    // Anbar
        Door(new Vector3(23, 0, -17), new Vector3(23, 0, -15), Vector3.right, 0, 0);  // Güc/Reaktor
        Door(new Vector3(9, 0, -17), new Vector3(9, 0, -15), Vector3.left, 0, 0);     // Arxiv
        Door(new Vector3(17, 0, -23), new Vector3(17, 0, -21), Vector3.left, 0, 0);   // Mühafizə
    }

    static void Door(Vector3 a, Vector3 b, Vector3 d, float openA, float openB)
    {
        Vector3 ab = (b - a).normalized;
        float leafScale = Vector3.Distance(a, b) / 2 / 1.044f; // modelin eni 1.044 m
        Leaf(a - d * 0.11f, ab, d, openA, leafScale);
        Leaf(b - d * 0.11f, -ab, d, openB, leafScale);

        var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        lintel.name = "DoorLintel";
        lintel.SetParent(parent);
        lintel.SetPositionAndRotation((a + b) / 2 - d * 0.11f + Vector3.up * 2.5f, Quaternion.LookRotation(d));
        lintel.localScale = new Vector3(Vector3.Distance(a, b), 1, 0.22f);
        lintel.GetComponent<Renderer>().sharedMaterial = SimpleMat("M_DoorLintel", new Color(0.72f, 0.70f, 0.64f), 0, 0.2f);
        lintel.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true; // 2 m-dən başlayır: NavMesh qapını bağlı saymasın
    }

    // Qanad: pivot menteşədədir, Door komponenti onu otağa doğru 0..90° fırladır; başlanğıcda 'angle' dərəcə aralıdır.
    // Modelin pivotu menteşə tərəfindədir (yerli x = 0..1.044).
    static void Leaf(Vector3 hinge, Vector3 along, Vector3 d, float angle, float scale)
    {
        var closed = Quaternion.LookRotation(Vector3.Cross(along, Vector3.up), Vector3.up); // bağlı: qanad açılış boyunca
        float sign = Vector3.Dot(d, closed * Vector3.forward) > 0 ? -1 : 1;                 // +Y fırlanma x-i -z-yə aparır
        var pivot = new GameObject("DoorLeaf").transform;
        pivot.SetParent(parent);
        pivot.SetPositionAndRotation(hinge, closed * Quaternion.Euler(0, sign * angle, 0));
        pivot.localScale = new Vector3(scale, 1, 1);
        var door = pivot.gameObject.AddComponent<Door>();
        door.angle = sign * angle;
        door.openAngle = sign * 90;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapBuilder.ModelPath("model")), pivot);
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<BoxCollider>();
        pivot.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true; // NavMesh qapıdan keçsin (varlıq qapını özü açır)
    }

    static Material SimpleMat(string name, Color color, float metallic, float smoothness)
    {
        string path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // Anomaliyalar sırayla (AnomalyManager). Hər otaqda bir; yeniləri bura sırası ilə əlavə olunur.
    static Transform[] labFloatingChairs;

    static void Anomalies(Transform root)
    {
        var go = new GameObject("Anomalies");
        go.transform.SetParent(root, false);
        var manager = go.AddComponent<AnomalyManager>();

        // 1. Laboratoriya (CAM 1 LAB WEST): 3 stul havaya qalxır, titrəyir, ətrafı bulanıqlaşır
        var chairs = new GameObject("1_Lab_FloatingChairs").AddComponent<FloatingChairsAnomaly>();
        chairs.transform.SetParent(go.transform, false);
        chairs.roomName = "LAB";
        chairs.miniGameScene = ""; // hələlik mini oyun yoxdur: skan düzəldir (sonra məs. "GameBoyMiniGame")
        chairs.miniGameLevel = 1;
        chairs.chairs = labFloatingChairs;
        const string matPath = "Assets/Materials/M_AnomalyDistortion.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat) { mat = new Material(Shader.Find("Custom/AnomalyDistortion")); AssetDatabase.CreateAsset(mat, matPath); }
        chairs.distortion = mat;
        chairs.startDelay = 20; // oyun başlayandan

        // 2. Anbar (CAM 3 STORAGE): döşəmənin ortasında qorxulu "Crepusculum" yazısı peyda olur
        var floor = new GameObject("2_Storage_FloorWriting").AddComponent<FloorWritingAnomaly>();
        floor.transform.SetParent(go.transform, false);
        floor.roomName = "STORAGE";
        floor.startDelay = 20; // 1-ci həll olunandan sonra
        floor.miniGameScene = ""; // hələlik mini oyun yoxdur: skan düzəldir
        floor.miniGameLevel = 2;
        floor.writing = ScaryWriting(floor.transform, "Crepusculum", new Vector3(27.1f, 0.01f, -3.7f), new Vector3(33, 0, -1), 4.6f, 1.7f);

        // 3. Anbar (CAM 3 STORAGE): döşəmə saatı titrəyir, havaya qalxır, əqrəblər tərsinə, qırmızı işıqlar
        var clock = new GameObject("3_Storage_Clock").AddComponent<ClockAnomaly>();
        clock.transform.SetParent(go.transform, false);
        clock.roomName = "STORAGE";
        clock.startDelay = 20; // 2-ci həll olunandan sonra
        clock.miniGameScene = ""; // hələlik mini oyun yoxdur: skan düzəldir
        clock.miniGameLevel = 3;
        clock.clock = storageClock;
        clock.hourHand = clockHour;
        clock.minuteHand = clockMinute;
        clock.secondHand = clockSecond;
        clock.redLights = clockLights;

        // 4. Reaktor (CAM 4 REACTOR, son anomaliya): varlıq kameranın düz altında peyda olur, tərpənmir, kameraya baxır
        var entity = new GameObject("4_Reactor_Entity").AddComponent<EntityAnomaly>();
        entity.transform.SetParent(go.transform, false);
        entity.roomName = "REACTOR";
        entity.startDelay = 20; // 3-cü həll olunandan sonra
        entity.miniGameScene = ""; // hələlik mini oyun yoxdur: skan düzəldir
        entity.miniGameLevel = 4;
        Entity(entity, cctv[3].transform, new Vector3(24.7f, 0, -20.05f), mat);

        manager.sequence = new Anomaly[] { chairs, floor, clock, entity };
        chairs.cctvIndex = 0; floor.cctvIndex = 2; clock.cctvIndex = 2; entity.cctvIndex = 3; // cctv[] sırası
        chairs.loopSound = Sound("anomaly sound of tables.wav", stream: true);
    }

    static AudioClip Sound(string file, bool stream = false)
    {
        string path = "Assets/sounds/" + file;
        if (stream) // uzun fayl (5 dəq): yaddaşa tam yüklənməsin
        {
            var imp = (AudioImporter)AssetImporter.GetAtPath(path);
            var s = imp.defaultSampleSettings;
            if (s.loadType != AudioClipLoadType.Streaming)
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (!clip) Debug.LogWarning("Sound not found: " + path);
        return clip;
    }

    // Bütün oyun səsləri (GameAudio)
    static void Audio(Transform root)
    {
        var go = new GameObject("Audio");
        go.transform.SetParent(root, false);
        var a = go.AddComponent<GameAudio>();
        a.doorOpen = Sound("door opening.wav");
        a.doorClose = Sound("door closing.wav");
        a.cctvAlert = Sound("camedra anomaly detectsion sound will dbe played  from camera when we saw anomaly in  camera.wav");
        a.creepy = new[] { Sound("random creepy sound.wav"), Sound("random creepy sound.mp3"), Sound("random creepy sound (2).mp3") };
        a.horrorMusic = Sound("random horror music effect to increaase  horror.wav", stream: true);
        a.chase = Sound("when entity chase  us.wav");
        a.endMusic = Sound("game end music when we won  it will play full when we fail last beep[ will not play.wav");
    }

    // Düşmən varlıq (EnemyEntity): oyunun əvvəlində gizlidir; anomaliya vaxtında düzəldilməsə spawn olur.
    // Animator: "Idle" (Zombie Idle) və "Crawl" (Running Crawl), ikisi də dövrü (loop).
    static void Enemy(Transform root)
    {
        var go = new GameObject("Enemy");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(8, 0, -5); // test üçün activeOnStart olanda burada (Lab) peyda olur
        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 1.9f;
        agent.angularSpeed = 360;
        agent.acceleration = 12;
        agent.stoppingDistance = 0.3f;
        agent.enabled = false;

        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/entity/Character_Monster_04.fbx"), go.transform);
        model.transform.localScale = Vector3.one * 0.4f; // model ~6 m-dir
        var animator = model.GetComponent<Animator>() ? model.GetComponent<Animator>() : model.AddComponent<Animator>();
        animator.runtimeAnimatorController = EnemyAnimator();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var bones = model.GetComponentsInChildren<Transform>();
        var enemy = go.AddComponent<EnemyEntity>();
        enemy.animator = animator;
        enemy.eyes = bones.First(t => t.name == "mixamorig:Head");
        enemy.hips = bones.First(t => t.name == "mixamorig:Hips");
        enemy.activeOnStart = true; // TEST: sonra false et (normalda yalnız anomaliya səhv olanda gəlir)
        foreach (var r in model.GetComponentsInChildren<Renderer>()) r.enabled = false;
    }

    static UnityEditor.Animations.AnimatorController EnemyAnimator()
    {
        const string path = "Assets/entity/Entity.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (ctrl) return ctrl;
        ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        sm.defaultState = AddLoopState(sm, "Idle", "Assets/entity/Zombie Idle.fbx");
        AddLoopState(sm, "Crawl", "Assets/entity/Running Crawl.fbx");
        return ctrl;
    }

    static UnityEditor.Animations.AnimatorState AddLoopState(UnityEditor.Animations.AnimatorStateMachine sm, string name, string fbx)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips) c.loopTime = true;
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
        var state = sm.AddState(name);
        state.motion = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        return state;
    }

    // Canlar (yuxarı solda ürəklər) + jump-scare qırmızı işartı + oyun sonu qaralma
    static void Lives(Transform root)
    {
        var canvasGo = new GameObject("LivesUI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        canvasGo.transform.SetParent(root, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30; // CCTV tam ekranının üstündə
        var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;

        UnityEngine.UI.Image FullScreen(string name)
        {
            var img = Stretch(new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)), canvasGo.transform).GetComponent<UnityEngine.UI.Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            return img;
        }
        var lives = canvasGo.AddComponent<PlayerLives>();
        lives.flash = FullScreen("Flash");
        lives.fade = FullScreen("Fade");
        lives.hearts = UIText("Hearts", canvasGo.transform, new Vector2(0, 1), new Vector2(40, -30), 44, new Color(0.75f, 0.05f, 0.05f), TextAnchor.UpperLeft);
        lives.hearts.text = "♥ ♥ ♥";
    }

    // NavMesh: döşəmə, divarlar və mebelin collider-lərindən. Qapılar və oyunçu daxil edilmir.
    static void BakeNavMesh(Transform root)
    {
        var player = Object.FindAnyObjectByType<HorrorController>();
        if (player && !player.GetComponent<Unity.AI.Navigation.NavMeshModifier>())
            player.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;

        // Mebelin üstü yerimək olmaz: varlıq stolun / stulun üstündə peyda olmasın və onların üstündən keçməsin (yalnız döşəmə)
        foreach (Transform group in root)
        {
            if (group.GetComponent<Unity.AI.Navigation.NavMeshModifier>()) continue;
            var mod = group.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
        }

        var surface = root.gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        const string path = "Assets/Scenes/SampleScene_NavMesh.asset";
        surface.navMeshData.name = "SampleScene_NavMesh"; // asset adı fayl adı ilə eyni olsun
        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
        if (existing)
        {
            EditorUtility.CopySerialized(surface.navMeshData, existing);
            surface.navMeshData = existing;
        }
        else AssetDatabase.CreateAsset(surface.navMeshData, path);
        surface.AddData();
    }

    // Varlığın sabit spawn / gəzinti nöqtələri: hər otaqda 4 (dəhlizdə 6), açıq döşəmədə, mebeldən və divardan aralı,
    // dəhlizə tam yolu olan. Ad = otaq adı (Anomaly.roomName ilə eyni). NavMesh bişiriləndən sonra çağırılır.
    static void EnemySpawns(Transform root)
    {
        var rooms = new (string name, float xa, float xb, float za, float zb, int count)[]
        {
            ("LAB", 1, 15, -9, -1, 4), ("STORAGE", 23, 33, -9, -1, 4), ("REACTOR", 23, 33, -21, -11, 4),
            ("ARCHIVE", 1, 9, -19, -11, 4), ("SECURITY", 11, 17, -25, -19, 4), ("CORRIDOR", 19, 21, -25, -1, 6),
        };
        var go = new GameObject("EnemySpawns");
        go.transform.SetParent(root, false);
        var player = Object.FindAnyObjectByType<HorrorController>();
        var corridor = new Vector3(20, 0, -10);
        var points = new System.Collections.Generic.List<Transform>();

        foreach (var r in rooms)
        {
            var valid = new System.Collections.Generic.List<Vector3>();
            bool narrow = r.xb - r.xa < 3; // dəhliz: 2 m enində
            float margin = narrow ? 1f : 0.8f, minEdge = narrow ? 0.25f : 0.6f;
            for (float x = r.xa + margin; x <= r.xb - margin; x += 0.5f)
                for (float z = r.za + margin; z <= r.zb - margin; z += 0.5f)
                {
                    var p = new Vector3(x, 0, z);
                    if (!UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 0.3f, UnityEngine.AI.NavMesh.AllAreas) || hit.position.y > 0.4f) continue;
                    if (UnityEngine.AI.NavMesh.FindClosestEdge(hit.position, out var edge, UnityEngine.AI.NavMesh.AllAreas) && edge.distance < minEdge) continue;
                    var blockers = Physics.OverlapCapsule(p + Vector3.up * 0.6f, p + Vector3.up * 1.8f, narrow ? 0.4f : 0.5f, ~0, QueryTriggerInteraction.Ignore);
                    if (blockers.Any(c => !player || !c.transform.IsChildOf(player.transform))) continue;
                    var path = new UnityEngine.AI.NavMeshPath();
                    if (!UnityEngine.AI.NavMesh.CalculatePath(hit.position, corridor, UnityEngine.AI.NavMesh.AllAreas, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete) continue;
                    valid.Add(hit.position);
                }
            if (valid.Count == 0) { Debug.LogWarning("EnemySpawns: no valid point in " + r.name); continue; }

            // bir-birindən ən uzaq nöqtələr (otağa yayılsın)
            var chosen = new System.Collections.Generic.List<Vector3> { valid.OrderBy(v => Vector3.Distance(v, new Vector3((r.xa + r.xb) / 2, 0, (r.za + r.zb) / 2))).First() };
            while (chosen.Count < r.count && chosen.Count < valid.Count)
                chosen.Add(valid.OrderByDescending(v => chosen.Min(c => Vector3.Distance(v, c))).First());

            for (int i = 0; i < chosen.Count; i++)
            {
                var t = new GameObject(r.name).transform;
                t.SetParent(go.transform, false);
                t.position = chosen[i];
                points.Add(t);
            }
        }
        Object.FindAnyObjectByType<EnemyEntity>().spawnPoints = points.ToArray();
    }

    // Varlıq (entity qovluğu, Mixamo): Zombie Idle-ın bir kadrında donmuş poza, ~2 m, gövdəsi və başı kameraya çevrilib.
    static void Entity(EntityAnomaly anomaly, Transform lookAt, Vector3 pos, Material distortion)
    {
        var body = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/entity/Character_Monster_04.fbx"), anomaly.transform);
        var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/entity/Zombie Idle.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        idle.SampleAnimation(body, 0.5f);
        body.transform.localScale = Vector3.one * 0.4f; // model T-pozada ~6 m-dir
        var toCam = lookAt.position - pos;
        toCam.y = 0;
        body.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(toCam));

        var head = body.GetComponentsInChildren<Transform>().First(t => t.name == "mixamorig:Head");
        head.rotation = Quaternion.FromToRotation(head.forward, (lookAt.position - head.position).normalized) * head.rotation; // düz kameraya
        foreach (var r in body.GetComponentsInChildren<Renderer>()) r.enabled = false; // anomaliya başlayana qədər görünmür

        var haze = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        haze.name = "SpawnHaze";
        Object.DestroyImmediate(haze.GetComponent<Collider>());
        haze.transform.SetParent(anomaly.transform, false);
        haze.transform.position = pos + Vector3.up * 1.1f;
        haze.transform.localScale = Vector3.one * 2.4f;
        var hr = haze.GetComponent<Renderer>();
        hr.sharedMaterial = distortion;
        hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hr.enabled = false;

        anomaly.entity = body;
        anomaly.head = head;
        anomaly.haze = hr;
    }

    // Döşəmə saatı: gövdə + ayrıca fırlanan 3 əqrəb (Assets/CLOCK/Split, orijinal mesh-dən ayrılıb) + sönük qırmızı işıqlar.
    static Transform storageClock, clockHour, clockMinute, clockSecond;
    static Light[] clockLights;

    static void GrandfatherClock(Vector3 pos, float rotY)
    {
        var root = new GameObject("GrandfatherClock").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CLOCK/M_Clock.mat");
        ClockPart("Body", root, "Clock_Body", new Vector3(0, 0, -0.021f), mat).gameObject.AddComponent<BoxCollider>();
        var pivot = new Vector3(0, 1.7326f, 0.1266f); // siferblatın mərkəzi
        clockHour = ClockPart("HandHour", root, "Clock_HandHour", pivot, mat);
        clockMinute = ClockPart("HandMinute", root, "Clock_HandMinute", pivot, mat);
        clockSecond = ClockPart("HandSecond", root, "Clock_HandSecond", pivot, mat);

        clockLights = new Light[2];
        for (int i = 0; i < 2; i++)
        {
            var l = new GameObject("RedLight").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.localPosition = new Vector3(i == 0 ? -0.6f : 0.6f, 1.3f, 0.45f);
            l.type = LightType.Point;
            l.color = new Color(1f, 0.05f, 0.03f);
            l.range = 3.5f;
            l.intensity = 0;
            l.enabled = false;
            clockLights[i] = l;
        }
        storageClock = root;
    }

    static Transform ClockPart(string name, Transform root, string mesh, Vector3 localPos, Material mat)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(root, false);
        go.transform.localPosition = localPos;
        go.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/CLOCK/Split/{mesh}.asset");
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    // Döşəmədə uzanan yazı: hərflərin "yuxarısı" kameradan uzağa baxır ki, kameradan düz oxunsun. width = yazının eni (m).
    // stretch: kameradan uzağa doğru uzatma (perspektivdə sıxılmasın deyə, yol yazıları kimi).
    static TextMesh ScaryWriting(Transform parent, string text, Vector3 pos, Vector3 readFrom, float width, float stretch)
    {
        var tm = new GameObject(text).AddComponent<TextMesh>();
        tm.transform.SetParent(parent, false);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.fontSize = 120;
        tm.fontStyle = FontStyle.BoldAndItalic;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.1f;

        const string matPath = "Assets/Materials/M_ScaryText.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat) { mat = new Material(Shader.Find("Custom/ScaryText")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.mainTexture = font.material.mainTexture;
        var r = tm.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        tm.characterSize *= width / r.bounds.size.x; // hələ fırlanmayıb: en x boyuncadır
        var away = pos - readFrom;
        away.y = 0;
        tm.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Vector3.down, away.normalized)); // üzü yuxarı, hərflərin yuxarısı kameradan uzağa
        tm.transform.localScale = new Vector3(1, stretch, 1);
        r.enabled = false; // anomaliya başlayana qədər görünmür (editorda da)
        return tm;
    }

    // Təhlükəsizlik kameraları (camera qovluğu): hər otağın küncündə bir, Laboratoriyada iki (hər tərəfdə bir).
    // Hamısı RT_CCTV-yə çəkir; SecurityDesk yalnız seçiləni işə salır.
    static readonly System.Collections.Generic.List<Camera> cctv = new System.Collections.Generic.List<Camera>();
    static readonly System.Collections.Generic.List<string> cctvNames = new System.Collections.Generic.List<string>();
    static GameObject securityDesk, securityPc;

    static void SecurityCameras(Transform root)
    {
        Room(root, "SecurityCameras", 0, 0, 0, 0);
        cctv.Clear();
        cctvNames.Clear();
        const string rtPath = "Assets/Materials/RT_CCTV.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (!rt) { rt = new RenderTexture(640, 576, 16); AssetDatabase.CreateAsset(rt, rtPath); }

        // ad, künc (divarların kəsişməsi), kameranın baxdığı nöqtə
        Cctv("LAB WEST", new Vector3(1, 0, -9), new Vector3(8, 0.8f, -4), rt); // şimal küncündə kabel yolu var
        Cctv("LAB EAST", new Vector3(15, 0, -1), new Vector3(7, 0.8f, -6), rt, 2.0f); // kabel yolunun altında; cənub küncündə qapı görüntünü örtürdü
        Cctv("STORAGE", new Vector3(33, 0, -1), new Vector3(27, 0.8f, -6), rt);
        Cctv("REACTOR", new Vector3(23, 0, -21), new Vector3(29, 0.8f, -14), rt, 1.7f); // borulardan aşağı; şimalda hava kanalı görüntünü örtürdü
        Cctv("ARCHIVE", new Vector3(1, 0, -19), new Vector3(5, 0.8f, -14), rt);
        Cctv("SECURITY", new Vector3(17, 0, -25), new Vector3(13, 0.8f, -21.5f), rt);
    }

    static void Cctv(string name, Vector3 corner, Vector3 target, RenderTexture rt, float height = 2.2f)
    {
        var flat = target - corner;
        flat.y = 0;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapBuilder.ModelPath("Camera")), parent);
        model.name = "CCTV_" + name;
        model.transform.SetPositionAndRotation(corner + Vector3.up * height, Quaternion.LookRotation(flat)); // kronşteyn küncdə, linza otağa

        var cam = new GameObject("Lens").AddComponent<Camera>();
        cam.transform.SetParent(model.transform, false);
        cam.transform.localPosition = new Vector3(0, 0.4f, 0.6f);
        cam.transform.LookAt(target);
        cam.fieldOfView = 100; // geniş bucaq: kameranın öz divarları boyunca olanlar da görünsün
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 30;
        cam.targetTexture = rt;
        cam.enabled = false;
        cctv.Add(cam);
        cctvNames.Add(name);
    }

    // Monitorun ekranına kamera görüntüsü, üstünə yazı; masaya SecurityDesk (oturmaq + kamera dəyişmək).
    // Ekranın yeri kompüter modelinin daxilində ölçülüb (yerli koordinatlar).
    static void SecurityMonitor()
    {
        if (!securityDesk || !securityPc) return;
        var pc = securityPc.transform;
        Vector3 c = pc.TransformPoint(new Vector3(0.2277f, 0.0888f, 0.2893f));
        Vector3 n = pc.TransformDirection(new Vector3(-0.1812f, -0.9816f, 0.0604f)).normalized; // ekranın üzü
        var rot = Quaternion.LookRotation(-n, Vector3.up);

        var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "CCTV_Screen";
        Object.DestroyImmediate(screen.GetComponent<Collider>());
        screen.transform.SetParent(securityDesk.transform, true);
        screen.transform.SetPositionAndRotation(c + n * 0.012f, rot);
        screen.transform.localScale = new Vector3(0.30f, 0.26f, 1);
        const string matPath = "Assets/Materials/M_CCTV_Screen.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", cctv[0].targetTexture);
        mat.SetColor("_BaseColor", new Color(2.2f, 2.5f, 2.2f)); // qaranlıq otaqlar görünsün (gecə görüşü kimi)
        screen.GetComponent<Renderer>().sharedMaterial = mat;

        var label = new GameObject("CCTV_Label").AddComponent<TextMesh>();
        label.transform.SetParent(securityDesk.transform, true);
        label.transform.SetPositionAndRotation(c + n * 0.016f + screen.transform.up * 0.115f - screen.transform.right * 0.135f, rot);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        label.fontSize = 60;
        label.characterSize = 0.0035f;
        label.anchor = TextAnchor.UpperLeft;
        label.color = new Color(1f, 0.3f, 0.25f);

        var seat = new GameObject("SeatView").transform;
        seat.SetParent(securityDesk.transform, true);
        seat.SetPositionAndRotation(c + n * 0.25f, rot); // monitora yaxın; sonra tam ekran açılır

        var desk = securityDesk.AddComponent<SecurityDesk>();
        desk.cameras = cctv.ToArray();
        desk.cameraNames = cctvNames.ToArray();
        desk.label = label;
        desk.fullscreen = FullscreenCctv(securityDesk.transform.root, cctv[0].targetTexture, out desk.fullscreenLabel);
        desk.seatView = seat;
        desk.player = Object.FindAnyObjectByType<HorrorController>();
        var flash = desk.player ? desk.player.head.Find("Flashlight") : null;
        if (flash) desk.flashlight = flash.gameObject;
    }

    // Masada oturanda açılan tam ekran CCTV: qara fon, ortada görüntü (nisbət saxlanır), yuxarıda kamera adı, aşağıda ipucu.
    // UI effektdən sonra çəkildiyi üçün yazılar kəskin qalır.
    static GameObject FullscreenCctv(Transform root, RenderTexture rt, out UnityEngine.UI.Text label)
    {
        var canvasGo = new GameObject("CCTV_Fullscreen", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        canvasGo.transform.SetParent(root, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;

        var bg = Stretch(new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image)), canvasGo.transform);
        bg.GetComponent<UnityEngine.UI.Image>().color = Color.black;

        var feedGo = new GameObject("Feed", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
        var feed = Stretch(feedGo, canvasGo.transform);
        feedGo.GetComponent<UnityEngine.UI.RawImage>().texture = rt;
        const string matPath = "Assets/Materials/M_CCTV_UI.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat) { mat = new Material(Shader.Find("UI/Default")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetColor("_Color", new Color(2.2f, 2.5f, 2.2f, 1)); // qaranlıq otaqlar görünsün (monitordakı kimi)
        feedGo.GetComponent<UnityEngine.UI.RawImage>().material = mat;
        var fit = feedGo.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)rt.width / rt.height;

        label = UIText("Label", feed, new Vector2(0, 1), new Vector2(40, -30), 48, new Color(1f, 0.35f, 0.3f), TextAnchor.UpperLeft);
        UIText("Hint", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0, 30), 26, new Color(0.7f, 0.7f, 0.7f), TextAnchor.LowerCenter)
            .text = "A / D:  switch camera        S:  stand up";

        canvasGo.SetActive(false);
        return canvasGo;
    }

    static RectTransform Stretch(GameObject go, Transform parent)
    {
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static UnityEngine.UI.Text UIText(string name, Transform parent, Vector2 anchor, Vector2 pos, int size, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900, 80);
        var t = go.GetComponent<UnityEngine.UI.Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    // ---------- Köməkçilər ----------

    static void Room(Transform root, string name, float minX, float maxX, float minZ, float maxZ)
    {
        parent = new GameObject(name).transform;
        parent.SetParent(root);
        x0 = minX; x1 = maxX; z0 = minZ; z1 = maxZ;
    }

    static GameObject Spawn(string model, float rotY, float tilt)
    {
        var path = MapBuilder.ModelPath(model);
        if (path == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        go.transform.rotation = Quaternion.Euler(0, rotY, tilt) * go.transform.rotation; // FBX-in öz import fırlanması saxlanır
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            mf.gameObject.AddComponent<BoxCollider>(); // oyunçu mebelin içindən keçməsin (mesh ölçüsünə avtomatik uyğunlaşır)
        return go;
    }

    static Bounds B(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // Döşəmədə (və ya y hündürlüyündə) sərbəst: bounds mərkəzi (x, z)-yə, altı y-ə.
    static GameObject At(string model, float x, float z, float rotY, float y = 0, float tilt = 0, float scale = 1)
    {
        var go = Spawn(model, rotY, tilt);
        if (!go) return null;
        go.transform.localScale *= scale;
        var b = B(go);
        go.transform.position += new Vector3(x - b.center.x, y - b.min.y, z - b.center.z);
        return go;
    }

    // Divara söykə: ön tərəf otağa baxır, arxa divara toxunur. along = divar boyunca x (N/S) və ya z (E/W).
    static GameObject Wall(string model, Side side, float along, float y = 0)
    {
        const float gap = 0.02f;
        float rot = side == Side.N ? 180 : side == Side.S ? 0 : side == Side.W ? 90 : 270;
        var go = Spawn(model, rot, 0);
        if (!go) return null;
        var b = B(go);
        Vector3 d = side switch
        {
            Side.N => new Vector3(along - b.center.x, 0, z1 - gap - b.max.z),
            Side.S => new Vector3(along - b.center.x, 0, z0 + gap - b.min.z),
            Side.W => new Vector3(x0 + gap - b.min.x, 0, along - b.center.z),
            _ => new Vector3(x1 - gap - b.max.x, 0, along - b.center.z),
        };
        d.y = y - b.min.y;
        go.transform.position += d;
        return go;
    }

    // Divar boyunca yan-yana düzür: start = sıranın başlanğıcı (N/S üçün x, E/W üçün z, artan istiqamətdə).
    static void WallRow(Side side, float start, float y, params string[] models)
    {
        bool alongX = side == Side.N || side == Side.S;
        float pos = start;
        foreach (var m in models)
        {
            var go = Wall(m, side, 0, y);
            if (!go) continue;
            var b = B(go);
            float w = alongX ? b.size.x : b.size.z;
            go.transform.position += (alongX ? Vector3.right : Vector3.forward) * (pos + w / 2);
            pos += w + 0.02f;
        }
    }

    // Başqa obyektin üstünə qoy (masa, qutu, taburet). dx/dz onun mərkəzinə görə.
    static GameObject Top(GameObject on, string model, float dx, float dz, float rotY, float scale = 1)
    {
        if (!on) return null;
        var b = B(on);
        return At(model, b.center.x + dx, b.center.z + dz, rotY, b.max.y, 0, scale);
    }

    // Tavandan as: üstü tavana toxunur.
    static GameObject Ceil(string model, float x, float z, float rotY)
    {
        var go = Spawn(model, rotY, 0);
        if (!go) return null;
        var b = B(go);
        go.transform.position += new Vector3(x - b.center.x, CeilingY - 0.01f - b.max.y, z - b.center.z);
        return go;
    }

    static void Lamp(float x, float z, float intensity = 1.5f, Color? color = null, float y = 2.5f)
    {
        var l = new GameObject("PointLight").AddComponent<Light>();
        l.transform.SetParent(parent);
        l.transform.position = new Vector3(x, y, z);
        l.type = LightType.Point;
        l.range = 9;
        l.intensity = intensity;
        l.color = color ?? new Color(0.85f, 0.95f, 0.9f);
    }

    static string[] Repeat(string model, int n)
    {
        var a = new string[n];
        for (int i = 0; i < n; i++) a[i] = model;
        return a;
    }

    // Pakda reaktor modeli yoxdur: sadə silindrlərdən placeholder (sonra əsl modellə əvəz et).
    static void Reactor(float x, float z)
    {
        const string path = "Assets/Materials/M_Placeholder_Reactor.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.22f, 0.25f, 0.23f) };
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.35f);
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateAsset(mat, path);
        }
        Cylinder("PLACEHOLDER_Reactor_Base", x, z, 3.6f, 0.3f, 0, mat);
        Cylinder("PLACEHOLDER_Reactor_Core", x, z, 2.6f, 2.4f, 0.3f, mat);
        Cylinder("PLACEHOLDER_Reactor_Cap", x, z, 3.0f, 0.15f, 2.7f, mat);
    }

    static void Cylinder(string name, float x, float z, float diameter, float height, float bottom, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, bottom + height / 2, z);
        go.transform.localScale = new Vector3(diameter, height / 2, diameter);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // yastı silindrdə capsule kürəyə çevrilir
        go.AddComponent<MeshCollider>();
    }
}
