using UnityEngine;

// A chair that rises off the floor, hovers and shakes (the warning), then locks onto
// the player's position and accelerates in a straight line until it leaves the arena.
public class ThrownChair : MonoBehaviour
{
    public SpriteRenderer body, shadow;
    public float riseTime = 0.3f;
    public float hoverTime = 0.8f;
    public float height = 0.7f;     // how high the chair floats above its shadow
    public float accel = 30f, maxSpeed = 17f;
    public float spin = 900f;       // deg/s while flying, shown in 90° steps to keep the pixel grid

    [HideInInspector] public TopDownPlayer target;
    [HideInInspector] public Rect outside;  // once it leaves this rect, it's done

    const float Px = 1 / 16f;       // one screen pixel (16 PPU, 1:1 into RT_GameBoy)

    float t, speed, angle;
    Vector2 dir, pos;               // pos: exact ground position; the transform gets it pixel-snapped
    Color baseColor;

    public bool Launched { get; private set; }
    public bool Done { get; private set; }
    public Vector2 Center => pos + Vector2.up * body.transform.localPosition.y;
    public Vector2 PrevCenter { get; private set; }  // centre last frame, so fast chairs can't skip past the player

    void Start()
    {
        baseColor = body.color;
        pos = transform.localPosition;
    }

    void Update()
    {
        t += Time.deltaTime;
        PrevCenter = Center;

        if (!Launched)
        {
            float rise = Mathf.SmoothStep(0, 1, t / riseTime);
            float warn = Mathf.Clamp01((t - riseTime) / hoverTime);           // 0 → 1 while hovering
            // Bob and shake in whole pixels: the shake is a 1-2px jitter that grows while it hovers.
            float shake = Mathf.Round(warn * 2) * Mathf.Sign(Mathf.Sin(t * 50));
            body.transform.localPosition = new Vector3(shake * Px, Snap(height * rise + 0.12f * Mathf.Sin(t * 9)), 0);
            body.color = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.4f), warn * (0.5f + 0.5f * Mathf.Sin(t * 30)));

            if (t >= riseTime + hoverTime)
            {
                Launched = true;
                body.color = baseColor;
                body.transform.localPosition = Vector3.up * Snap(height);
                dir = target.Center - Center;
                dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.down;
            }
        }
        else
        {
            speed = Mathf.Min(speed + accel * Time.deltaTime, maxSpeed);
            pos += dir * speed * Time.deltaTime;
            transform.localPosition = TopDownPlayer.Snap(pos);
            angle += spin * Time.deltaTime;
            body.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Round(angle / 90) * 90);
            Done = !outside.Contains(pos);
        }

        body.sortingOrder = TopDownPlayer.Order(pos.y);
    }

    static float Snap(float v) => Mathf.Round(v / Px) * Px;
}
