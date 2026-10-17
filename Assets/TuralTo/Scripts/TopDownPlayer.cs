using UnityEngine;
using UnityEngine.InputSystem;

// Top-down mini-game player: WASD / arrows move, 4-direction walk animation.
// Works in the mini-game root's local space (the scene sits far away from the main level).
public class TopDownPlayer : MonoBehaviour
{
    public float speed = 5f;
    public Rect bounds = new Rect(-5f, -3.9f, 10f, 5.5f);   // allowed feet positions
    public float frameTime = 0.1f;                            // same as the source GIFs
    public Sprite[] idle, down, up, left, right;

    SpriteRenderer sr;
    Sprite[] anim;
    float animTime;
    Vector2 pos;    // exact position; the transform gets it snapped to whole screen pixels

    // Sprite pivot is at the feet; hits are checked against the body centre.
    public Vector2 Center => pos + Vector2.up;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = idle;
        pos = transform.localPosition;
    }

    void Update()
    {
        var kb = Keyboard.current;
        Vector2 move = Vector2.zero;
        if (kb != null)
        {
            move.x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            move.y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
        }
        move = Vector2.ClampMagnitude(move, 1);

        pos += move * speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax);
        pos.y = Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax);
        transform.localPosition = Snap(pos);

        var next = move == Vector2.zero ? idle
                 : Mathf.Abs(move.x) > Mathf.Abs(move.y) ? (move.x > 0 ? right : left)
                 : (move.y > 0 ? up : down);
        if (next != anim) { anim = next; animTime = 0; }
        animTime += Time.deltaTime;
        sr.sprite = anim[(int)(animTime / frameTime) % anim.Length];
        sr.sortingOrder = Order(pos.y);
    }

    public void ResetTo(Vector2 start)
    {
        pos = start;
        transform.localPosition = Snap(pos);
        anim = idle;
        sr.sprite = idle[0];
        sr.enabled = true;
    }

    // 16 PPU drawn 1:1 into RT_GameBoy: whole-pixel positions keep the 2x sprite's pixels even.
    public static Vector2 Snap(Vector2 v) => new Vector2(Mathf.Round(v.x * 16) / 16, Mathf.Round(v.y * 16) / 16);

    // Lower on screen = drawn in front.
    public static int Order(float y) => Mathf.RoundToInt(-y * 16);
}
