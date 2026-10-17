using UnityEngine;

// Son anomaliya: varlıq (entity) qısa, qırıq-qırıq yanıb-sönmə və bulanıq titrəşmə ilə peyda olur,
// sonra tərpənmədən dayanıb kameraya baxır. Pozası və baxışı builder-də dondurulub (Animator yoxdur).
// End() çağırılanda eyni effektlə yox olur.
public class EntityAnomaly : Anomaly
{
    public GameObject entity;
    public Transform head;
    public Renderer haze;                  // Custom/AnomalyDistortion kürəsi
    public float effectTime = 1.2f;        // peyda olma / yox olma effekti (s)

    float t = 1;
    bool visible;
    Renderer[] body;
    MaterialPropertyBlock block;

    public override Vector3 ScanPoint => head.position;
    public override void Begin() { visible = true; t = 0; }
    public override void End() { visible = false; t = 0; }

    void Start() => Apply(0, false);

    void Update()
    {
        if (t >= 1) return;
        t = Mathf.Min(1, t + Time.deltaTime / effectTime);
        Apply(t, visible);
    }

    // p: effektin gedişi (0..1), show: sonda görünsünmü
    public void Apply(float p, bool show)
    {
        if (body == null)
        {
            body = entity.GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }
        // effektin ilk yarısında qırıq-qırıq yanıb-sönür, sonra son vəziyyətdə qalır
        bool on = p < 0.5f && p > 0 ? Mathf.PerlinNoise(p * 40, 0.5f) > 0.45f : show;
        foreach (var r in body) r.enabled = on;

        float h = Mathf.Sin(p * Mathf.PI); // titrəşmə əvvəl güclənir, sonra sönür
        haze.enabled = h > 0.01f;
        block.SetFloat("_Fade", h);
        haze.SetPropertyBlock(block);
    }
}
