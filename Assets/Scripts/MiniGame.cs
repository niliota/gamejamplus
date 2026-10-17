using UnityEngine;
using UnityEngine.InputSystem;

// Game Boy mini-oyunu: yuxarıdan düşən bloklardan qaç. A / D və ya ← / →.
// Bloka dəyəndə oyun yenidən başlayır; xal yaşadığın müddətdir. winTime saniyə dayananda anomaliya həll olunur.
public class MiniGame : MonoBehaviour
{
    public Transform player;
    public Transform[] blocks;        // təkrar istifadə olunan düşən bloklar
    public TextMesh scoreText;
    public float halfWidth = 4.5f;    // oyun sahəsinin yarı eni
    public float top = 4.5f, bottom = -4.5f;
    public float playerSpeed = 7f;
    public float winTime = 30f;       // bu qədər saniyə dayan → anomaliya həll olunur

    float speed, score;

    void Start() => ResetGame();

    void ResetGame()
    {
        speed = 3f;
        score = 0;
        for (int i = 0; i < blocks.Length; i++) Respawn(blocks[i], top + 1 + i * (top - bottom) / blocks.Length);
    }

    void Respawn(Transform b, float y) => b.localPosition = new Vector3(Random.Range(-halfWidth, halfWidth), y, 0);

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
        var p = player.localPosition;
        p.x = Mathf.Clamp(p.x + x * playerSpeed * Time.deltaTime, -halfWidth, halfWidth);
        player.localPosition = p;

        speed += 0.25f * Time.deltaTime;
        score += Time.deltaTime;
        scoreText.text = Mathf.FloorToInt(score * 10).ToString();
        if (score >= winTime) { GameBoyConsole.CompleteCurrent(); enabled = false; return; }

        foreach (var b in blocks)
        {
            b.localPosition += Vector3.down * speed * Time.deltaTime;
            if (b.localPosition.y < bottom - 1) Respawn(b, top + 1);
            if (Mathf.Abs(b.localPosition.x - p.x) < 0.8f && Mathf.Abs(b.localPosition.y - p.y) < 0.8f) { ResetGame(); return; }
        }
    }
}
