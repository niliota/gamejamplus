using UnityEngine;

// Qapı qanadı (menteşə pivotunun üstündə). Toggle() açır / bağlayır, qanad yumşaq fırlanır.
// Həmişə oyunçudan uzağa (baxdığı tərəfdən qarşıya) açılır.
// Açıq qapıdan keçib qarşı tərəfdə closeDistance qədər uzaqlaşanda qapı öz-özünə bağlanır.
// angle: hazırkı açılma bucağı, openAngle: tam açıq bucaq (yalnız böyüklüyü istifadə olunur).
public class Door : MonoBehaviour
{
    public float angle;
    public float openAngle = 90f;
    public float duration = 0.6f;       // açılma / bağlanma vaxtı (s)
    public float closeDistance = 1.4f;  // keçəndən sonra qapı müstəvisindən bu qədər uzaqda bağlanır (qanad sənə dəyməsin)

    float from, target, t = 1, openedSide;
    Quaternion closedRot;
    Vector3 normal;                     // bağlı qapının müstəvisinə perpendikulyar
    Transform player;

    void Awake()
    {
        closedRot = transform.localRotation * Quaternion.Euler(0, -angle, 0);
        normal = transform.rotation * Quaternion.Euler(0, -angle, 0) * Vector3.forward;
        target = angle;
        var p = FindAnyObjectByType<HorrorController>();
        if (p) player = p.transform;
    }

    float Side() => player ? Vector3.Dot(player.position - transform.position, normal) : 0;

    void Interact() => Toggle(); // HorrorController klikləyəndə çağırır

    public bool IsClosed => Mathf.Abs(target) < 1f;

    // Varlıq üçün: bağlıdırsa "from" nöqtəsindən uzağa açılır və açıq qalır (avtomatik bağlanmır)
    public void OpenAwayFrom(Vector3 from)
    {
        if (!IsClosed) return;
        from = from - transform.position;
        float side = Vector3.Dot(from, normal);
        this.from = angle;
        target = (side < 0 ? -1 : 1) * Mathf.Abs(openAngle);
        openedSide = 0;
        t = 0;
        GameAudio.Door(transform.position, true);
    }

    public void Toggle()
    {
        from = angle;
        if (Mathf.Abs(target) > 1f) target = 0;          // açıqdırsa bağla
        else
        {
            openedSide = Side();                         // hansı tərəfdən açdığını yadda saxla
            // Səndən uzağa açılır: +Y fırlanma qanadı normalın əks tərəfinə aparır
            target = (openedSide < 0 ? -1 : 1) * Mathf.Abs(openAngle);
        }
        t = 0;
        GameAudio.Door(transform.position, target != 0);
    }

    void Update()
    {
        // Keçdisə (tərəf dəyişdi və kifayət qədər uzaqlaşdı) arxanca bağla
        if (target != 0 && t >= 1 && openedSide != 0)
        {
            float s = Side();
            if (Mathf.Sign(s) != Mathf.Sign(openedSide) && Mathf.Abs(s) > closeDistance) Toggle();
        }

        if (t >= 1) return;
        t = Mathf.Min(1, t + Time.deltaTime / duration);
        angle = Mathf.Lerp(from, target, t * t * (3 - 2 * t)); // yumşaq başlayıb dayanır
        transform.localRotation = closedRot * Quaternion.Euler(0, angle, 0);
    }
}
