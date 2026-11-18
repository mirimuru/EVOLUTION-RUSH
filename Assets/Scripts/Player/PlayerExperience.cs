using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの経験値・レベル・レベルアップ時の強化選択を管理する。
/// </summary>
public class PlayerExperience : MonoBehaviour
{
    private class UpgradeChoice
    {
        public EnergyAttackUpgradeType type;
        public string title;
        public string description;

        public UpgradeChoice(EnergyAttackUpgradeType type, string title, string description)
        {
            this.type = type;
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
    private readonly List<UpgradeChoice> choices = new List<UpgradeChoice>();
    private bool choosingUpgrade;
    private float previousTimeScale = 1f;

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    private void Awake()
    {
        playerAttack = GetComponent<PlayerAutoAttack>();
        if (playerAttack == null)
        {
            playerAttack = GetComponentInChildren<PlayerAutoAttack>();
        }
    }

    /// <summary>
    /// 経験値オーブを取得したときに呼び出す。
    /// </summary>
    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

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

        if (playerAttack == null)
        {
            return;
        }

        List<UpgradeChoice> available = new List<UpgradeChoice>();

        if (playerAttack.DamageUpgradeLevel < 5)
            available.Add(new UpgradeChoice(EnergyAttackUpgradeType.Damage, "威力強化", "エネルギー弾のダメージ +1"));

        if (playerAttack.FireRateUpgradeLevel < 5)
            available.Add(new UpgradeChoice(EnergyAttackUpgradeType.FireRate, "連射速度強化", "エネルギー弾の発射間隔を短縮"));

        if (playerAttack.ProjectileSpeedUpgradeLevel < 5)
            available.Add(new UpgradeChoice(EnergyAttackUpgradeType.ProjectileSpeed, "弾速強化", "エネルギー弾が速くなる"));

        if (playerAttack.AttackRangeUpgradeLevel < 5)
            available.Add(new UpgradeChoice(EnergyAttackUpgradeType.AttackRange, "射程強化", "攻撃できる距離が伸びる"));

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
        if (!choosingUpgrade || index < 0 || index >= choices.Count || playerAttack == null)
        {
            return;
        }

        playerAttack.ApplyUpgrade(choices[index].type);
        Debug.Log($"強化取得: {choices[index].title}");

        choosingUpgrade = false;
        choices.Clear();
        Time.timeScale = previousTimeScale;

        // 経験値を一度に多く取得していた場合、続けてレベルアップする。
        if (currentExperience >= experienceToNextLevel)
        {
            BeginLevelUp();
        }
    }

    private void OnGUI()
    {
        // 画面上部にレベルと経験値を表示する。
        GUI.Box(new Rect(Screen.width / 2f - 125f, 10f, 250f, 48f),
            $"LEVEL {level}    EXP {currentExperience} / {experienceToNextLevel}");

        if (!choosingUpgrade)
        {
            return;
        }

        float panelWidth = 390f;
        float panelHeight = 250f;
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
            if (GUILayout.Button(choice.title, GUILayout.Height(38f)))
            {
                SelectUpgrade(i);
            }
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
