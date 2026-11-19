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
        ShockwaveCooldown,
        UnlockLightning,
        LightningDamage,
        LightningChain,
        LightningCooldown,
        UnlockMeteor,
        MeteorCount,
        MeteorRadius,
        MeteorDamage,
        MeteorCooldown,
        EnergyBranch, OrbBranch, ShockwaveBranch, LightningBranch, MeteorBranch,
        EvolutionCore, EvolveEnergy, EvolveOrb, EvolveShockwave, EvolveLightning, EvolveMeteor, PlasmaFusion
    }

    private class UpgradeChoice
    {
        public UpgradeKind kind;
        public string title;
        public string description;
        public bool variant;

        public UpgradeChoice(UpgradeKind kind, string title, string description, bool variant = false)
        {
            this.kind = kind;
            this.title = title;
            this.description = description;
            this.variant = variant;
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
    private LightningAttack lightningAttack;
    private MeteorAttack meteorAttack;
    private readonly List<UpgradeChoice> choices = new List<UpgradeChoice>();
    private bool choosingUpgrade;
    private int evolutionCores;
    private float previousTimeScale = 1f;
    private Vector2 upgradeScrollPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug")]
    [SerializeField] private bool showDebugPanel;
    [SerializeField] private string debugExperienceInput = "10";
    private string debugMessage = "";
#endif

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

        lightningAttack = GetComponent<LightningAttack>();
        if (lightningAttack == null) lightningAttack = GetComponentInChildren<LightningAttack>();

        meteorAttack = GetComponent<MeteorAttack>();
        if (meteorAttack == null) meteorAttack = GetComponentInChildren<MeteorAttack>();
    }

    public void ResetForNewRun()
    {
        if (choosingUpgrade)
        {
            Time.timeScale = previousTimeScale;
            choosingUpgrade = false;
        }

        level = 1;
        currentExperience = 0;
        experienceToNextLevel = 5;
        choices.Clear();
        evolutionCores = 0;
        upgradeScrollPosition = Vector2.zero;

        if (playerAttack != null)
            playerAttack.ResetUpgrades();

        if (orbitingOrbAttack != null) Destroy(orbitingOrbAttack);
        if (shockwaveAttack != null) Destroy(shockwaveAttack);
        if (lightningAttack != null) Destroy(lightningAttack);
        if (meteorAttack != null) Destroy(meteorAttack);

        orbitingOrbAttack = null;
        shockwaveAttack = null;
        lightningAttack = null;
        meteorAttack = null;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        // デバッグ入力などで大きな値が加算されても整数オーバーフローしないようにする。
        currentExperience = amount > int.MaxValue - currentExperience
            ? int.MaxValue
            : currentExperience + amount;

        Debug.Log($"経験値 +{amount} ({currentExperience}/{experienceToNextLevel})");

        bool runHasFinished = GameSessionManager.Instance != null &&
            GameSessionManager.Instance.HasFinished;

        if (!choosingUpgrade && !runHasFinished && currentExperience >= experienceToNextLevel)
        {
            BeginLevelUp();
        }
    }

    private void BeginLevelUp()
    {
        currentExperience -= experienceToNextLevel;
        level++;
        SoundManager.Play(SoundManager.Effect.LevelUp);
        ScreenFlash.Trigger(new Color(0.2f, 0.9f, 1f), 0.35f, 0.3f);
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

        if (lightningAttack == null)
        {
            available.Add(new UpgradeChoice(UpgradeKind.UnlockLightning, "新攻撃：雷撃", "最も近い敵に雷を落とし、近くの敵へ連鎖ダメージ"));
        }
        else
        {
            if (lightningAttack.DamageUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.LightningDamage, "雷撃：威力強化", "雷撃のダメージ +1"));

            if (lightningAttack.ChainUpgradeLevel < 4)
                available.Add(new UpgradeChoice(UpgradeKind.LightningChain, "雷撃：連鎖数増加", "雷が連鎖する敵を1体追加"));

            if (lightningAttack.CooldownUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.LightningCooldown, "雷撃：発動間隔短縮", "雷撃がより短い間隔で発生"));
        }

        if (meteorAttack == null)
        {
            available.Add(new UpgradeChoice(UpgradeKind.UnlockMeteor, "新攻撃：メテオ", "敵が密集した場所に隕石を落とし、範囲ダメージを与える"));
        }
        else
        {
            if (meteorAttack.CountUpgradeLevel < 3)
                available.Add(new UpgradeChoice(UpgradeKind.MeteorCount, "メテオ：隕石数増加", "一度に落とす隕石を1個追加"));

            if (meteorAttack.RadiusUpgradeLevel < 4)
                available.Add(new UpgradeChoice(UpgradeKind.MeteorRadius, "メテオ：爆発範囲拡大", "爆発の範囲が広がる"));

            if (meteorAttack.DamageUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.MeteorDamage, "メテオ：威力強化", "メテオのダメージ +2"));

            if (meteorAttack.CooldownUpgradeLevel < 5)
                available.Add(new UpgradeChoice(UpgradeKind.MeteorCooldown, "メテオ：発動間隔短縮", "メテオがより短い間隔で落ちる"));
        }

        if (playerAttack != null && !playerAttack.BranchChosen)
        {
            available.Add(new UpgradeChoice(UpgradeKind.EnergyBranch, "エネルギー弾：連射型", "発射間隔を大幅に短縮する", true));
            available.Add(new UpgradeChoice(UpgradeKind.EnergyBranch, "エネルギー弾：重撃型", "弾の威力と弾速を大きく上げる", false));
        }
        if (orbitingOrbAttack != null && !orbitingOrbAttack.BranchChosen)
        {
            available.Add(new UpgradeChoice(UpgradeKind.OrbBranch, "回転オーブ：群体型", "オーブを2個追加する", true));
            available.Add(new UpgradeChoice(UpgradeKind.OrbBranch, "回転オーブ：高威力型", "オーブの威力と軌道半径を上げる", false));
        }
        if (shockwaveAttack != null && !shockwaveAttack.BranchChosen)
        {
            available.Add(new UpgradeChoice(UpgradeKind.ShockwaveBranch, "衝撃波：広範囲型", "攻撃範囲を大きく広げる", true));
            available.Add(new UpgradeChoice(UpgradeKind.ShockwaveBranch, "衝撃波：高速発動型", "発動間隔を大幅に短縮する", false));
        }
        if (lightningAttack != null && !lightningAttack.BranchChosen)
        {
            available.Add(new UpgradeChoice(UpgradeKind.LightningBranch, "雷撃：連鎖型", "連鎖する敵の数を3体増やす", true));
            available.Add(new UpgradeChoice(UpgradeKind.LightningBranch, "雷撃：高威力型", "雷撃の威力を大きく上げる", false));
        }
        if (meteorAttack != null && !meteorAttack.BranchChosen)
        {
            available.Add(new UpgradeChoice(UpgradeKind.MeteorBranch, "メテオ：隕石雨型", "一度に落とす隕石を2個追加する", true));
            available.Add(new UpgradeChoice(UpgradeKind.MeteorBranch, "メテオ：大爆発型", "爆発範囲を大きく広げる", false));
        }

        if (evolutionCores < 3)
            available.Add(new UpgradeChoice(UpgradeKind.EvolutionCore, "進化アイテム：進化コア", "攻撃の進化に必要なコアを1個獲得"));

        if (evolutionCores > 0)
        {
            if (playerAttack != null && playerAttack.BranchChosen && playerAttack.TotalUpgradeLevel >= 2 && !playerAttack.IsEvolved)
                available.Add(new UpgradeChoice(UpgradeKind.EvolveEnergy, "進化：レールエネルギー", "エネルギー弾の威力・弾速・連射性能が上昇"));
            if (orbitingOrbAttack != null && orbitingOrbAttack.BranchChosen && orbitingOrbAttack.TotalUpgradeLevel >= 2 && !orbitingOrbAttack.IsEvolved)
                available.Add(new UpgradeChoice(UpgradeKind.EvolveOrb, "進化：オービタルストーム", "オーブの威力・軌道半径・回転速度が上昇"));
            if (shockwaveAttack != null && shockwaveAttack.BranchChosen && shockwaveAttack.TotalUpgradeLevel >= 2 && !shockwaveAttack.IsEvolved)
                available.Add(new UpgradeChoice(UpgradeKind.EvolveShockwave, "進化：ノヴァウェーブ", "衝撃波の範囲・威力・発動速度が大幅上昇"));
            if (lightningAttack != null && lightningAttack.BranchChosen && lightningAttack.TotalUpgradeLevel >= 2 && !lightningAttack.IsEvolved)
                available.Add(new UpgradeChoice(UpgradeKind.EvolveLightning, "進化：チェインテンペスト", "雷撃の威力と連鎖数が大幅上昇"));
            if (meteorAttack != null && meteorAttack.BranchChosen && meteorAttack.TotalUpgradeLevel >= 2 && !meteorAttack.IsEvolved)
                available.Add(new UpgradeChoice(UpgradeKind.EvolveMeteor, "進化：メテオレイン", "隕石の威力と爆発範囲が大幅上昇"));
            if (playerAttack != null && lightningAttack != null && playerAttack.BranchChosen && lightningAttack.BranchChosen &&
                playerAttack.TotalUpgradeLevel >= 2 && lightningAttack.TotalUpgradeLevel >= 2 &&
                !playerAttack.IsEvolved && !lightningAttack.IsEvolved)
                available.Add(new UpgradeChoice(UpgradeKind.PlasmaFusion, "特殊進化：プラズマキャノン", "エネルギー弾と雷撃を融合。両方の攻撃が同時に進化する"));
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
            case UpgradeKind.UnlockLightning:
                if (lightningAttack == null)
                {
                    lightningAttack = gameObject.AddComponent<LightningAttack>();
                    applied = true;
                }
                break;
            case UpgradeKind.LightningDamage:
                applied = lightningAttack != null && lightningAttack.ApplyDamageUpgrade();
                break;
            case UpgradeKind.LightningChain:
                applied = lightningAttack != null && lightningAttack.ApplyChainUpgrade();
                break;
            case UpgradeKind.LightningCooldown:
                applied = lightningAttack != null && lightningAttack.ApplyCooldownUpgrade();
                break;
            case UpgradeKind.UnlockMeteor:
                if (meteorAttack == null)
                {
                    meteorAttack = gameObject.AddComponent<MeteorAttack>();
                    applied = true;
                }
                break;
            case UpgradeKind.MeteorCount:
                applied = meteorAttack != null && meteorAttack.ApplyCountUpgrade();
                break;
            case UpgradeKind.MeteorRadius:
                applied = meteorAttack != null && meteorAttack.ApplyRadiusUpgrade();
                break;
            case UpgradeKind.MeteorDamage:
                applied = meteorAttack != null && meteorAttack.ApplyDamageUpgrade();
                break;
            case UpgradeKind.MeteorCooldown:
                applied = meteorAttack != null && meteorAttack.ApplyCooldownUpgrade();
                break;
            case UpgradeKind.EnergyBranch: applied = playerAttack != null && playerAttack.ApplyBranchUpgrade(choice.variant); break;
            case UpgradeKind.OrbBranch: applied = orbitingOrbAttack != null && orbitingOrbAttack.ApplyBranchUpgrade(choice.variant); break;
            case UpgradeKind.ShockwaveBranch: applied = shockwaveAttack != null && shockwaveAttack.ApplyBranchUpgrade(choice.variant); break;
            case UpgradeKind.LightningBranch: applied = lightningAttack != null && lightningAttack.ApplyBranchUpgrade(choice.variant); break;
            case UpgradeKind.MeteorBranch: applied = meteorAttack != null && meteorAttack.ApplyBranchUpgrade(choice.variant); break;
            case UpgradeKind.EvolutionCore:
                if (evolutionCores < 3) { evolutionCores++; applied = true; }
                break;
            case UpgradeKind.EvolveEnergy: applied = playerAttack != null && playerAttack.ApplyEvolution(); if (applied) evolutionCores--; break;
            case UpgradeKind.EvolveOrb: applied = orbitingOrbAttack != null && orbitingOrbAttack.ApplyEvolution(); if (applied) evolutionCores--; break;
            case UpgradeKind.EvolveShockwave: applied = shockwaveAttack != null && shockwaveAttack.ApplyEvolution(); if (applied) evolutionCores--; break;
            case UpgradeKind.EvolveLightning: applied = lightningAttack != null && lightningAttack.ApplyEvolution(); if (applied) evolutionCores--; break;
            case UpgradeKind.EvolveMeteor: applied = meteorAttack != null && meteorAttack.ApplyEvolution(); if (applied) evolutionCores--; break;
            case UpgradeKind.PlasmaFusion:
                if (playerAttack != null && lightningAttack != null && evolutionCores > 0)
                {
                    bool energyOk = playerAttack.ApplyEvolution();
                    bool lightningOk = lightningAttack.ApplyEvolution();
                    applied = energyOk && lightningOk;
                    if (applied)
                    {
                        playerAttack.ApplyPlasmaFusionBonus();
                        lightningAttack.ApplyPlasmaFusionBonus();
                        evolutionCores--;
                    }
                }
                break;
        }

        if (!applied)
        {
            Debug.LogWarning("強化を適用できませんでした。");
            return;
        }

        bool isEvolution = choice.kind == UpgradeKind.EvolveEnergy ||
            choice.kind == UpgradeKind.EvolveOrb || choice.kind == UpgradeKind.EvolveShockwave ||
            choice.kind == UpgradeKind.EvolveLightning || choice.kind == UpgradeKind.EvolveMeteor ||
            choice.kind == UpgradeKind.PlasmaFusion;
        SoundManager.Play(isEvolution ? SoundManager.Effect.Evolution : SoundManager.Effect.Upgrade);
        Debug.Log($"強化取得: {choice.title}");
        choosingUpgrade = false;
        choices.Clear();
        Time.timeScale = previousTimeScale;

        if (currentExperience >= experienceToNextLevel) BeginLevelUp();
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width / 2f - 155f, 10f, 310f, 48f),
            $"LEVEL {level}    EXP {currentExperience} / {experienceToNextLevel}    CORE {evolutionCores}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DrawDebugPanel();
