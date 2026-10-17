using UnityEngine;
using UnityEngine.InputSystem;

// Qorxu oyunu üçün birinci şəxs controller.
// WASD: yeri, Shift: qaç, Mouse: bax, Q / E: sola / sağa boylan (peek), sol klik: baxdığın obyektlə əlaqə (qapı, masa).
// Kursor kilidlənir; Esc ilə açılır, oyuna klikləyəndə yenidən kilidlənir.
[RequireComponent(typeof(CharacterController))]
public class HorrorController : MonoBehaviour
{
    public Transform head;                 // kamera; boylanma və baş sarsıntısı bunu hərəkət etdirir
    public float walkSpeed = 2f;           // m/s
    public float runSpeed = 3.8f;
    public float mouseSensitivity = 0.1f;  // dərəcə / piksel
    public float gravity = -15f;
    public float interactDistance = 2.5f;  // sol klik ilə qapı açma məsafəsi (m)

    [Header("Boylanma (Q / E)")]
    public float leanAngle = 15f;          // başın yana əyilməsi (dərəcə)
    public float leanOffset = 0.45f;       // başın yana sürüşməsi (m)
    public float leanSpeed = 8f;

    [Header("Baş sarsıntısı")]
    public float bobAmount = 0.04f;        // m
    public float bobFrequency = 9f;        // yeriyəndə; qaçanda 1.4 dəfə tez

    CharacterController cc;
    Vector3 headBase;
    float pitch, lean, bobTime, fallSpeed;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        headBase = head.localPosition;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.Locked;

        // Baxış: gövdə sağa-sola, baş yuxarı-aşağı fırlanır.
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0, d.x, 0);
            pitch = Mathf.Clamp(pitch - d.y, -80, 80);

            // Sol klik: baxdığın obyektdə Interact() çağır (qapı, mühafizə masası...)
            if (mouse.leftButton.wasPressedThisFrame &&
                Physics.Raycast(head.position, head.forward, out var clickHit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
                clickHit.collider.SendMessageUpwards("Interact", SendMessageOptions.DontRequireReceiver);
        }

        // Yeriş
        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        bool running = kb.leftShiftKey.isPressed && z > 0;
        Vector3 move = (transform.right * x + transform.forward * z).normalized * (running ? runSpeed : walkSpeed);
        fallSpeed = cc.isGrounded ? -1f : fallSpeed + gravity * Time.deltaTime;
        move.y = fallSpeed;
        cc.Move(move * Time.deltaTime);

        // Boylanma: yanda divar varsa, baş ona girməsin deyə az əyilir.
        float target = (kb.eKey.isPressed ? 1 : 0) - (kb.qKey.isPressed ? 1 : 0);
        if (target != 0 && Physics.SphereCast(transform.TransformPoint(headBase), 0.15f, transform.right * target,
                out var hit, leanOffset, ~0, QueryTriggerInteraction.Ignore))
            target *= hit.distance / leanOffset;
        lean = Mathf.Lerp(lean, target, leanSpeed * Time.deltaTime);

        // Baş sarsıntısı: hər addımda bir az enir, addımlarla sağa-sola yırğalanır; dayananda sakitcə sönür.
        bool walking = cc.isGrounded && new Vector2(cc.velocity.x, cc.velocity.z).sqrMagnitude > 0.01f;
        if (walking) bobTime += Time.deltaTime * bobFrequency * (running ? 1.4f : 1f);
        else bobTime = Mathf.MoveTowards(bobTime, Mathf.Round(bobTime / Mathf.PI) * Mathf.PI, Time.deltaTime * 3f);
        float s = Mathf.Sin(bobTime) * bobAmount * (running ? 1.5f : 1f);
        Vector3 bob = new Vector3(s * 0.5f, -Mathf.Abs(s), 0);

        head.localPosition = headBase + Vector3.right * lean * leanOffset + bob;
        head.localRotation = Quaternion.Euler(pitch, 0, -lean * leanAngle);
    }
}
