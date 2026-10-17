using UnityEngine;

// Anomaliyaları sırayla işə salır. Eyni anda yalnız biri aktivdir:
// birincisi oyun başlayandan, hər növbətisi əvvəlki həll olunandan (Resolve) öz startDelay saniyəsi sonra başlayır.
public class AnomalyManager : MonoBehaviour
{
    public Anomaly[] sequence;

    int index = -1;
    float timer;
    bool active;

    public Anomaly Current => active ? sequence[index] : null;

    void Start() => timer = sequence.Length > 0 ? sequence[0].startDelay : 0;

    void Update()
    {
        if (active || index >= sequence.Length - 1) return;
        timer -= Time.deltaTime;
        if (timer > 0) return;
        index++;
        active = true;
        sequence[index].Begin();
    }

    // Mini oyun uğurla bitəndə çağırılır. Test üçün: Inspector-da komponentin menyusundan "Resolve current".
    [ContextMenu("Resolve current")]
    public void Resolve()
    {
        if (!active) return;
        sequence[index].End();
        active = false;
        if (index + 1 < sequence.Length) timer = sequence[index + 1].startDelay;
    }
}
