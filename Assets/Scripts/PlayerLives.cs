using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Oyunçunun canları (yuxarı solda ürəklər). Varlıq tutanda Lose(); canlar bitəndə ekran qaralır və oyun yenidən başlayır.
public class PlayerLives : MonoBehaviour
{
    public int lives = 3;
    public Text hearts;                // "♥ ♥ ♥"
    public Image flash;                // tam ekran qırmızı (jump-scare)
    public Image fade;                 // tam ekran qara (oyun sonu)

    void Start() => Refresh();

    void Refresh() => hearts.text = string.Join(" ", new string('♥', Mathf.Max(lives, 0)).ToCharArray());

    public void Flash() => StartCoroutine(FlashRoutine());

    IEnumerator FlashRoutine()
    {
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            flash.color = new Color(0.6f, 0, 0, (Mathf.PingPong(t * 6, 1) * 0.5f) * (1 - t / 1.2f));
            yield return null;
        }
        flash.color = Color.clear;
    }

    public void Lose()
    {
        lives--;
        Refresh();
        if (lives <= 0) StartCoroutine(GameOver());
    }

    IEnumerator GameOver()
    {
        FindAnyObjectByType<HorrorController>().enabled = false;
        if (GameAudio.I) GameAudio.I.Fail();
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            fade.color = new Color(0, 0, 0, t / 2f);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
