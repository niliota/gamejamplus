using UnityEngine;

// Anomaliya: stullar yavaş-yavaş havaya qalxır, orada titrəyir; ətraflarında bulanıq, titrəşən hava olur.
// End() çağırılanda yumşaq yerinə enir.
public class FloatingChairsAnomaly : Anomaly
{
    public Transform[] chairs;
    public Material distortion;          // Custom/AnomalyDistortion
    public float height = 0.7f;          // nə qədər qalxır (m)
    public float riseTime = 2.5f;        // qalxma / enmə müddəti (s)
    public float shake = 0.025f;         // titrəmə (m)
    public float shakeAngle = 4f;        // titrəmə (dərəcə)
    public float hazeSize = 1.5f;        // bulanıq kürənin diametri (m)

    [System.NonSerialized] Vector3[] basePos; // skript yenidən yüklənəndə boş massiv kimi qalmasın
    [System.NonSerialized] Quaternion[] baseRot;
    [System.NonSerialized] Renderer[] hazes;
    MaterialPropertyBlock block;
    float amount, target;                // 0 = yerdə, 1 = havada

    public override void Begin() => target = 1;
    public override void End() => target = 0;

    public override Vector3 ScanPoint // stulların ortası
    {
        get
        {
            Vector3 sum = Vector3.zero;
            foreach (var c in chairs) sum += c.position;
            return sum / chairs.Length + Vector3.up;
        }
    }

    void Update()
    {
        if (amount == 0 && target == 0) return;
        amount = Mathf.MoveTowards(amount, target, Time.deltaTime / riseTime);
        Apply(amount * amount * (3 - 2 * amount));
    }

    // e: 0 = yerdə (ilkin vəziyyət), 1 = tam havada
    public void Apply(float e)
    {
        if (basePos == null || basePos.Length != chairs.Length) Init();
        float t = Time.time;
        for (int i = 0; i < chairs.Length; i++)
        {
            float seed = i * 13.7f;
            Vector3 n = new Vector3(Mathf.PerlinNoise(seed, t * 9), Mathf.PerlinNoise(seed + 5, t * 9), Mathf.PerlinNoise(seed + 9, t * 9)) * 2 - Vector3.one;
            float bob = Mathf.Sin(t * 1.5f + i) * 0.05f;
            chairs[i].SetPositionAndRotation(
                basePos[i] + (Vector3.up * (height + bob) + n * shake) * e,
                Quaternion.Euler(n * shakeAngle * e) * baseRot[i]);

            hazes[i].enabled = e > 0.01f;
            block.SetFloat("_Fade", e);
            hazes[i].SetPropertyBlock(block);
        }
    }

    void Init()
    {
        block = new MaterialPropertyBlock();
        basePos = new Vector3[chairs.Length];
        baseRot = new Quaternion[chairs.Length];
        hazes = new Renderer[chairs.Length];
        for (int i = 0; i < chairs.Length; i++)
        {
            basePos[i] = chairs[i].position;
            baseRot[i] = chairs[i].rotation;

            var rs = chairs[i].GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var haze = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            haze.name = "AnomalyHaze";
            DestroyImmediate(haze.GetComponent<Collider>());
            haze.transform.SetParent(chairs[i], true);
            haze.transform.position = b.center;
            haze.transform.localScale = Vector3.one * hazeSize;
            hazes[i] = haze.GetComponent<Renderer>();
            hazes[i].sharedMaterial = distortion;
            hazes[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hazes[i].enabled = false;
        }
    }
}
