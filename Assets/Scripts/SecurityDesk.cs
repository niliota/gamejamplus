using UnityEngine;
using UnityEngine.InputSystem;

// Mühafizə masası: masaya və ya kompüterə klikləyəndə oturursan, kamera monitora yaxınlaşır
// və təhlükəsizlik kameraları tam ekranda açılır (ətraf görünmür).
// A / D (← / →): kamera dəyiş, S və ya Esc: qalx.
// Kameraların hamısı eyni RenderTexture-a çəkir; yalnız seçilən kamera işləyir.
public class SecurityDesk : MonoBehaviour
{
    public Camera[] cameras;
    public string[] cameraNames;
    public TextMesh label;                  // monitordakı "CAM 1  LAB" yazısı
    public GameObject fullscreen;           // tam ekran CCTV (UI), oturanda açılır
    public UnityEngine.UI.Text fullscreenLabel;
    public Transform seatView;              // oturanda başın (kameranın) yeri və istiqaməti
    public HorrorController player;
    public GameObject flashlight;           // oturanda sönür
    public float moveTime = 0.6f;
    [Range(0, 1)] public float effectReduction = 0.5f; // tam ekranda LethalLook nə qədər zəifləsin (1 = piksel/dither tam yox)
    public float effectFadeTime = 0.5f;                // zəifləmə tam ekran açılandan sonra bu müddətdə yavaş-yavaş olur

    int current;
    bool seated;
    float t = 1, weaken, weakenTarget;
    Vector3 standPos, fromPos, toPos;
    Quaternion standRot, fromRot, toRot;
    static readonly int LethalLookWeaken = Shader.PropertyToID("_LethalLookWeaken");

    void Start()
    {
        if (fullscreen) fullscreen.SetActive(false);
        Show(0);
    }

    void OnDisable() => Shader.SetGlobalFloat(LethalLookWeaken, 0); // oturarkən Play dayansa effekt zəif qalmasın

    void Show(int i)
    {
        current = (i + cameras.Length) % cameras.Length;
        for (int k = 0; k < cameras.Length; k++) cameras[k].enabled = k == current;
        string text = $"CAM {current + 1}  {cameraNames[current]}";
        if (label) label.text = text;
        if (fullscreenLabel) fullscreenLabel.text = text;
        if (seated && t >= 1 && GameAudio.I) GameAudio.I.CameraViewed(current);
    }

    void Interact() // HorrorController klikləyəndə çağırır
    {
        if (seated || t < 1) return;
        seated = true;
        player.enabled = false;
        if (flashlight) flashlight.SetActive(false);
        standPos = player.head.position;
        standRot = player.head.rotation;
        Move(seatView.position, seatView.rotation);
    }

    // Varlıq tutanda: dərhal ayağa qalx (tam ekran bağlanır, baş yerinə qayıdır)
    public void ForceStand()
    {
        if (!seated) return;
        seated = false;
        t = 1;
        if (fullscreen) fullscreen.SetActive(false);
        weaken = weakenTarget = 0;
        Shader.SetGlobalFloat(LethalLookWeaken, 0);
        player.head.SetPositionAndRotation(standPos, standRot);
        if (flashlight) flashlight.SetActive(true);
        player.enabled = true;
    }

    void Move(Vector3 pos, Quaternion rot)
    {
        fromPos = player.head.position;
        fromRot = player.head.rotation;
        toPos = pos;
        toRot = rot;
        t = 0;
    }

    void Update()
    {
        // Effektin zəifləməsi yalnız tam ekran örtəndə və yumşaq (keçid görünməsin)
        if (weaken != weakenTarget)
        {
            weaken = Mathf.MoveTowards(weaken, weakenTarget, Time.deltaTime / effectFadeTime);
            Shader.SetGlobalFloat(LethalLookWeaken, weaken);
        }

        if (t < 1) // oturma / qalxma hərəkəti
        {
            t = Mathf.Min(1, t + Time.deltaTime / moveTime);
            float s = t * t * (3 - 2 * t);
            player.head.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
            if (t >= 1)
            {
                if (seated) // monitora çatdıq: tam ekran, sonra effekt yavaş-yavaş zəifləyir
                {
                    if (fullscreen) fullscreen.SetActive(true);
                    weakenTarget = effectReduction;
                    if (GameAudio.I) GameAudio.I.CameraViewed(current);
                }
                else
                {
                    player.enabled = true;
                    if (flashlight) flashlight.SetActive(true);
                }
            }
            return;
        }
        if (!seated) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Show(current + 1);
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Show(current - 1);
        else if (kb.sKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
        {
            seated = false;
            if (fullscreen) fullscreen.SetActive(false);
            weaken = weakenTarget = 0; // tam ekran bağlanan anda effekt normaldır
            Shader.SetGlobalFloat(LethalLookWeaken, 0);
            Move(standPos, standRot);
        }
    }
}
