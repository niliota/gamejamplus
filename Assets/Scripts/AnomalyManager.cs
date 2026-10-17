using UnityEngine;

// Anomaliyaları sırayla işə salır. Eyni anda yalnız biri aktivdir:
// birincisi oyun başlayandan, hər növbətisi əvvəlki həll olunandan (Resolve) öz startDelay saniyəsi sonra başlayır.
// Aktiv anomaliya öz timeLimit müddətində düzəldilməsə varlıq (EnemyEntity) həmin otaqda spawn olur.
public class AnomalyManager : MonoBehaviour
{
    public Anomaly[] sequence;
    public EnemyEntity enemy;

    [Header("Test")]
    public bool randomScanFail = true;              // skan təsadüfi alınmaya bilər
    [Range(0, 1)] public float scanFailChance = 0.5f;

    int index = -1;
    float timer, elapsed;
    bool active;

    public Anomaly Current => active ? sequence[index] : null;

    void Start()
    {
        timer = sequence.Length > 0 ? sequence[0].startDelay : 0;
        if (!enemy) enemy = FindAnyObjectByType<EnemyEntity>();
    }

    void Update()
    {
        if (active)
        {
            elapsed += Time.deltaTime;
            if (elapsed > sequence[index].timeLimit) Fail(); // vaxtında düzəldilmədi (hər timeLimit-dən bir təkrarlanır)
            return;
        }
        if (index >= sequence.Length - 1) return;
        timer -= Time.deltaTime;
        if (timer > 0) return;
        index++;
        active = true;
        elapsed = 0;
        sequence[index].Begin();
        if (GameAudio.I) GameAudio.I.AnomalyStarted(sequence[index]);
    }

    public bool ScanFails() => randomScanFail && Random.value < scanFailChance;

    // Səhv: skan alınmadı və ya vaxt bitdi. Anomaliya aktiv qalır; varlıq daha aqressiv olur və həmin otaqda peyda olur.
    [ContextMenu("Fail current")]
    public void Fail()
    {
        if (!active) return;
        elapsed = 0;
        if (enemy) enemy.Provoke(sequence[index].roomName);
    }

    // Skan / mini oyun uğurla bitəndə çağırılır. Test üçün: Inspector-da komponentin menyusundan "Resolve current".
    [ContextMenu("Resolve current")]
    public void Resolve()
    {
        if (!active) return;
        sequence[index].End();
        active = false;
        if (GameAudio.I) GameAudio.I.AnomalyEnded();
        if (index + 1 < sequence.Length) timer = sequence[index + 1].startDelay;
        else if (GameAudio.I) GameAudio.I.Win(); // sonuncu da düzəldildi
    }
}
