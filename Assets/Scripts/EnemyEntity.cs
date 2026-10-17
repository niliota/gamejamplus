using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Düşmən varlıq. Oyunun əvvəlində yoxdur. Hər səhvdə (skan alınmadı / vaxt bitdi) AnomalyManager Provoke() çağırır:
// aqressivlik +1 olur (sıfırlanmır), varlıq həmin otaqda oyunçudan uzaq tərəfdə peyda olur və arxanca düşür (Running Crawl).
// Oyunçu qaçıb gözdən itəndə (persistence saniyə görmədikdə) yox olur — növbəti səhvə qədər.
// Aqressivlik yüksəldikcə: təqib sürəti artır, izi daha gec buraxır; randomVisitLevel-dən sonra arabir özü gəlib gəzir.
// Qapıları özü açır. Tutanda: jump-scare, oyunçu bir can itirir, varlıq yox olur.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyEntity : MonoBehaviour
{
    public Animator animator;                  // "Idle" (Zombie Idle) və "Crawl" (Running Crawl) vəziyyətləri
    public Transform eyes;                     // başın sümüyü
    public Transform hips;                     // animasiyanın yerindən sürüşməsinin qarşısını almaq üçün
    public bool activeOnStart;                 // TEST: oyun başlayan kimi oyunçudan ən uzaq spawn nöqtəsində peyda olub gəzir
    public Transform[] spawnPoints;            // sabit spawn / gəzinti nöqtələri (Build Decor yaradır; ad = otaq adı, məs. "LAB")

    [Header("Görmə")]
    public float sightRange = 12f;
    public float sightAngle = 120f;            // dərəcə
    public float loseSightTime = 5f;           // 1-ci səviyyədə bu qədər görməsə yox olur

    [Header("Hərəkət")]
    public float wanderSpeed = 0.9f;           // m/s (oyunçu: yeriş 2, qaçış 3.8)
    public float chaseSpeed = 3.2f;            // 1-ci səviyyədə
    public float catchDistance = 1.1f;
    public float doorReach = 1.8f;             // qapını bu məsafədən açır
    public float spawnPause = 1.5f;            // peyda olanda bu qədər dayanıb baxır (oyunçu qaçmağa macal tapsın)

    [Header("Aqressivlik (hər səhv +1, sıfırlanmır)")]
    public int aggression;
    public float speedPerLevel = 0.3f;         // 3-cü səviyyədə oyunçunun qaçışı qədər (3.8)
    public float maxChaseSpeed = 4.6f;
    public float persistencePerLevel = 2f;     // hər səviyyə izi bu qədər saniyə gec buraxır
    public int randomVisitLevel = 3;           // bu səviyyədən sonra arabir özü gəlir
    public Vector2 randomVisitInterval = new Vector2(60, 120); // s (səviyyə artdıqca qısalır)
    public float roamTime = 45f;               // özü gələndə bu qədər gəzir, tapmasa gedir

    enum State { Hidden, Wander, Chase, Catching }
    State state = State.Hidden;
    public bool IsChasing => state == State.Chase && pause <= 0;

    float ChaseSpeed => Mathf.Min(chaseSpeed + Mathf.Max(aggression - 1, 0) * speedPerLevel, maxChaseSpeed);
    float Persistence => loseSightTime + Mathf.Max(aggression - 1, 0) * persistencePerLevel;

    NavMeshAgent agent;
    HorrorController player;
    PlayerLives lives;
    Door[] doors;
    Renderer[] body;
    Vector3 hipsBase, lastSeen;
    float unseenTime, idleTime, doorCheck, pause, roamLeft, visitTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        body = GetComponentsInChildren<Renderer>();
        hipsBase = hips.localPosition;
        Hide();
    }

    void Start()
    {
        player = FindAnyObjectByType<HorrorController>();
        lives = FindAnyObjectByType<PlayerLives>();
        doors = FindObjectsByType<Door>();
        if (activeOnStart) SpawnHere();
    }

    [ContextMenu("Spawn (test)")]
    void SpawnHere()
    {
        if (Appear(Farthest(null), 0)) { state = State.Wander; roamLeft = float.PositiveInfinity; }
    }

    // Səhv oldu: aqressivlik artır, varlıq həmin otağın sabit nöqtələrindən oyunçudan ən uzağında peyda olur
    // (artıq oyundadırsa oyunçunun yerini öyrənir) və təqib edir
    public void Provoke(string room)
    {
        aggression++;
        if (state == State.Catching) return;
        if (state == State.Hidden && !Appear(Farthest(room), spawnPause)) return;
        lastSeen = player ? player.transform.position : transform.position;
        unseenTime = 0;
        state = State.Chase;
    }

    bool Appear(Vector3 at, float wait)
    {
        if (!NavMesh.SamplePosition(at, out var hit, 0.5f, NavMesh.AllAreas)) return false;
        foreach (var r in body) r.enabled = true;
        transform.position = hit.position; // agent NavMesh üzərində aktivləşsin
        if (player) transform.rotation = Quaternion.LookRotation(Flat(player.transform.position - hit.position));
        agent.enabled = true;
        agent.Warp(hit.position);
        pause = wait;
        idleTime = 2f;
        unseenTime = 0;
        current = null;
        Play("Idle", 1f);
        return true;
    }

    // Otağın (room boşdursa hamısının) sabit nöqtələrindən oyunçudan ən uzağı
    Vector3 Farthest(string room)
    {
        Vector3 best = transform.position;
        float bestDist = -1;
        foreach (var p in spawnPoints)
        {
            if (!string.IsNullOrEmpty(room) && p.name != room) continue;
            float d = player ? Vector3.Distance(p.position, player.transform.position) : 0;
            if (d > bestDist) { bestDist = d; best = p.position; }
        }
        return best;
    }

    void Hide()
    {
        state = State.Hidden;
        foreach (var r in body) r.enabled = false;
        if (agent.enabled) agent.enabled = false;
    }

    void Update()
    {
        if (state == State.Hidden) { RandomVisit(); return; }
        if (state == State.Catching || !player) return;

        if (pause > 0) // yenicə peyda oldu: dayanıb oyunçuya baxır
        {
            pause -= Time.deltaTime;
            agent.isStopped = true;
            Face(player.transform.position);
            return;
        }
        agent.isStopped = false;

        bool sees = CanSeePlayer();
        if (sees) { lastSeen = player.transform.position; unseenTime = 0; state = State.Chase; }

        if (state == State.Chase)
        {
            agent.speed = ChaseSpeed;
            agent.SetDestination(lastSeen);
            bool arrived = !agent.pathPending && agent.remainingDistance < 0.5f;
            Play(arrived ? "Idle" : "Crawl", arrived ? 1f : 1.3f * ChaseSpeed / chaseSpeed);
            if (!sees && (unseenTime += Time.deltaTime) > Persistence) { Hide(); return; } // qaçdın: yox olur
            if (sees && FlatDistance(player.transform.position) < catchDistance)
                StartCoroutine(Catch());
        }
        else
        {
            if ((roamLeft -= Time.deltaTime) <= 0) { Hide(); return; } // özü gəlmişdi, tapmadı
            Wander();
        }

        OpenDoors();
    }

    // Yüksək aqressivlikdə arabir özü gəlir: oyunçudan uzaqda peyda olub gəzir
    void RandomVisit()
    {
        if (aggression < randomVisitLevel || !player || spawnPoints.Length == 0) return;
        if ((visitTimer -= Time.deltaTime) > 0) return;
        float faster = 1 + (aggression - randomVisitLevel) * 0.5f;
        visitTimer = Random.Range(randomVisitInterval.x, randomVisitInterval.y) / faster;

        for (int i = 0; i < 10; i++)
        {
            var p = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            if (Vector3.Distance(p, player.transform.position) < sightRange) continue;
            if (Appear(p, 0)) { state = State.Wander; roamLeft = roamTime; }
            return;
        }
    }

    void Wander()
    {
        agent.speed = wanderSpeed;
        if (agent.pathPending) return;
        if (agent.remainingDistance > 0.4f) { Play("Crawl", 0.5f); return; }

        Play("Idle", 1f); // çatdı: bir az dayan, sonra başqa nöqtəyə
        if ((idleTime -= Time.deltaTime) > 0) return;
        idleTime = Random.Range(2f, 5f);
        if (spawnPoints.Length > 0) agent.SetDestination(spawnPoints[Random.Range(0, spawnPoints.Length)].position);
    }

    bool CanSeePlayer()
    {
        Vector3 target = player.head.position;
        Vector3 to = target - eyes.position;
        if (to.magnitude > sightRange) return false;
        if (Vector3.Angle(transform.forward, Flat(to)) > sightAngle / 2) return false;
        // arada divar / bağlı qapı / mebel varmı (oyunçunun öz collider-i sayılmır)
        foreach (var hit in Physics.RaycastAll(eyes.position, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(player.transform) && !hit.collider.transform.IsChildOf(transform)) return false;
        return true;
    }

    void OpenDoors()
    {
        if ((doorCheck -= Time.deltaTime) > 0) return;
        doorCheck = 0.2f;
        foreach (var d in doors)
            if (d.IsClosed && Vector3.Distance(d.transform.position, transform.position) < doorReach) d.OpenAwayFrom(transform.position);
    }

    // Jump-scare: varlıq kameranın qarşısına gəlir, ekran qırmızı yanır, kamera titrəyir; sonra bir can gedir və varlıq yox olur
    IEnumerator Catch()
    {
        state = State.Catching;
        var desk = FindAnyObjectByType<SecurityDesk>();
        if (desk) desk.ForceStand();
        player.enabled = false;
        agent.enabled = false;

        var head = player.head;
        Vector3 flatFwd = Flat(head.forward).normalized;
        transform.SetPositionAndRotation(head.position + flatFwd * 0.75f - Vector3.up * 1.75f, Quaternion.LookRotation(-flatFwd));
        Play("Idle", 1f);
        if (lives) lives.Flash();

        Quaternion baseRot = head.localRotation;
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            head.localRotation = baseRot * Quaternion.Euler(Random.insideUnitSphere * 4f); // titrəmə
            yield return null;
        }
        head.localRotation = baseRot;

        Hide();
        player.enabled = true;
        if (lives) lives.Lose();
    }

    void LateUpdate()
    {
        // animasiya kökü yerindən sürüşməsin (hərəkəti NavMeshAgent edir)
        if (state != State.Hidden) hips.localPosition = new Vector3(hipsBase.x, hips.localPosition.y, hipsBase.z);
    }

    void Face(Vector3 p)
    {
        Vector3 d = Flat(p - transform.position);
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360 * Time.deltaTime);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    float FlatDistance(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(transform.position.x, transform.position.z));

    string current;
    void Play(string anim, float speed)
    {
        animator.speed = speed;
        if (current == anim) return;
        current = anim;
        animator.CrossFadeInFixedTime(anim, 0.25f);
    }
}
