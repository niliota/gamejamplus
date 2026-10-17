using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Konsol yalnız anomaliya üçün açılır:
// aktiv anomaliyanın yanında (scanRadius içində və ona baxanda) F basılı saxla → skan effekti → konsol aşağıdan qalxır
// və həmin anomaliyanın mini oyun səhnəsi (miniGameScene / miniGameLevel bölümü) additive yüklənir.
// miniGameScene boşdursa konsol açılmır: skan bitən kimi anomaliya həll olunur.
// F və ya Esc: konsolu bağla (anomaliya həll olunmur). Mini oyun qalib gələndə GameBoyConsole.CompleteCurrent() çağırır.
// Konsol açıq olanda oyunçunun hərəkəti, baxışı (HorrorController) və fənər söndürülür.
public class GameBoyConsole : MonoBehaviour
{
    public GameObject ui;                       // kameranın qarşısındakı konsol (başlanğıcda bağlı)
    public HorrorController player;
    public GameObject flashlight;               // konsol açıq olanda sönür
    public AnomalyManager anomalies;
    public RenderTexture screenTexture;         // mini oyun kameralarının çəkdiyi RT (konsolun ekranında görünür)

    [Header("Skan (F basılı saxla)")]
    public float scanTime = 2.5f;               // saniyə
    public float scanRadius = 3.5f;             // anomaliyaya bu qədər yaxın olmalısan (m)
    public GameObject scanUI;
    public UnityEngine.UI.Image scanTint, scanFill;
    public RectTransform scanLine;
    public UnityEngine.UI.Text scanText;
    public UnityEngine.UI.Image scanIcon;       // anomaliyanın yanında görünən skan ikonu (yazısız ipucu)

    [Header("Animasiya")]
    public float duration = 0.45f;              // qalxma / enmə (s)
    public float drop = 0.35f;                  // gizli vəziyyətdə nə qədər aşağıda (m)
    public float tilt = 30f;                    // gizli vəziyyətdə əyilmə (dərəcə)

    public static int Level { get; private set; } // açılan bölüm (mini oyun oxuyur)
    static GameBoyConsole instance;

    bool open;
    float progress, scan;                       // progress: konsol animasiyası, scan: skan 0..1
    float failFlash;                            // skan alınmayanda qırmızı yanma (1 → 0)
    string loadedScene;
    Vector3 shownPos;
    Quaternion shownRot;
    Renderer screen;
    MaterialPropertyBlock block;
    AsyncOperation pending;

    void Start()
    {
        instance = this;
        if (!anomalies) anomalies = FindAnyObjectByType<AnomalyManager>(); // Tools > Build Decor onu yenidən yaradır
        shownPos = ui.transform.localPosition;
        shownRot = ui.transform.localRotation;
        foreach (var r in ui.GetComponentsInChildren<Renderer>(true)) if (r.name == "Screen") screen = r;
        block = new MaterialPropertyBlock();
        if (scanUI) scanUI.SetActive(false);
    }

    // Mini oyun qalib gələndə çağırır: anomaliya həll olunur, konsol bağlanır.
    public static void CompleteCurrent()
    {
        if (!instance || !instance.open) return;
        if (instance.anomalies) instance.anomalies.Resolve();
        instance.Close();
    }

    void Update()
    {
        var kb = Keyboard.current;
        bool busy = pending != null && !pending.isDone;

        if (open)
        {
            if (kb != null && !busy && (kb.fKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) Close();
        }
        else UpdateScan(kb, busy);

        Animate();
    }

    void UpdateScan(Keyboard kb, bool busy)
    {
        var target = anomalies ? anomalies.Current : null;
        bool inRange = target && !busy && player.enabled && InRange(target);
        failFlash = Mathf.MoveTowards(failFlash, 0, Time.deltaTime);
        bool holding = inRange && failFlash == 0 && kb != null && kb.fKey.isPressed;
        scan = holding ? scan + Time.deltaTime / scanTime : Mathf.MoveTowards(scan, 0, Time.deltaTime * 2);

        if (scanUI)
        {
            scanUI.SetActive(scan > 0 || failFlash > 0); // yalnız skan zamanı: göstərici və effekt, yazı yoxdur
            scanFill.fillAmount = scan;
            scanTint.color = failFlash > 0 ? new Color(1f, 0.1f, 0.05f, 0.35f * failFlash) // skan alınmadı: qırmızı
                                           : new Color(0.3f, 1f, 0.5f, 0.25f * scan);
            scanLine.anchorMin = scanLine.anchorMax = new Vector2(0.5f, 1 - Mathf.Repeat(Time.time * 1.2f, 1));
            if (scanText) scanText.enabled = false;
        }
        if (scanIcon) // skan ediləcək bir şey var: yumşaq yanıb-sönən ikon; skan başlayanda gizlənir
        {
            scanIcon.enabled = inRange && scan == 0;
            scanIcon.color = new Color(0.5f, 1f, 0.65f, 0.55f + 0.3f * Mathf.Sin(Time.time * 4));
        }

        if (scan >= 1)
        {
            scan = 0;
            if (anomalies.ScanFails()) { failFlash = 1; anomalies.Fail(); } // anomaliya qalır, yenidən skan etmək olar
            else if (string.IsNullOrEmpty(target.miniGameScene)) anomalies.Resolve(); // mini oyun yoxdur: skan anomaliyanı düzəldir
            else Open(target);
        }
    }

    bool InRange(Anomaly a)
    {
        Vector3 to = a.ScanPoint - player.head.position;
        return to.magnitude <= scanRadius && Vector3.Dot(player.head.forward, to.normalized) > 0.6f;
    }

    void Open(Anomaly a)
    {
        open = true;
        if (scanUI) scanUI.SetActive(false);
        if (scanIcon) scanIcon.enabled = false;
        ui.SetActive(true);
        player.enabled = false;
        if (flashlight) flashlight.SetActive(false);

        Level = a.miniGameLevel;
        loadedScene = string.IsNullOrEmpty(a.miniGameScene) ? null : a.miniGameScene;
        SetScreen(loadedScene != null);
        if (loadedScene != null) pending = SceneManager.LoadSceneAsync(loadedScene, LoadSceneMode.Additive);
    }

    void Close()
    {
        open = false;
        player.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        if (loadedScene != null) pending = SceneManager.UnloadSceneAsync(loadedScene);
        loadedScene = null;
    }

    // Ekran: mini oyun varsa RenderTexture, yoxdursa sönülü (tünd)
    void SetScreen(bool on)
    {
        if (!screen) return;
        screen.GetPropertyBlock(block);
        if (on) block.SetTexture("_BaseMap", screenTexture);
        block.SetColor("_BaseColor", on ? Color.white : new Color(0.04f, 0.05f, 0.045f));
        screen.SetPropertyBlock(block);
    }

    // Konsolun qalxması / enməsi
    void Animate()
    {
        float target = open ? 1 : 0;
        if (progress == target) return;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);
        float t = progress * progress * progress * (progress * (6 * progress - 15) + 10); // smootherstep
        ui.transform.localPosition = shownPos + Vector3.down * drop * (1 - t);
        ui.transform.localRotation = Quaternion.Euler(tilt * (1 - t), 0, 0) * shownRot;

        if (progress == 0)
        {
            ui.SetActive(false);
            if (flashlight) flashlight.SetActive(true); // konsol tam enəndən sonra
        }
    }
}
