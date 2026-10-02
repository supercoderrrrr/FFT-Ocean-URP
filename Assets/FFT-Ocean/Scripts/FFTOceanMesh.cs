using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class FFTOceanMesh : MonoBehaviour
{
    [Header("Camera-centred Clipmap")]
    [SerializeField, Range(32, 240)] private int patchResolution = 240;
    [SerializeField, Range(3, 10)] private int clipLevels = 8;
    [SerializeField, Range(0.05f, 4f)] private float baseVertexSpacing = 0.125f;
    [SerializeField, Min(1f)] private float maximumExpectedWaveHeight = 32f;

    private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
    private readonly List<float> spacings = new List<float>();
    private readonly List<Mesh> generatedMeshes = new List<Mesh>();
    private readonly List<GameObject> generatedObjects = new List<GameObject>();

    public IReadOnlyList<MeshRenderer> Renderers => renderers;
    public IReadOnlyList<float> Spacings => spacings;
    public int PatchResolution => patchResolution;

    private void OnEnable()
    {
        if (Application.isPlaying)
            EnsureCreated();
    }

    private void OnDisable()
    {
        ReleaseClipmap();
    }

    private void OnValidate()
    {
        patchResolution = Mathf.Clamp(patchResolution, 32, 240);
        if ((patchResolution & 1) != 0)
            patchResolution++;
        clipLevels = Mathf.Clamp(clipLevels, 3, 10);
        baseVertexSpacing = Mathf.Max(0.05f, baseVertexSpacing);
        maximumExpectedWaveHeight = Mathf.Max(1f, maximumExpectedWaveHeight);
    }

    public void EnsureCreated()
    {
        if (!Application.isPlaying || generatedMeshes.Count > 0)
            return;

        for (int level = 0; level < clipLevels; level++)
        {
            float spacing = baseVertexSpacing * Mathf.Pow(2f, level);
            Mesh mesh = CreateLevelMesh(level, spacing);
            GameObject levelObject = new GameObject(level == 0 ? "Clipmap Center" : $"Clipmap Ring {level}");
            levelObject.transform.SetParent(transform, false);

            MeshFilter filter = levelObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = levelObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            renderer.allowOcclusionWhenDynamic = false;

            generatedMeshes.Add(mesh);
            generatedObjects.Add(levelObject);
            renderers.Add(renderer);
            spacings.Add(spacing);
        }
    }

    private Mesh CreateLevelMesh(int level, float spacing)
    {
        int verticesPerLine = patchResolution + 1;
        int half = patchResolution / 2;
        int innerHalf = patchResolution / 4;
        int vertexCount = verticesPerLine * verticesPerLine;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector4[] tangents = new Vector4[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        List<int> indices = new List<int>(patchResolution * patchResolution * 6);

        for (int z = 0; z <= patchResolution; z++)
        {
            for (int x = 0; x <= patchResolution; x++)
            {
                int index = z * verticesPerLine + x;
                vertices[index] = new Vector3((x - half) * spacing, 0f, (z - half) * spacing);
                normals[index] = Vector3.up;
                tangents[index] = new Vector4(1f, 0f, 0f, -1f);
                uv[index] = new Vector2(x / (float)patchResolution, z / (float)patchResolution);
            }
        }

        for (int z = 0; z < patchResolution; z++)
        {
            int cellZ = z - half;
            for (int x = 0; x < patchResolution; x++)
            {
                int cellX = x - half;
                bool insideHole = level > 0
                    && cellX >= -innerHalf && cellX < innerHalf
                    && cellZ >= -innerHalf && cellZ < innerHalf;
                if (insideHole)
                    continue;

                int bottomLeft = z * verticesPerLine + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + verticesPerLine;
                int topRight = topLeft + 1;
                if (((x + z + level) & 1) == 0)
                {
                    indices.Add(bottomLeft);
                    indices.Add(topLeft);
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);
                    indices.Add(bottomRight);
                }
                else
                {
                    indices.Add(bottomLeft);
                    indices.Add(topLeft);
                    indices.Add(bottomRight);
                    indices.Add(bottomRight);
                    indices.Add(topLeft);
                    indices.Add(topRight);
                }
            }
        }

        Mesh mesh = new Mesh
        {
            name = level == 0 ? "FFT Ocean Clipmap Center" : $"FFT Ocean Clipmap Ring {level}",
            indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.uv = uv;
        mesh.SetTriangles(indices, 0, true);
        float diameter = patchResolution * spacing;
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(diameter + spacing * 2f, maximumExpectedWaveHeight * 2f, diameter + spacing * 2f));
        return mesh;
    }

    private void ReleaseClipmap()
    {
        foreach (GameObject generatedObject in generatedObjects)
        {
            if (generatedObject == null)
                continue;
            if (Application.isPlaying)
                Destroy(generatedObject);
            else
                DestroyImmediate(generatedObject);
        }
        foreach (Mesh mesh in generatedMeshes)
        {
            if (mesh == null)
                continue;
            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
        renderers.Clear();
        spacings.Clear();
        generatedMeshes.Clear();
        generatedObjects.Clear();
    }
}
