using System.Collections.Generic;
using UnityEngine;

// Mini-game for the floating chairs anomaly: chairs rise around the arena and get thrown at the player.
// Survive winTime seconds → anomaly is resolved. Getting hit restarts the round.
// Built by Tools > Build Chair MiniGame (Assets/TuralTo/ChairMiniGame.unity).
public class ChairDodgeGame : MonoBehaviour
{
    public TopDownPlayer player;
    public ThrownChair chairTemplate;   // inactive in the scene, cloned for every throw
    public Camera cam;
    public TextMesh timerText;

    public Rect spawnArea = new Rect(-5f, -3f, 10f, 4f);
    public Rect outside = new Rect(-10f, -7f, 20f, 14f);
    public Vector2 playerStart = new Vector2(0, -2f);
    public float winTime = 15f;
    public float spawnStart = 1.2f, spawnEnd = 0.5f;   // seconds between throws, start → end of round
    public float safeDistance = 4f;                    // chairs never appear closer than this
    public float hitRadius = 1f;
    public float hitPause = 1f;

    readonly List<ThrownChair> chairs = new List<ThrownChair>();
    float time, nextSpawn, pause;
    bool won;
    Vector3 camPos;

    void Start()
    {
        // Played on its own in the editor: draw to the Game view instead of the console's RenderTexture.
        if (!FindAnyObjectByType<GameBoyConsole>()) cam.targetTexture = null;
        camPos = cam.transform.localPosition;
        chairTemplate.gameObject.SetActive(false);
        ResetRound();
    }

    void ResetRound()
    {
        ClearChairs();
        time = 0;
        nextSpawn = 1f;
        player.ResetTo(playerStart);
        player.enabled = true;
    }

    void Update()
    {
        if (won) return;

        if (pause > 0)
        {
            pause -= Time.deltaTime;
            float shake = Mathf.Clamp01(pause - hitPause + 0.3f) * 0.5f;
            cam.transform.localPosition = camPos + (Vector3)Random.insideUnitCircle * shake;
            player.GetComponent<SpriteRenderer>().enabled = Mathf.Repeat(pause, 0.16f) > 0.08f;
            if (pause <= 0) ResetRound();
            return;
        }

        time += Time.deltaTime;
        float left = winTime - time;
        timerText.text = Mathf.CeilToInt(Mathf.Max(left, 0)).ToString();
        if (left <= 0) { Win(); return; }

        nextSpawn -= Time.deltaTime;
        if (nextSpawn <= 0)
        {
            float k = time / winTime;
            Spawn();
            if (k > 0.5f && Random.value < k - 0.4f) Spawn(); // second half: sometimes two at once
            nextSpawn = Mathf.Lerp(spawnStart, spawnEnd, k);
        }

        for (int i = chairs.Count - 1; i >= 0; i--)
        {
            var c = chairs[i];
            if (c.Done) { Destroy(c.gameObject); chairs.RemoveAt(i); continue; }
            if (c.Launched && SegmentDistance(player.Center, c.PrevCenter, c.Center) < hitRadius) { Hit(); return; }
        }
    }

    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float k = ab.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
        return Vector2.Distance(p, a + ab * k);
    }

    void Spawn()
    {
        Vector2 pos = Vector2.zero;
        for (int i = 0; i < 12; i++)
        {
            pos = new Vector2(Random.Range(spawnArea.xMin, spawnArea.xMax), Random.Range(spawnArea.yMin, spawnArea.yMax));
            if (Vector2.Distance(pos, player.Center) >= safeDistance) break;
        }

        var c = Instantiate(chairTemplate, chairTemplate.transform.parent);
        c.transform.localPosition = pos;
        c.target = player;
        c.outside = outside;
        c.gameObject.SetActive(true);
        chairs.Add(c);
    }

    void Hit()
    {
        ClearChairs();
        player.enabled = false;
        pause = hitPause;
    }

    void Win()
    {
        won = true;
        ClearChairs();
        player.enabled = false;
        timerText.text = "CLEAR";
        Invoke(nameof(Complete), 1f);
    }

    void Complete() => GameBoyConsole.CompleteCurrent();

    void ClearChairs()
    {
        foreach (var c in chairs) if (c) Destroy(c.gameObject);
        chairs.Clear();
        cam.transform.localPosition = camPos;
    }
}
