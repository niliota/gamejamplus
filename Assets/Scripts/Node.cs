using System.Collections.Generic;
using UnityEngine;

// Xəritədə dayanma nöqtəsi (kəsişmə, koridor sonu, qapı önü).
public class Node : MonoBehaviour
{
    public List<Node> neighbors = new List<Node>();

    void Awake()
    {
        // Əlaqəni avtomatik iki tərəfli edir: A-ya B əlavə etsən, B-yə də A əlavə olunur.
        foreach (var n in neighbors)
            if (n && !n.neighbors.Contains(this))
                n.neighbors.Add(this);
    }

    // Scene pəncərəsində nöqtələri və əlaqələri göstərir.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);
        foreach (var n in neighbors)
            if (n) Gizmos.DrawLine(transform.position, n.transform.position);
    }
}
