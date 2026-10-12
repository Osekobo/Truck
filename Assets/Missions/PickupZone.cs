using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupZone : MonoBehaviour
{
    public Color gizmoColor = new Color(0.2f, 0.5f, 1f, 0.3f);

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}