#endif

        if (!choosingUpgrade) return;

        float panelWidth = Mathf.Min(500f, Screen.width - 30f);
        float panelHeight = Mathf.Min(460f, Screen.height - 30f);
        Rect panel = new Rect(
            (Screen.width - panelWidth) / 2f,
            (Screen.height - panelHeight) / 2f,
            panelWidth,
            panelHeight
        );

        GUILayout.BeginArea(panel, GUI.skin.window);
        GUILayout.Space(8);
        GUILayout.Label($"LEVEL UP!  レベル {level}", GUI.skin.box);
        GUILayout.Space(6);
        GUILayout.Label("強化する項目を1つ選んでください。");

        float scrollHeight = Mathf.Max(100f, panelHeight - 105f);
        upgradeScrollPosition = GUILayout.BeginScrollView(
            upgradeScrollPosition,
            GUILayout.Height(scrollHeight)
        );

        for (int i = 0; i < choices.Count; i++)
        {
            UpgradeChoice choice = choices[i];
            GUILayout.BeginVertical(GUI.skin.box);
            if (GUILayout.Button(choice.title, GUILayout.Height(38f)))
            {
                SelectUpgrade(i);
            }
            GUILayout.Label(choice.description, GUILayout.ExpandHeight(false));
            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void DrawDebugPanel()
    {
        const float panelWidth = 260f;
        const float panelHeight = 185f;

        Rect toggleRect = new Rect(Screen.width - 105f, 10f, 95f, 28f);
        if (GUI.Button(toggleRect, showDebugPanel ? "DEBUG ▼" : "DEBUG ▶"))
        {
            showDebugPanel = !showDebugPanel;
        }

        if (!showDebugPanel) return;

        Rect panelRect = new Rect(
            Screen.width - panelWidth - 10f,
            43f,
            panelWidth,
            panelHeight
        );

        GUILayout.BeginArea(panelRect, GUI.skin.window);
        GUILayout.Label("経験値デバッグ");
        GUILayout.Label($"現在：{currentExperience} / 次のレベル：{experienceToNextLevel}");
        GUILayout.Label("設定する経験値（0以上）");
        debugExperienceInput = GUILayout.TextField(debugExperienceInput);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("経験値を設定"))
        {
            SetDebugExperience();
        }
        if (GUILayout.Button("経験値を加算"))
        {
            AddDebugExperience();
        }
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(debugMessage))
        {
            GUILayout.Label(debugMessage);
        }

        GUILayout.Space(6f);
        GUILayout.Label("タイマー操作は画面左下のDEBUG TIMEから行えます。");
        GUILayout.EndArea();
    }

    private bool TryGetDebugExperience(out int value)
    {
        if (int.TryParse(debugExperienceInput, out value) && value >= 0)
        {
            return true;
        }

        debugMessage = "0以上の整数を入力してください。";
        return false;
    }

    private void SetDebugExperience()
    {
        if (!TryGetDebugExperience(out int value)) return;

        currentExperience = value;
        debugMessage = $"経験値を {value} に設定しました。";

        if (!choosingUpgrade && currentExperience >= experienceToNextLevel)
        {
            BeginLevelUp();
        }
    }

    private void AddDebugExperience()
    {
        if (!TryGetDebugExperience(out int value)) return;
        if (value == 0)
        {
            debugMessage = "加算する経験値を1以上にしてください。";
            return;
        }

        AddExperience(value);
        debugMessage = $"経験値を {value} 加算しました。";
    }
#endif

    private void OnDisable()
    {
        if (choosingUpgrade)
        {
            Time.timeScale = previousTimeScale;
            choosingUpgrade = false;
        }
    }
}
