using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeliveryZone : MonoBehaviour
{
    public string zoneName = "Warehouse";
    public Color gizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.3f);

    public static event System.Action<DeliveryZone> OnTruckEntered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<TruckController>() == null) return;
        OnTruckEntered?.Invoke(this);
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
