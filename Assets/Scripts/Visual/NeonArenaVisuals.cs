using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NEON ARENAの床・発光リング・外周ライトを自動生成する。
/// シーンファイルを編集せず、プレイ開始時に表示用オブジェクトだけを追加する。
/// </summary>
public class NeonArenaVisuals : MonoBehaviour
{
    [SerializeField] private float arenaRadius = 24f;
    [SerializeField] private int ringSegments = 96;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Renderer> pulseRenderers = new List<Renderer>();
    private Material ringMaterial;
    private float pulseTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        if (FindFirstObjectByType<NeonArenaVisuals>() != null) return;
        new GameObject("NEON ARENA VISUALS").AddComponent<NeonArenaVisuals>();
    }

    private void Awake()
    {
        BuildArena();
    }

    private void Update()
    {
        GameSessionManager session = GameSessionManager.Instance;
        float progress = session != null ? session.Progress : 0f;
        pulseTime += Time.deltaTime * (1.5f + progress * 3f);
        float pulse = 0.75f + Mathf.Sin(pulseTime) * 0.2f + progress * 0.25f;

        foreach (Renderer renderer in pulseRenderers)
        {
            if (renderer == null) continue;
            Material material = renderer.sharedMaterial;
            if (material != null && material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", new Color(0.02f, 0.55f, 1f) * pulse * 2.2f);
        }
    }

    private void BuildArena()
    {
        Material floor = CreateMaterial(new Color(0.012f, 0.022f, 0.055f), Color.black, 0f);
        Material cyan = CreateMaterial(new Color(0.02f, 0.45f, 0.75f), new Color(0.02f, 0.55f, 1f), 2.2f);
        Material violet = CreateMaterial(new Color(0.28f, 0.035f, 0.48f), new Color(0.5f, 0.025f, 1f), 2.4f);
        ringMaterial = cyan;

        GameObject disk = new GameObject("NeonArenaFloor");
        disk.transform.SetParent(transform, false);
        MeshFilter filter = disk.AddComponent<MeshFilter>();
        MeshRenderer renderer = disk.AddComponent<MeshRenderer>();
        filter.sharedMesh = CreateDiskMesh(arenaRadius, ringSegments);
        renderer.sharedMaterial = floor;

        CreateRing("ArenaInnerRing", 7f, cyan, 0.055f);
        CreateRing("ArenaMidRing", 15f, violet, 0.075f);
        CreateRing("ArenaOuterRing", arenaRadius - 1.2f, cyan, 0.13f);
        CreateRing("ArenaBoundaryRing", arenaRadius, violet, 0.18f);

        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            CreateLine("FloorRadialLine", direction * 7.5f, direction * (arenaRadius - 1.5f), cyan, 0.025f);

            Vector3 pillarPosition = direction * (arenaRadius - 0.6f);
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "NeonBoundaryPillar";
            pillar.transform.SetParent(transform, false);
            pillar.transform.localPosition = pillarPosition + Vector3.up * 1.5f;
            pillar.transform.localScale = new Vector3(0.22f, 1.5f, 0.22f);
            Collider collider = pillar.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer pillarRenderer = pillar.GetComponent<Renderer>();
            pillarRenderer.sharedMaterial = i % 2 == 0 ? cyan : violet;
            pulseRenderers.Add(pillarRenderer);

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "NeonBoundaryLight";
            cap.transform.SetParent(transform, false);
            cap.transform.localPosition = pillarPosition + Vector3.up * 3.1f;
            cap.transform.localScale = Vector3.one * 0.34f;
            Collider capCollider = cap.GetComponent<Collider>();
            if (capCollider != null) Destroy(capCollider);
            cap.GetComponent<Renderer>().sharedMaterial = cyan;
        }
    }

    private void CreateRing(string name, float radius, Material material, float width)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(transform, false);
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = ringSegments;
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        line.startColor = line.endColor = Color.white;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ringSegments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.045f, Mathf.Sin(angle) * radius));
        }
    }

    private void CreateLine(string name, Vector3 start, Vector3 end, Material material, float width)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        line.startColor = line.endColor = Color.white;
        line.SetPosition(0, new Vector3(start.x, 0.035f, start.z));
        line.SetPosition(1, new Vector3(end.x, 0.035f, end.z));
    }

    private Mesh CreateDiskMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh { name = "NeonArenaDiskMesh" };
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        vertices[0] = new Vector3(0f, -0.035f, 0f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, -0.035f, Mathf.Sin(angle) * radius);
            int next = (i + 1) % segments;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = next + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Material CreateMaterial(Color baseColor, Color emission, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = baseColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (intensity > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission * intensity);
        }
        materials.Add(material);
        return material;
    }

    private void OnDestroy()
    {
        foreach (Material material in materials)
        {
            if (material == null) continue;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            if (Application.isPlaying) Destroy(filter.sharedMesh);
            else DestroyImmediate(filter.sharedMesh);
        }
    }
}
