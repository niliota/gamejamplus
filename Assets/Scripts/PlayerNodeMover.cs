using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// W: baxdığın istiqamətdəki növbəti node-a get (basılı saxlasan davam edir)
// A / D: 90° sola / sağa dön
// S: 180° geri dön
// Mouse: gedilə bilən döşəmənin üstündə əl kursoru çıxır, klikləyəndə oraya dönüb gedir
public class PlayerNodeMover : MonoBehaviour
{
    public Node currentNode;
    public float moveSpeed = 5f;     // metr/saniyə
    public float turnSpeed = 540f;   // dərəcə/saniyə
    public float eyeHeight = 1.6f;   // kameranın node-dan hündürlüyü

    [Header("Kamera sarsıntısı")]
    public float bobAmount = 0.04f;  // metr
    public float stepLength = 0.8f;  // bir addımın uzunluğu (m)

    [Header("Mouse")]
    public Texture2D handCursor;
    public Vector2 handHotspot = new Vector2(6, 0); // barmağın ucu

    bool busy, handShown;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        transform.position = Eye(currentNode);
    }

    void Update()
    {
        if (busy) { SetHand(false); return; }

        var hover = NodeUnderMouse();
        SetHand(hover);
        if (hover && Mouse.current.leftButton.wasPressedThisFrame) { StartCoroutine(Do(GoTo(hover))); return; }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.isPressed) TryMove();
        else if (kb.aKey.wasPressedThisFrame) StartCoroutine(Do(Turn(-90)));
        else if (kb.dKey.wasPressedThisFrame) StartCoroutine(Do(Turn(90)));
        else if (kb.sKey.wasPressedThisFrame) StartCoroutine(Do(Turn(180)));
    }

    void TryMove()
    {
        // Baxdığın istiqamətə ən yaxın qonşunu seç (təxminən 45° daxilində).
        Node best = null;
        float bestDot = 0.7f;

        foreach (var n in currentNode.neighbors)
        {
            Vector3 dir = n.transform.position - currentNode.transform.position;
            dir.y = 0;
            float dot = Vector3.Dot(transform.forward, dir.normalized);
            if (dot > bestDot) { bestDot = dot; best = n; }
        }

        if (best) StartCoroutine(Do(MoveTo(best)));
    }

    // Mouse döşəmədə hansısa qonşuya gedən yolun üstündədirsə, o qonşunu qaytarır.
    Node NodeUnderMouse()
    {
        if (Mouse.current == null || !cam) return null;
        var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 60f) || hit.normal.y < 0.9f) return null; // yalnız döşəmə

        Vector3 a = currentNode.transform.position;
        Node best = null;
        float bestDist = 1.2f; // koridorun yarı eni + bir az

        foreach (var n in currentNode.neighbors)
        {
            Vector3 ab = n.transform.position - a;
            float t = Vector3.Dot(hit.point - a, ab) / ab.sqrMagnitude;
            if (t < 0.1f || t > 1f) continue; // ayağının altı sayılmır
            float d = Vector3.Distance(hit.point, a + ab * t);
            if (d < bestDist) { bestDist = d; best = n; }
        }
        return best;
    }

    void SetHand(bool on)
    {
        if (on == handShown) return;
        handShown = on;
        Cursor.SetCursor(on ? handCursor : null, handHotspot, CursorMode.Auto);
    }

    IEnumerator Do(IEnumerator action)
    {
        busy = true;
        yield return action;
        busy = false;
    }

    IEnumerator GoTo(Node target)
    {
        Vector3 dir = target.transform.position - currentNode.transform.position;
        dir.y = 0;
        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        if (Mathf.Abs(angle) > 1f) yield return Turn(Mathf.Round(angle / 90f) * 90f);
        yield return MoveTo(target);
    }

    IEnumerator MoveTo(Node target)
    {
        Vector3 pos = Eye(currentNode), end = Eye(target);
        float total = Vector3.Distance(pos, end);
        int steps = Mathf.Max(1, Mathf.RoundToInt(total / stepLength));

        while (pos != end)
        {
            pos = Vector3.MoveTowards(pos, end, moveSpeed * Time.deltaTime);
            // Addım sarsıntısı: hər addımda aşağı-yuxarı, iki addımda bir sağa-sola. Başda və sonda sıfırdır.
            float s = Mathf.Sin((1 - Vector3.Distance(pos, end) / total) * steps * Mathf.PI);
            transform.position = pos + Vector3.down * Mathf.Abs(s) * bobAmount + transform.right * s * bobAmount * 0.5f;
            yield return null;
        }

        transform.position = end;
        currentNode = target;
    }

    IEnumerator Turn(float angle)
    {
        Quaternion start = transform.rotation;
        Vector3 eye = Eye(currentNode);
        float total = Mathf.Abs(angle), done = 0;

        while (done < total)
        {
            done = Mathf.Min(total, done + turnSpeed * Time.deltaTime);
            float t = done / total;
            transform.rotation = start * Quaternion.Euler(0, angle * t, 0);
            transform.position = eye + Vector3.down * Mathf.Sin(t * Mathf.PI) * bobAmount; // dönərkən yüngül çökmə
            yield return null;
        }

        transform.position = eye;
    }

    Vector3 Eye(Node n) => n.transform.position + Vector3.up * eyeHeight;
}
