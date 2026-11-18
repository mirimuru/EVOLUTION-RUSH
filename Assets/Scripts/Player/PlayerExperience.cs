using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの経験値・レベル・レベルアップ時の強化選択を管理する。
/// </summary>
public class PlayerExperience : MonoBehaviour
{
    private enum UpgradeKind
    {
        EnergyDamage,
        EnergyFireRate,
        EnergyProjectileSpeed,
        EnergyAttackRange,
        UnlockOrbitingOrb,
        OrbCount,
        OrbDamage,
        OrbSpeed,
        UnlockShockwave,
        ShockwaveRadius,
        ShockwaveDamage,
        ShockwaveCooldown
    }

    private class UpgradeChoice
    {
        public UpgradeKind kind;
        public string title;
        public string description;

        public UpgradeChoice(UpgradeKind kind, string title, string description)
        {
            this.kind = kind;
            this.title = title;
            this.description = description;
        }
    }

    [Header("Experience")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExperience;
    [SerializeField] private int experienceToNextLevel = 5;
    [SerializeField] private float experienceGrowth = 1.35f;

    private PlayerAutoAttack playerAttack;
    private OrbitingOrbAttack orbitingOrbAttack;
    private ShockwaveAttack shockwaveAttack;
    private readonly List<UpgradeChoice> choices = new List<UpgradeChoice>();
    private bool choosingUpgrade;
    private float previousTimeScale = 1f;

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    private void Awake()
    {
        playerAttack = GetComponent<PlayerAutoAttack>();
        if (playerAttack == null) playerAttack = GetComponentInChildren<PlayerAutoAttack>();

        orbitingOrbAttack = GetComponent<OrbitingOrbAttack>();
        if (orbitingOrbAttack == null) orbitingOrbAttack = GetComponentInChildren<OrbitingOrbAttack>();

        shockwaveAttack = GetComponent<ShockwaveAttack>();
        if (shockwaveAttack == null) shockwaveAttack = GetComponentInChildren<ShockwaveAttack>();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        currentExperience += amount;
        Debug.Log($"経験値 +{amount} ({currentExperience}/{experienceToNextLevel})");

        if (!choosingUpgrade && currentExperience >= experienceToNextLevel)
        {
            BeginLevelUp();
        }
    }

    private void BeginLevelUp()
    {
        currentExperience -= experienceToNextLevel;
        level++;
        experienceToNextLevel = Mathf.Max(
            experienceToNextLevel + 1,
            Mathf.CeilToInt(experienceToNextLevel * experienceGrowth)
        );

        BuildUpgradeChoices();
        if (choices.Count == 0)
        {
            Debug.LogWarning("選択できる強化がありません。");
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        choosingUpgrade = true;
    }

    private void BuildUpgradeChoices()
    {
        choices.Clear();
        List<UpgradeChoice> available = new List<UpgradeChoice>();

        if (playerAttack != null)
        {
            if (playerAttack.DamageUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.EnergyDamage, "エネルギー弾：威力強化", "エネルギー弾のダメージ +1"));

            if (playerAttack.FireRateUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.EnergyFireRate, "エネルギー弾：連射速度強化", "発射間隔を短縮"));

            if (playerAttack.ProjectileSpeedUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.EnergyProjectileSpeed, "エネルギー弾：弾速強化", "エネルギー弾が速くなる"));

            if (playerAttack.AttackRangeUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.EnergyAttackRange, "エネルギー弾：射程強化", "攻撃できる距離が伸びる"));
        }

        if (orbitingOrbAttack == null)
        {
            available.Add(new UpgradeChoice(UpgradeKind.UnlockOrbitingOrb, "新攻撃：回転オーブ", "プレイヤーの周囲を回るオーブで敵に接触ダメージを与える"));
        }
        else
        {
            if (orbitingOrbAttack.OrbCountUpgradeLevel < 3)
                available.Add(new UpgradeChoice(UpgradeKind.OrbCount, "回転オーブ：数を増加", "回転するオーブを1個追加"));

            if (orbitingOrbAttack.DamageUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.OrbDamage, "回転オーブ：威力強化", "接触ダメージ +1"));

            if (orbitingOrbAttack.SpeedUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.OrbSpeed, "回転オーブ：回転速度強化", "オーブの回転速度が上昇"));
        }

        if (shockwaveAttack == null)
        {
            available.Add(new UpgradeChoice(UpgradeKind.UnlockShockwave, "新攻撃：衝撃波", "一定間隔で周囲の敵に範囲ダメージを与える"));
        }
        else
        {
            if (shockwaveAttack.RadiusUpgradeLevel < 4)
                available.Add(new UpgradeChoice(UpgradeKind.ShockwaveRadius, "衝撃波：範囲拡大", "衝撃波が届く距離を拡大"));

            if (shockwaveAttack.DamageUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.ShockwaveDamage, "衝撃波：威力強化", "衝撃波のダメージ +1"));

            if (shockwaveAttack.CooldownUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.ShockwaveCooldown, "衝撃波：発動間隔短縮", "衝撃波がより短い間隔で発生"));
        }

        int count = Mathf.Min(3, available.Count);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, available.Count);
            choices.Add(available[index]);
            available.RemoveAt(index);
        }
    }

    private void SelectUpgrade(int index)
    {
        if (!choosingUpgrade || index < 0 || index >= choices.Count) return;

        UpgradeChoice choice = choices[index];
        bool applied = false;

        switch (choice.kind)
        {
            case UpgradeKind.EnergyDamage:
                applied = playerAttack != null && playerAttack.ApplyUpgrade(EnergyAttackUpgradeType.Damage);
                break;
            case UpgradeKind.EnergyFireRate:
                applied = playerAttack != null && playerAttack.ApplyUpgrade(EnergyAttackUpgradeType.FireRate);
                break;
            case UpgradeKind.EnergyProjectileSpeed:
                applied = playerAttack != null && playerAttack.ApplyUpgrade(EnergyAttackUpgradeType.ProjectileSpeed);
                break;
            case UpgradeKind.EnergyAttackRange:
                applied = playerAttack != null && playerAttack.ApplyUpgrade(EnergyAttackUpgradeType.AttackRange);
                break;
            case UpgradeKind.UnlockOrbitingOrb:
                if (orbitingOrbAttack == null)
                {
                    orbitingOrbAttack = gameObject.AddComponent<OrbitingOrbAttack>();
                    applied = true;
                }
                break;
            case UpgradeKind.OrbCount:
                applied = orbitingOrbAttack != null && orbitingOrbAttack.ApplyOrbCountUpgrade();
                break;
            case UpgradeKind.OrbDamage:
                applied = orbitingOrbAttack != null && orbitingOrbAttack.ApplyDamageUpgrade();
                break;
            case UpgradeKind.OrbSpeed:
                applied = orbitingOrbAttack != null && orbitingOrbAttack.ApplySpeedUpgrade();
                break;
            case UpgradeKind.UnlockShockwave:
                if (shockwaveAttack == null)
                {
                    shockwaveAttack = gameObject.AddComponent<ShockwaveAttack>();
                    applied = true;
                }
                break;
            case UpgradeKind.ShockwaveRadius:
                applied = shockwaveAttack != null && shockwaveAttack.ApplyRadiusUpgrade();
                break;
            case UpgradeKind.ShockwaveDamage:
                applied = shockwaveAttack != null && shockwaveAttack.ApplyDamageUpgrade();
                break;
            case UpgradeKind.ShockwaveCooldown:
                applied = shockwaveAttack != null && shockwaveAttack.ApplyCooldownUpgrade();
                break;
        }

        if (!applied)
        {
            Debug.LogWarning("強化を適用できませんでした。");
            return;
        }

        Debug.Log($"強化取得: {choice.title}");
        choosingUpgrade = false;
        choices.Clear();
        Time.timeScale = previousTimeScale;

        if (currentExperience >= experienceToNextLevel) BeginLevelUp();
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width / 2f - 125f, 10f, 250f, 48f),
            $"LEVEL {level}    EXP {currentExperience} / {experienceToNextLevel}");

        if (!choosingUpgrade) return;

        float panelWidth = 430f;
        float panelHeight = 285f;
        Rect panel = new Rect(
            (Screen.width - panelWidth) / 2f,
            (Screen.height - panelHeight) / 2f,
            panelWidth,
            panelHeight
        );

        GUILayout.BeginArea(panel, GUI.skin.window);
        GUILayout.Space(10);
        GUILayout.Label($"LEVEL UP!  レベル {level}", GUI.skin.box);
        GUILayout.Space(8);
        GUILayout.Label("強化する項目を1つ選んでください。");

        for (int i = 0; i < choices.Count; i++)
        {
            UpgradeChoice choice = choices[i];
            GUILayout.BeginVertical(GUI.skin.box);
            if (GUILayout.Button(choice.title, GUILayout.Height(38f))) SelectUpgrade(i);
            GUILayout.Label(choice.description);
            GUILayout.EndVertical();
        }

        GUILayout.EndArea();
    }

    private void OnDisable()
    {
        if (choosingUpgrade)
        {
            Time.timeScale = previousTimeScale;
            choosingUpgrade = false;
        }
    }
}
