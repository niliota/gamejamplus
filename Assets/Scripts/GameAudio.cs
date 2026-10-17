using System.Collections;
using UnityEngine;

// Oyunun bütün səsləri (Assets/sounds):
// - qapı açılma / bağlanma (3D, qapıdan)
// - anomaliyanın öz səsi (Anomaly.loopSound, 3D, aktiv olduğu müddətdə dövrə ilə)
// - CCTV-də aktiv anomaliyanı görəndə xəbərdarlıq (hər anomaliya üçün bir dəfə, faylın yalnız cctvStart–cctvEnd hissəsi)
// - təsadüfi qorxulu səslər (oyunçunun ətrafında) və arabir qorxu musiqisi
// - varlıq təqib edəndə musiqi (yavaş-yavaş gəlir / sönür)
// - oyun sonu: qalib → tam, uduzanda → son siqnal (failCut-dan sonrası) çalınmır
public class GameAudio : MonoBehaviour
{
    public static GameAudio I;

    public AudioClip doorOpen, doorClose;

    [Header("Anomaliya")]
    public AudioClip cctvAlert;
    public float cctvStart = 1.4f, cctvEnd = 7f;   // faylın bu hissəsi çalınır (s)
    public float anomalyVolume = 0.8f;

    [Header("Atmosfer")]
    public AudioClip[] creepy;
    public Vector2 creepyInterval = new Vector2(40, 90);
    public AudioClip horrorMusic;
    public Vector2 musicInterval = new Vector2(180, 240);
    public float musicVolume = 0.45f;

    [Header("Təqib")]
    public AudioClip chase;
    public float chaseVolume = 0.6f, chaseFadeIn = 1f, chaseFadeOut = 3f;

    [Header("Oyun sonu")]
    public AudioClip endMusic;
    public float failCut = 1.9f;                    // uduzanda bu saniyədə kəsilir (son siqnaldan əvvəl)

    AudioSource music, chaseSrc, ui, anomalySrc;
    Transform listener;
    EnemyEntity enemy;
    AnomalyManager anomalies;
    Anomaly alerted;                                // xəbərdarlığı artıq çalınmış anomaliya
    float creepyTimer, musicTimer, anomalyTarget;
    bool ended;

    void Awake()
    {
        I = this;
        music = Source2D(false);
        chaseSrc = Source2D(true);
        chaseSrc.clip = chase;
        ui = Source2D(false);
        anomalySrc = gameObject.AddComponent<AudioSource>();
        anomalySrc.playOnAwake = false;
        anomalySrc.loop = true;
        Set3D(anomalySrc, 2f, 18f);
    }

    void Start()
    {
        var player = FindAnyObjectByType<HorrorController>();
        if (player) listener = player.transform;
        enemy = FindAnyObjectByType<EnemyEntity>();
        anomalies = FindAnyObjectByType<AnomalyManager>();
        creepyTimer = Random.Range(creepyInterval.x, creepyInterval.y);
        musicTimer = Random.Range(musicInterval.x, musicInterval.y) * 0.5f;
    }

    AudioSource Source2D(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0;
        return s;
    }

    static void Set3D(AudioSource s, float min, float max)
    {
        s.spatialBlend = 1;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = min;
        s.maxDistance = max;
        s.dopplerLevel = 0;
    }

    // Bir dəfəlik 3D səs (qapılar, qorxulu səslər)
    public static void PlayAt(AudioClip clip, Vector3 pos, float volume = 1f, float maxDistance = 15f)
    {
        if (!clip) return;
        var go = new GameObject("Sfx " + clip.name);
        go.transform.position = pos;
        var s = go.AddComponent<AudioSource>();
        Set3D(s, 1f, maxDistance);
        s.clip = clip;
        s.volume = volume;
        s.Play();
        Destroy(go, clip.length + 0.1f);
    }

    public static void Door(Vector3 pos, bool opening)
    {
        if (I) PlayAt(opening ? I.doorOpen : I.doorClose, pos, 0.8f, 12f);
    }

