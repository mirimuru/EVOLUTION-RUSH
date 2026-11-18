using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー・通常敵・ボスの仮ビジュアルをランタイム生成する。
/// 後から3Dモデルへ差し替えやすいよう、当たり判定を持つルートとは分離している。
/// </summary>
public class NeonCharacterVisuals : MonoBehaviour
{
    private readonly List<Material> ownedMaterials = new List<Material>();
    private Transform visualRoot;

    public void BuildPlayer()
    {
        if (visualRoot != null) return;
        CreateRoot();
        HideRootRenderers();

        Material armor = MakeMaterial(new Color(0.055f, 0.10f, 0.18f), new Color(0.02f, 0.28f, 0.48f), 0.8f);
        Material bright = MakeMaterial(new Color(0.12f, 0.78f, 1f), new Color(0.05f, 0.65f, 1f), 3.2f);
        Material dark = MakeMaterial(new Color(0.018f, 0.035f, 0.07f), Color.black, 0f);
        Material visor = MakeMaterial(new Color(0.3f, 0.95f, 1f), new Color(0.1f, 0.8f, 1f), 4f);

        Part(PrimitiveType.Capsule, "ArmoredBody", new Vector3(0f, 0.05f, 0f), new Vector3(0.72f, 0.92f, 0.48f), armor);
        Part(PrimitiveType.Sphere, "EnergyCore", new Vector3(0f, 0.18f, 0.255f), new Vector3(0.28f, 0.3f, 0.12f), bright);
        Part(PrimitiveType.Sphere, "Helmet", new Vector3(0f, 0.88f, 0.02f), new Vector3(0.48f, 0.48f, 0.43f), dark);
        Part(PrimitiveType.Cube, "Visor", new Vector3(0f, 0.91f, 0.225f), new Vector3(0.34f, 0.095f, 0.045f), visor);
        Part(PrimitiveType.Cube, "LeftShoulder", new Vector3(-0.48f, 0.42f, 0f), new Vector3(0.32f, 0.34f, 0.42f), armor);
        Part(PrimitiveType.Cube, "RightShoulder", new Vector3(0.48f, 0.42f, 0f), new Vector3(0.32f, 0.34f, 0.42f), armor);
        Part(PrimitiveType.Cube, "LeftArmGlow", new Vector3(-0.49f, 0.25f, 0.19f), new Vector3(0.07f, 0.32f, 0.055f), bright);
        Part(PrimitiveType.Cube, "RightArmGlow", new Vector3(0.49f, 0.25f, 0.19f), new Vector3(0.07f, 0.32f, 0.055f), bright);
        Part(PrimitiveType.Cube, "ChestPlate", new Vector3(0f, 0.48f, 0.245f), new Vector3(0.43f, 0.28f, 0.09f), dark);
        Part(PrimitiveType.Cube, "ChestLine", new Vector3(0f, 0.49f, 0.298f), new Vector3(0.28f, 0.035f, 0.025f), bright);
    }

    public void BuildEnemy()
    {
        if (visualRoot != null) return;
        CreateRoot();
        HideRootRenderers();

        Material shell = MakeMaterial(new Color(0.16f, 0.035f, 0.20f), new Color(0.35f, 0.015f, 0.5f), 0.9f);
        Material core = MakeMaterial(new Color(1f, 0.12f, 0.28f), new Color(1f, 0.015f, 0.08f), 3f);
        Material dark = MakeMaterial(new Color(0.035f, 0.025f, 0.07f), Color.black, 0f);

        Part(PrimitiveType.Sphere, "EnemyShell", new Vector3(0f, 0f, 0f), new Vector3(0.82f, 0.9f, 0.78f), shell);
        Part(PrimitiveType.Sphere, "EnemyCore", new Vector3(0f, 0.04f, 0.39f), new Vector3(0.3f, 0.3f, 0.16f), core);
        Part(PrimitiveType.Cube, "EnemyFront", new Vector3(0f, 0.28f, 0.36f), new Vector3(0.52f, 0.12f, 0.18f), dark);
        Part(PrimitiveType.Cone, "LeftHorn", new Vector3(-0.28f, 0.48f, 0f), new Vector3(0.22f, 0.42f, 0.22f), shell);
        Part(PrimitiveType.Cone, "RightHorn", new Vector3(0.28f, 0.48f, 0f), new Vector3(0.22f, 0.42f, 0.22f), shell);
    }

    public void BuildBoss()
    {
        if (visualRoot != null) return;
        CreateRoot();
        HideRootRenderers();

        Material shell = MakeMaterial(new Color(0.10f, 0.035f, 0.22f), new Color(0.42f, 0.04f, 0.9f), 1.8f);
        Material core = MakeMaterial(new Color(0.2f, 0.9f, 1f), new Color(0.1f, 0.7f, 1f), 4f);
        Material trim = MakeMaterial(new Color(0.9f, 0.12f, 0.65f), new Color(0.8f, 0.02f, 0.5f), 2.5f);

        Part(PrimitiveType.Sphere, "BossCore", new Vector3(0f, 0.05f, 0.35f), new Vector3(0.72f, 0.72f, 0.25f), core);
        Part(PrimitiveType.Cube, "BossChest", new Vector3(0f, 0.25f, 0.2f), new Vector3(1.25f, 0.65f, 0.42f), shell);
        Part(PrimitiveType.Sphere, "BossShoulderL", new Vector3(-0.78f, 0.35f, 0f), new Vector3(0.62f, 0.62f, 0.62f), trim);
        Part(PrimitiveType.Sphere, "BossShoulderR", new Vector3(0.78f, 0.35f, 0f), new Vector3(0.62f, 0.62f, 0.62f), trim);
        Part(PrimitiveType.Sphere, "BossHead", new Vector3(0f, 0.9f, 0f), new Vector3(0.72f, 0.65f, 0.65f), shell);
        Part(PrimitiveType.Cube, "BossVisor", new Vector3(0f, 0.93f, 0.32f), new Vector3(0.48f, 0.12f, 0.06f), core);
    }

    private void CreateRoot()
    {
        GameObject root = new GameObject("NeonVisualRoot");
        visualRoot = root.transform;
        visualRoot.SetParent(transform, false);
    }

    private void HideRootRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.transform != visualRoot) renderer.enabled = false;
        }
    }

    private void Part(PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private Material MakeMaterial(Color baseColor, Color emission, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = baseColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission * intensity);
        }
        ownedMaterials.Add(material);
        return material;
    }

    private void OnDestroy()
    {
        foreach (Material material in ownedMaterials)
        {
            if (material == null) continue;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
        ownedMaterials.Clear();
    }
}
