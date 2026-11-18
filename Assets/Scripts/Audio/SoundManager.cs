using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public enum Effect
    {
        PlayerShoot, EnemyHit, EnemyDefeated, PlayerDamaged, LevelUp,
        Upgrade, Evolution, BossSpawn, BossAttack, Victory, GameOver
    }

    private static SoundManager instance;
    private AudioSource audioSource;
    private AudioClip[] clips;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance() => EnsureInstance();

    private static void EnsureInstance()
    {
        if (instance != null) return;
        SoundManager existing = FindFirstObjectByType<SoundManager>();
        if (existing != null) { instance = existing; return; }
        instance = new GameObject("SoundManager").AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.38f;
        audioSource.dopplerLevel = 0f;
        BuildClips();
    }

    public static void Play(Effect effect)
    {
        EnsureInstance();
        if (instance == null || instance.audioSource == null || instance.clips == null) return;
        int index = (int)effect;
        if (index >= 0 && index < instance.clips.Length && instance.clips[index] != null)
            instance.audioSource.PlayOneShot(instance.clips[index]);
    }

    private void BuildClips()
    {
        clips = new AudioClip[System.Enum.GetValues(typeof(Effect)).Length];
        int rate = AudioSettings.outputSampleRate;
        if (rate <= 0) rate = 44100;
        CreateClip(Effect.PlayerShoot, rate, 0.10f, 920f, 360f, 0.20f, 0f);
        CreateClip(Effect.EnemyHit, rate, 0.075f, 520f, 260f, 0.32f, 0.04f);
        CreateClip(Effect.EnemyDefeated, rate, 0.22f, 620f, 1180f, 0.18f, 0.02f);
        CreateClip(Effect.PlayerDamaged, rate, 0.24f, 190f, 72f, 0.55f, 0.12f);
        CreateClip(Effect.LevelUp, rate, 0.48f, 520f, 1040f, 0.12f, 0f);
        CreateClip(Effect.Upgrade, rate, 0.20f, 680f, 880f, 0.12f, 0f);
        CreateClip(Effect.Evolution, rate, 0.72f, 330f, 1320f, 0.16f, 0f);
        CreateClip(Effect.BossSpawn, rate, 0.95f, 110f, 55f, 0.62f, 0.18f);
        CreateClip(Effect.BossAttack, rate, 0.32f, 150f, 65f, 0.62f, 0.22f);
        CreateClip(Effect.Victory, rate, 0.85f, 440f, 1320f, 0.10f, 0f);
        CreateClip(Effect.GameOver, rate, 0.75f, 260f, 65f, 0.50f, 0.08f);
    }

    private void CreateClip(Effect effect, int rate, float duration, float startFrequency,
        float endFrequency, float noiseAmount, float subAmount)
    {
        int count = Mathf.Max(1, Mathf.CeilToInt(rate * duration));
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            if (effect == Effect.LevelUp || effect == Effect.Evolution || effect == Effect.Victory)
            {
                int note = Mathf.Min(3, Mathf.FloorToInt(t * 4f));
                float[] intervals = effect == Effect.LevelUp
                    ? new float[] { 1f, 1.25f, 1.5f, 2f }
                    : new float[] { 1f, 1.5f, 2f, 3f };
                frequency = startFrequency * intervals[note];
            }

            float phase = 2f * Mathf.PI * frequency * i / rate;
            float tone = Mathf.Sin(phase) + 0.22f * Mathf.Sin(phase * 2f);
            float sub = Mathf.Sin(phase * 0.5f) * subAmount;
            float seed = Mathf.Sin(i * 12.9898f + (int)effect * 78.233f) * 43758.5453f;
            float noise = (seed - Mathf.Floor(seed)) * 2f - 1f;
            float envelope = Mathf.Pow(1f - t,
                effect == Effect.BossSpawn || effect == Effect.GameOver ? 0.65f : 1.8f);
            data[i] = (tone * (1f - noiseAmount) + noise * noiseAmount + sub) * envelope * 0.32f;
        }

        AudioClip clip = AudioClip.Create("SFX_" + effect, count, 1, rate, false);
        clip.SetData(data, 0);
        clips[(int)effect] = clip;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (clips == null) return;
        foreach (AudioClip clip in clips) if (clip != null) Destroy(clip);
    }
}
