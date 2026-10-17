using UnityEngine;

// Bir anomaliya. AnomalyManager sırası gələndə Begin(), həll olunanda End() çağırır.
// Oyunçu yanına gəlib F basılı saxlayanda skan edilir, sonra konsolda miniGameScene (və miniGameLevel bölümü) açılır.
public abstract class Anomaly : MonoBehaviour
{
    public string roomName;            // məs. "LAB"
    public float startDelay = 20f;     // əvvəlki həll olunandan (birincisi üçün oyun başlayandan) neçə saniyə sonra başlasın
    public string miniGameScene;       // konsolda açılacaq səhnə (Build siyahısında olmalıdır); boşdursa skan anomaliyanı birbaşa düzəldir
    public int miniGameLevel;          // həmin səhnədə hansı bölüm (mini oyun GameBoyConsole.Level ilə oxuyur)

    public virtual Vector3 ScanPoint => transform.position; // skan üçün yaxınlaşılan nöqtə

    public abstract void Begin();
    public abstract void End();
}
