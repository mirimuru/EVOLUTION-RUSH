using UnityEngine;

/// <summary>
/// 電子音楽のBGMをC#から合成してループ再生する。音声ファイルやInspector設定は不要。
/// 通常時とボス戦（残り30秒）で別のループを切り替える。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [SerializeField] private float normalVolume = 0.22f;
    [SerializeField] private float pausedVolume = 0.10f;
    [SerializeField] private float finishedVolume = 0.06f;

    private const float TempoBpm = 128f;
    private const int Bars = 8;

    private AudioSource audioSource;
    private AudioClip normalClip;
    private AudioClip bossClip;
    private uint noiseState = 12345u;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        if (FindFirstObjectByType<MusicManager>() != null) return;
        new GameObject("MusicManager").AddComponent<MusicManager>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;

        int rate = AudioSettings.outputSampleRate;
        if (rate <= 0) rate = 44100;

        normalClip = BuildLoop(rate, false);
        bossClip = BuildLoop(rate, true);

        audioSource.clip = normalClip;
        audioSource.Play();
    }

    private void Update()
    {
        GameSessionManager session = GameSessionManager.Instance;
        bool finished = session != null && session.HasFinished;
        bool bossPhase = session != null && session.BossPhaseStarted && !finished;

        AudioClip desired = bossPhase ? bossClip : normalClip;
        if (audioSource.clip != desired)
        {
            audioSource.clip = desired;
            audioSource.Play();
        }

        // 終了後は小さく、レベルアップ選択などで時間が止まっている間は控えめにする。
        float target = finished ? finishedVolume : (Time.timeScale <= 0f ? pausedVolume : normalVolume);
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, target, Time.unscaledDeltaTime * 0.5f);
    }

    private void OnDestroy()
    {
        if (normalClip != null) Destroy(normalClip);
        if (bossClip != null) Destroy(bossClip);
    }

    // ---- 合成 ----

    // 和音進行：Am - F - C - G（各2小節）。値はルート音のMIDIノート番号と、3和音の構成音。
    private static readonly int[] ChordRoots = { 57, 53, 48, 55 };
    private static readonly int[][] ChordOffsets =
    {
        new[] { 0, 3, 7 },
        new[] { 0, 4, 7 },
        new[] { 0, 4, 7 },
        new[] { 0, 4, 7 }
    };
    private static readonly int[] ArpPattern = { 0, 1, 2, 1, 2, 1, 0, 1 };

    private AudioClip BuildLoop(int rate, bool boss)
    {
        float stepSeconds = 60f / TempoBpm / 4f;      // 16分音符
        int stepSamples = Mathf.RoundToInt(stepSeconds * rate);
        int totalSteps = Bars * 16;
        int length = stepSamples * totalSteps;
        float[] buffer = new float[length];
        noiseState = boss ? 987654u : 12345u;

        for (int step = 0; step < totalSteps; step++)
        {
            int start = step * stepSamples;
            int bar = step / 16;
            int chord = bar / 2;
            int root = ChordRoots[chord];
            int[] offsets = ChordOffsets[chord];

            // キック：4分音符ごと
            if (step % 4 == 0) AddKick(buffer, start, rate, 0.9f);

            // スネア：2拍目・4拍目
            if (step % 8 == 4) AddNoise(buffer, start, rate, 0.14f, 0.28f, 28f);

            // ハイハット：通常は8分の裏、ボス戦は16分すべて
            if (boss ? true : step % 4 == 2) AddNoise(buffer, start, rate, 0.04f, boss ? 0.10f : 0.13f, 70f);

            // ベース：8分音符ごと（ボス戦はオクターブを交互に）
            if (step % 2 == 0)
            {
                int bassNote = root - 24 + (boss && (step / 2) % 2 == 1 ? 12 : 0);
                AddBass(buffer, start, rate, MidiToHz(bassNote), stepSeconds * 1.9f, 0.34f);
            }

            // アルペジオ：16分音符ごと
            int arpIndex = ArpPattern[step % ArpPattern.Length];
            int arpNote = root + 12 + offsets[arpIndex];
            AddLead(buffer, start, rate, MidiToHz(arpNote), stepSeconds * 1.6f, boss ? 0.16f : 0.12f);

            // ボス戦のみ：小節の頭に高いリード音を重ねる
            if (boss && step % 16 == 0)
            {
                AddLead(buffer, start, rate, MidiToHz(root + 24 + offsets[2]), stepSeconds * 7f, 0.13f);
            }
        }

        Normalize(buffer, 0.8f);

        AudioClip clip = AudioClip.Create(boss ? "BGM_Boss" : "BGM_Normal", length, 1, rate, false);
        clip.SetData(buffer, 0);
        return clip;
    }

    private static float MidiToHz(int note)
    {
        return 440f * Mathf.Pow(2f, (note - 69) / 12f);
    }

    // バッファの末尾を超えた分は先頭へ回り込ませ、ループの継ぎ目でぷつっと鳴らないようにする。
    private static void Mix(float[] buffer, int index, float value)
    {
        buffer[index % buffer.Length] += value;
    }

    private void AddKick(float[] buffer, int start, int rate, float amplitude)
    {
        int count = Mathf.RoundToInt(0.20f * rate);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float frequency = Mathf.Lerp(130f, 42f, Mathf.Clamp01(t / 0.12f));
            phase += 2f * Mathf.PI * frequency / rate;
            float envelope = Mathf.Exp(-t * 16f);
            Mix(buffer, start + i, Mathf.Sin(phase) * envelope * amplitude);
        }
    }

    private void AddNoise(float[] buffer, int start, int rate, float duration, float amplitude, float decay)
    {
        int count = Mathf.RoundToInt(duration * rate);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            Mix(buffer, start + i, NextNoise() * Mathf.Exp(-t * decay) * amplitude);
        }
    }

    private void AddBass(float[] buffer, int start, int rate, float frequency, float duration, float amplitude)
    {
        int count = Mathf.RoundToInt(duration * rate);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            phase += frequency / rate;
            float saw = 2f * (phase - Mathf.Floor(phase + 0.5f));
            float sine = Mathf.Sin(2f * Mathf.PI * phase);
            float envelope = Mathf.Exp(-t * 4.5f) * Mathf.Min(1f, t * 400f);
            Mix(buffer, start + i, (sine * 0.7f + saw * 0.3f) * envelope * amplitude);
        }
    }

    private void AddLead(float[] buffer, int start, int rate, float frequency, float duration, float amplitude)
    {
        int count = Mathf.RoundToInt(duration * rate);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            phase += frequency / rate;
            float saw = 2f * (phase - Mathf.Floor(phase + 0.5f));
            float square = phase - Mathf.Floor(phase) < 0.5f ? 1f : -1f;
            float envelope = Mathf.Exp(-t * 9f) * Mathf.Min(1f, t * 600f);
            Mix(buffer, start + i, (saw * 0.55f + square * 0.25f) * envelope * amplitude);
        }
    }

    private float NextNoise()
    {
        noiseState = noiseState * 1664525u + 1013904223u;
        return ((noiseState >> 8) & 0xFFFF) / 32768f - 1f;
    }

    private static void Normalize(float[] buffer, float targetPeak)
    {
        float peak = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float magnitude = Mathf.Abs(buffer[i]);
            if (magnitude > peak) peak = magnitude;
        }

        if (peak < 0.0001f) return;

        float gain = targetPeak / peak;
        for (int i = 0; i < buffer.Length; i++) buffer[i] *= gain;
    }
}
