using UnityEngine;

// Anomaliya: döşəmədə qorxulu yazı yavaş-yavaş peyda olur (Custom/ScaryText); End() çağırılanda yavaş-yavaş itir.
public class FloorWritingAnomaly : Anomaly
{
    public TextMesh writing;
    public float fadeTime = 3f;

    float amount, target;
    MaterialPropertyBlock block;
    Renderer rend;

    public override Vector3 ScanPoint => writing.transform.position;

    public override void Begin() => target = 1;
    public override void End() => target = 0;

    void Start() => Apply(0);

    void Update()
    {
        if (amount == target) return;
        amount = Mathf.MoveTowards(amount, target, Time.deltaTime / fadeTime);
        Apply(amount);
    }

    // e: 0 = görünmür, 1 = tam görünür
    public void Apply(float e)
    {
        if (!rend)
        {
            rend = writing.GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }
        rend.enabled = e > 0.001f;
        rend.GetPropertyBlock(block);
        block.SetFloat("_Fade", e);
        block.SetTexture("_MainTex", writing.font.material.mainTexture); // dinamik şrift atlası
        rend.SetPropertyBlock(block);
    }
}
