using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class VisionConeFilter : MonoBehaviour
{
    [Header("Ángulo Horizontal (Izquierda / Derecha)")]
    [Range(-180f, 0f)] public float minHorizontalAngle = -35f;
    [Range(0f, 180f)] public float maxHorizontalAngle = 35f;

    [Header("Ángulo Vertical (Abajo / Arriba)")]
    [Range(-90f, 0f)] public float minVerticalAngle = -20f;
    [Range(0f, 90f)] public float maxVerticalAngle = 20f;

    [Header("Filtrado")]
    public LayerMask targetLayers = ~0;

    private SphereCollider sphereCol;
    private readonly HashSet<Collider> insideSphere = new HashSet<Collider>();

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        sphereCol.isTrigger = true;
    }

    void OnEnable()
    {
        if (sphereCol != null)
            sphereCol.enabled = true;
    }

    void OnDisable()
    {
        insideSphere.Clear();
        if (sphereCol != null)
            sphereCol.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && IsInLayerMask(other.gameObject.layer, targetLayers))
        {
            insideSphere.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null)
        {
            insideSphere.Remove(other);
        }
    }

    public bool IsPointInsideCone(Vector3 worldPoint)
    {
        float radius = GetWorldSphereRadius();
        Vector3 toPoint = worldPoint - transform.position;

        if (toPoint.sqrMagnitude > radius * radius)
            return false;

        if (toPoint.sqrMagnitude < 0.000001f)
            return true;

        Vector3 localDir = transform.InverseTransformDirection(toPoint.normalized).normalized;

        float horizontalAngle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float verticalAngle = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

        bool hOk = horizontalAngle >= minHorizontalAngle && horizontalAngle <= maxHorizontalAngle;
        bool vOk = verticalAngle >= minVerticalAngle && verticalAngle <= maxVerticalAngle;

        return hOk && vOk;
    }

    public bool IsColliderInsideCone(Collider targetCollider)
    {
        if (targetCollider == null || !insideSphere.Contains(targetCollider))
            return false;

        Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
        return IsPointInsideCone(closestPoint);
    }

    private float GetWorldSphereRadius()
    {
        if (sphereCol == null) return 0f;
        Vector3 s = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        return sphereCol.radius * maxScale;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (sphereCol == null)
            sphereCol = GetComponent<SphereCollider>();

        float radius = GetWorldSphereRadius();
        if (radius <= 0f) return;

        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = Color.red;
        foreach (var dir in GetCornerDirections())
        {
            Gizmos.DrawRay(transform.position, dir * radius);
        }

        Gizmos.color = Color.yellow;
        DrawArc(minHorizontalAngle, maxHorizontalAngle, true, radius);
        DrawArc(minVerticalAngle, maxVerticalAngle, false, radius);
    }

    private Vector3[] GetCornerDirections()
    {
        float[] hAngles = { minHorizontalAngle, maxHorizontalAngle };
        float[] vAngles = { minVerticalAngle, maxVerticalAngle };
        Vector3[] dirs = new Vector3[4];
        int idx = 0;

        foreach (float h in hAngles)
            foreach (float v in vAngles)
                dirs[idx++] = DirectionFromAngles(h, v);

        return dirs;
    }

    private Vector3 DirectionFromAngles(float hDeg, float vDeg)
    {
        float hRad = hDeg * Mathf.Deg2Rad;
        float vRad = vDeg * Mathf.Deg2Rad;
        float cosV = Mathf.Cos(vRad);

        Vector3 localDir = new Vector3(
            Mathf.Sin(hRad) * cosV,
            Mathf.Sin(vRad),
            Mathf.Cos(hRad) * cosV
        );

        return transform.TransformDirection(localDir).normalized;
    }

    private void DrawArc(float fromDeg, float toDeg, bool horizontal, float radius)
    {
        const int steps = 20;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float angle = Mathf.Lerp(fromDeg, toDeg, t);
            Vector3 dir = horizontal
                ? DirectionFromAngles(angle, 0f)
                : DirectionFromAngles(0f, angle);

            Vector3 pt = transform.position + dir * radius;
            if (i > 0)
                Gizmos.DrawLine(prevPoint, pt);
            prevPoint = pt;
        }
    }
}