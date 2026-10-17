using UnityEngine;

// Bir anomaliya. AnomalyManager sırası gələndə Begin(), həll olunanda End() çağırır.
// Oyunçu yanına gəlib F basılı saxlayanda skan edilir, sonra konsolda miniGameScene (və miniGameLevel bölümü) açılır.
public abstract class Anomaly : MonoBehaviour
{
    public string roomName;            // məs. "LAB"
    public float startDelay = 20f;     // əvvəlki həll olunandan (birincisi üçün oyun başlayandan) neçə saniyə sonra başlasın
    public float timeLimit = 90f;      // bu müddətdə düzəldilməsə varlıq (EnemyEntity) bu otaqda spawn olur
    public string miniGameScene;       // konsolda açılacaq səhnə (Build siyahısında olmalıdır); boşdursa skan anomaliyanı birbaşa düzəldir
    public int miniGameLevel;          // həmin səhnədə hansı bölüm (mini oyun GameBoyConsole.Level ilə oxuyur)
    public int cctvIndex = -1;         // hansı CCTV kamerasında görünür (mühafizə masasında ona keçəndə xəbərdarlıq səsi)
    public AudioClip loopSound;        // aktiv olduğu müddətdə anomaliyanın yerindən dövrə ilə çalınır (boş ola bilər)

    public virtual Vector3 ScanPoint => transform.position; // skan üçün yaxınlaşılan nöqtə

    public abstract void Begin();
    public abstract void End();
}