    void Update()
    {
        // anomaliyanın səsi yumşaq gəlir / sönür
        if (anomalySrc.isPlaying)
        {
            anomalySrc.volume = Mathf.MoveTowards(anomalySrc.volume, anomalyTarget, Time.deltaTime / 2.5f);
            if (anomalyTarget == 0 && anomalySrc.volume == 0) anomalySrc.Stop();
        }

        // təqib musiqisi
        bool chasing = !ended && enemy && enemy.IsChasing;
        if (chasing && !chaseSrc.isPlaying && chase) { chaseSrc.volume = 0; chaseSrc.Play(); }
        if (chaseSrc.isPlaying)
        {
            float target = chasing ? chaseVolume : 0;
            chaseSrc.volume = Mathf.MoveTowards(chaseSrc.volume, target, Time.deltaTime * chaseVolume / (chasing ? chaseFadeIn : chaseFadeOut));
            if (!chasing && chaseSrc.volume == 0) chaseSrc.Stop();
            if (chasing && music.isPlaying) music.volume = Mathf.MoveTowards(music.volume, 0, Time.deltaTime);
        }
        if (ended || !listener) return;

        // təsadüfi qorxulu səs: oyunçudan 6–12 m aralıda təsadüfi istiqamətdə
        if ((creepyTimer -= Time.deltaTime) <= 0 && creepy.Length > 0)
        {
            creepyTimer = Random.Range(creepyInterval.x, creepyInterval.y);
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(6f, 12f);
            PlayAt(creepy[Random.Range(0, creepy.Length)], listener.position + new Vector3(dir.x, 1.5f, dir.y), 0.8f, 20f);
        }

        // arabir qorxu musiqisi (təqib zamanı yox)
        if (!music.isPlaying && !chaseSrc.isPlaying && horrorMusic && (musicTimer -= Time.deltaTime) <= 0)
        {
            musicTimer = Random.Range(musicInterval.x, musicInterval.y);
            music.clip = horrorMusic;
            music.volume = musicVolume;
            music.Play();
        }
    }

    // AnomalyManager çağırır
    public void AnomalyStarted(Anomaly a)
    {
        if (!a.loopSound) return;
        anomalySrc.transform.position = a.ScanPoint;
        anomalySrc.clip = a.loopSound;
        anomalySrc.time = Random.Range(0, a.loopSound.length * 0.8f);
        anomalySrc.volume = 0;
        anomalyTarget = anomalyVolume;
        anomalySrc.Play();
    }

    public void AnomalyEnded() => anomalyTarget = 0;

    // SecurityDesk çağırır: tam ekranda hansı kamera göstərilir
    public void CameraViewed(int index)
    {
        var a = anomalies ? anomalies.Current : null;
        if (!a || a == alerted || a.cctvIndex != index || !cctvAlert) return;
        alerted = a;
        StartCoroutine(PlayPart(ui, cctvAlert, cctvStart, cctvEnd, 1f, 0.4f));
    }

    public void Win() => End(endMusic ? endMusic.length : 0);
    public void Fail() => End(failCut);

    void End(float until)
    {
        ended = true;
        StopAllCoroutines();
        music.Stop();
        anomalyTarget = 0;
        StartCoroutine(PlayPart(music, endMusic, 0, until, 1f, 0.05f));
    }

    // Klipin [from, to] hissəsini çalır, sonunda qısa sönmə
    static IEnumerator PlayPart(AudioSource s, AudioClip clip, float from, float to, float volume, float fade)
    {
        if (!clip) yield break;
        s.clip = clip;
        s.volume = volume;
        s.time = from;
        s.Play();
        bool full = to >= clip.length;
        while (s.isPlaying && s.time < to)
        {
            if (!full) s.volume = volume * Mathf.Clamp01((to - s.time) / fade);
            yield return null;
        }
        if (!full) s.Stop();
    }
}
