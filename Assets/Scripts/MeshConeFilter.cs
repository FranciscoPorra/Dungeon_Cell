using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(VisionConeFilter))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SphereConeSectorMesh : MonoBehaviour
{
    [Header("Resolución de la Malla")]
    [Range(8, 64)] public int horizontalSteps = 24;
    [Range(4, 32)] public int verticalSteps = 12;

    [Header("Doble Cara")]
    [Tooltip("Genera triángulos invertidos para que también se vea desde el interior")]
    public bool doubleSidedMesh = true;

    [Header("Rendimiento en VR")]
    public bool disableShadows = true;

    private SphereCollider sphereCol;
    private VisionConeFilter coneFilter;
    private MeshFilter meshFilter;
    private Mesh sectorMesh;

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        coneFilter = GetComponent<VisionConeFilter>();
        meshFilter = GetComponent<MeshFilter>();

        if (disableShadows)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        RebuildSectorMesh();
    }

    [ContextMenu("Rebuild Sector Mesh")]
    public void RebuildSectorMesh()
    {
        if (sphereCol == null) sphereCol = GetComponent<SphereCollider>();
        if (coneFilter == null) coneFilter = GetComponent<VisionConeFilter>();
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

        if (sectorMesh == null)
        {
            sectorMesh = new Mesh { name = "VisionConeSectorMesh" };
        }
        else
        {
            sectorMesh.Clear();
        }

        float radius = sphereCol != null ? Mathf.Max(0.01f, sphereCol.radius) : 3f;
        float minH = coneFilter != null ? coneFilter.minHorizontalAngle : -35f;
        float maxH = coneFilter != null ? coneFilter.maxHorizontalAngle : 35f;
        float minV = coneFilter != null ? coneFilter.minVerticalAngle : -20f;
        float maxV = coneFilter != null ? coneFilter.maxVerticalAngle : 20f;

        // Vértice 0 = origen (mirilla / centro del SphereCollider)
        // Grilla de (horizontalSteps + 1) x (verticalSteps + 1) en la superficie esférica
        int cols = horizontalSteps + 1;
        int rows = verticalSteps + 1;
        int gridCount = cols * rows;

        Vector3[] vertices = new Vector3[1 + gridCount];
        vertices[0] = Vector3.zero;

        for (int r = 0; r < rows; r++)
        {
            float vT = (float)r / verticalSteps;
            float vDeg = Mathf.Lerp(minV, maxV, vT);
            float vRad = vDeg * Mathf.Deg2Rad;
            float cosV = Mathf.Cos(vRad);
            float sinV = Mathf.Sin(vRad);

            for (int c = 0; c < cols; c++)
            {
                float hT = (float)c / horizontalSteps;
                float hDeg = Mathf.Lerp(minH, maxH, hT);
                float hRad = hDeg * Mathf.Deg2Rad;

                // +Z adelante, +X derecha, +Y arriba (coincide con VisionConeFilter)
                Vector3 dir = new Vector3(
                    Mathf.Sin(hRad) * cosV,
                    sinV,
                    Mathf.Cos(hRad) * cosV
                );

                int idx = 1 + r * cols + c;
                vertices[idx] = dir * radius;
            }
        }

        var tris = new System.Collections.Generic.List<int>();

        // 1. Tapa exterior (superficie esférica)
        for (int r = 0; r < verticalSteps; r++)
        {
            for (int c = 0; c < horizontalSteps; c++)
            {
                int i00 = 1 + r * cols + c;
                int i10 = 1 + r * cols + (c + 1);
                int i01 = 1 + (r + 1) * cols + c;
                int i11 = 1 + (r + 1) * cols + (c + 1);

                tris.Add(i00);
                tris.Add(i01);
                tris.Add(i10);

                tris.Add(i10);
                tris.Add(i01);
                tris.Add(i11);
            }
        }

        // 2. Paredes laterales que conectan el origen (0) con el contorno del sector
        for (int c = 0; c < horizontalSteps; c++)
        {
            int b0 = 1 + 0 * cols + c;
            int b1 = 1 + 0 * cols + (c + 1);
            tris.Add(0);
            tris.Add(b1);
            tris.Add(b0);

            int t0 = 1 + verticalSteps * cols + c;
            int t1 = 1 + verticalSteps * cols + (c + 1);
            tris.Add(0);
            tris.Add(t0);
            tris.Add(t1);
        }

        for (int r = 0; r < verticalSteps; r++)
        {
            int l0 = 1 + r * cols + 0;
            int l1 = 1 + (r + 1) * cols + 0;
            tris.Add(0);
            tris.Add(l0);
            tris.Add(l1);

            int ri0 = 1 + r * cols + horizontalSteps;
            int ri1 = 1 + (r + 1) * cols + horizontalSteps;
            tris.Add(0);
            tris.Add(ri1);
            tris.Add(ri0);
        }

        // 3. Caras invertidas para que también se pinte desde el interior
        if (doubleSidedMesh)
        {
            int outerTriCount = tris.Count;
            for (int i = 0; i < outerTriCount; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];

                tris.Add(a);
                tris.Add(c);
                tris.Add(b);
            }
        }

        sectorMesh.vertices = vertices;
        sectorMesh.triangles = tris.ToArray();
        sectorMesh.RecalculateNormals();
        sectorMesh.RecalculateBounds();

        meshFilter.sharedMesh = sectorMesh;
    }
}