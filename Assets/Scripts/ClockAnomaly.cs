using UnityEngine;

// Anomaliya: döşəmə saatı titrəyir, bir az havaya qalxır, əqrəbləri sürətlə tərsinə fırlanır,
// ətrafında qırmızı işıqlar yanıb-sönür. Normal vaxtda əqrəblər adi saat kimi irəli gedir.
public class ClockAnomaly : Anomaly
{
    public Transform clock;                        // saatın kökü
    public Transform hourHand, minuteHand, secondHand;
    public Light[] redLights;
    public float height = 0.35f;                   // nə qədər qalxır (m)
    public float riseTime = 2f;
    public float shake = 0.02f;                    // titrəmə (m)
    public float shakeAngle = 2.5f;                // titrəmə (dərəcə)
    public float reverseSpeed = 720f;              // anomaliyada saniyə əqrəbi tərsinə (dərəcə/s)
    public float lightIntensity = 3f;

    [System.NonSerialized] bool ready;
    Vector3 basePos;
    Quaternion baseRot, hourRot, minuteRot, secondRot;
    float amount, target, hourAngle, minuteAngle, secondAngle;

    public override Vector3 ScanPoint => clock.position + Vector3.up * 1.2f;
    public override void Begin() => target = 1;
    public override void End() => target = 0;

    void Update()
    {
        amount = Mathf.MoveTowards(amount, target, Time.deltaTime / riseTime);
        float e = amount * amount * (3 - 2 * amount);

        // Əqrəblər: normalda irəli (saniyə 6°/s), anomaliyada sürətlə geri. Müsbət bucaq öndən baxanda saat istiqamətidir.
        secondAngle += Mathf.Lerp(6f, -reverseSpeed, e) * Time.deltaTime;
        minuteAngle += Mathf.Lerp(0.1f, -reverseSpeed / 6f, e) * Time.deltaTime;
        hourAngle += Mathf.Lerp(0.0083f, -reverseSpeed / 36f, e) * Time.deltaTime;
        secondHand.localRotation = secondRot * Quaternion.AngleAxis(secondAngle, Vector3.forward);
        minuteHand.localRotation = minuteRot * Quaternion.AngleAxis(minuteAngle, Vector3.forward);
        hourHand.localRotation = hourRot * Quaternion.AngleAxis(hourAngle, Vector3.forward);

        Apply(e);
    }

    // e: 0 = normal (yerdə, işıqlar sönük), 1 = tam anomaliya
    public void Apply(float e)
    {
        if (!ready) Init();
        float t = Time.time;
        Vector3 n = new Vector3(Mathf.PerlinNoise(1.3f, t * 9), Mathf.PerlinNoise(5.1f, t * 9), Mathf.PerlinNoise(9.7f, t * 9)) * 2 - Vector3.one;
        float bob = Mathf.Sin(t * 1.3f) * 0.04f;
        clock.SetPositionAndRotation(
            basePos + (Vector3.up * (height + bob) + n * shake) * e,
            Quaternion.Euler(n * shakeAngle * e) * baseRot);

        for (int i = 0; i < redLights.Length; i++)
        {
            float flicker = Mathf.PerlinNoise(i * 3.3f, t * 6) > 0.35f ? 1 : 0.15f; // qırıq-qırıq yanıb-sönmə
            redLights[i].enabled = e > 0.01f;
            redLights[i].intensity = lightIntensity * e * flicker;
        }
    }

    void Init()
    {
        ready = true;
        basePos = clock.position;
        baseRot = clock.rotation;
        hourRot = hourHand.localRotation;
        minuteRot = minuteHand.localRotation;
        secondRot = secondHand.localRotation;
    }
}
