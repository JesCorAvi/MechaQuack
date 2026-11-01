using UnityEngine;

/// <summary>
/// Generador procedural de efectos de sonido (Web Audio API equivalente en Unity).
/// Sintetiza formas de onda en tiempo de ejecución para no requerir ningún asset externo.
/// </summary>
public class ProceduralAudio : MonoBehaviour
{
    private static ProceduralAudio _instance;
    public static ProceduralAudio Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindFirstObjectByType<ProceduralAudio>();
            return _instance;
        }
        private set => _instance = value;
    }

    private AudioSource audioSource;

    private AudioClip clipQuack;
    private AudioClip clipJump;
    private AudioClip clipStomp;
    private AudioClip clipButtonClick;
    private AudioClip clipMechaStep;
    private AudioClip clipPunch;
    private AudioClip clipLaser;
    private AudioClip clipExplosion;
    private AudioClip clipAlarm;
    private AudioClip clipAcidSizzle;
    private AudioClip clipHydraulicRecover;
    private AudioClip clipGatling;
    private AudioClip clipMissileLaunch;
    private AudioClip clipAimServo;
    private AudioClip clipTargetLock;
    private AudioClip clipCrashLand;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D para claridad cómica del HUD

        GenerateAllClips();
    }

    private void GenerateAllClips()
    {
        clipQuack = GenerateQuackClip();
        clipJump = GenerateToneSweepClip(220f, 560f, 0.14f, WaveType.Sine, 0.35f);
        clipStomp = GenerateToneSweepClip(180f, 42f, 0.22f, WaveType.Triangle, 0.7f);
        clipButtonClick = GenerateClickClip();
        clipMechaStep = GenerateToneSweepClip(90f, 26f, 0.2f, WaveType.Sawtooth, 0.45f);
        clipPunch = GeneratePunchClip();
        clipLaser = GenerateToneSweepClip(1300f, 150f, 0.45f, WaveType.Sawtooth, 0.5f);
        clipExplosion = GenerateExplosionClip();
        clipAlarm = GenerateToneSweepClip(880f, 440f, 0.28f, WaveType.Sawtooth, 0.55f);
        clipAcidSizzle = GenerateToneSweepClip(600f, 1200f, 0.25f, WaveType.Noise, 0.5f);
        clipHydraulicRecover = GenerateToneSweepClip(60f, 320f, 0.35f, WaveType.Sawtooth, 0.8f);
        clipGatling = GenerateToneSweepClip(360f, 65f, 0.08f, WaveType.Sawtooth, 0.65f);
        clipMissileLaunch = GenerateToneSweepClip(140f, 620f, 0.28f, WaveType.Noise, 0.75f);
        clipAimServo = GenerateToneSweepClip(420f, 340f, 0.05f, WaveType.Triangle, 0.25f);
        clipTargetLock = GenerateToneSweepClip(780f, 1560f, 0.16f, WaveType.Sine, 0.55f);
        clipCrashLand = GenerateToneSweepClip(140f, 32f, 0.42f, WaveType.Sawtooth, 0.95f);
    }

    public void PlayQuack() => PlayClip(clipQuack, 0.9f);
    public void PlayJump() => PlayClip(clipJump, 0.5f);
    public void PlayStomp() => PlayClip(clipStomp, 0.9f);
    public void PlayButtonClick() => PlayClip(clipButtonClick, 0.6f);
    public void PlayMechaStep() => PlayClip(clipMechaStep, 0.45f);
    public void PlayPunch() => PlayClip(clipPunch, 0.85f);
    public void PlayLaser() => PlayClip(clipLaser, 0.75f);
    public void PlayGatlingFire() => PlayClip(clipGatling, 0.65f);
    public void PlayMissileLaunch() => PlayClip(clipMissileLaunch, 0.85f);
    public void PlayAimServoTick() => PlayClip(clipAimServo, 0.35f);
    public void PlayTargetLock() => PlayClip(clipTargetLock, 0.75f);
    public void PlayCrashLand() => PlayClip(clipCrashLand, 0.95f);
    public void PlayExplosion() => PlayClip(clipExplosion, 0.8f);
    public void PlayAlarm() => PlayClip(clipAlarm, 0.75f);
    public void PlayAcidSizzle() => PlayClip(clipAcidSizzle, 0.65f);
    public void PlayHydraulicRecover() => PlayClip(clipHydraulicRecover, 0.9f);

    private void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            // Variación sutil de tono (SFX variation según audio-design) para evitar fatiga auditiva
            audioSource.pitch = 1.0f + Random.Range(-0.05f, 0.05f);
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private enum WaveType { Sine, Sawtooth, Triangle, Noise }

    private AudioClip GenerateToneSweepClip(float startFreq, float endFreq, float duration, WaveType type, float volume = 0.5f)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float val = 0f;
            switch (type)
            {
                case WaveType.Sine:
                    val = Mathf.Sin(phase);
                    break;
                case WaveType.Sawtooth:
                    val = (float)(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f)));
                    break;
                case WaveType.Triangle:
                    val = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
                    break;
                case WaveType.Noise:
                    val = Random.Range(-1f, 1f);
                    break;
            }

            // Envolvente de decaimiento
            float envelope = Mathf.Clamp01(1f - t);
            samples[i] = val * envelope * volume;
        }

        AudioClip clip = AudioClip.Create("Synth_" + type, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateQuackClip()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Frecuencia nasal ascendente y luego descendente
            float freq = t < 0.25f ? Mathf.Lerp(340f, 520f, t / 0.25f) : Mathf.Lerp(520f, 220f, (t - 0.25f) / 0.75f);
            phase += 2f * Mathf.PI * freq / sampleRate;

            // Diente de sierra con modulación armónica (efecto graznido)
            float saw = (float)(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f)));
            float harmonic = Mathf.Sin(phase * 2.3f) * 0.4f;
            float val = (saw + harmonic) * 0.7f;

            float env = t < 0.1f ? t / 0.1f : Mathf.Pow(1f - t, 1.4f);
            samples[i] = val * env * 0.65f;
        }

        AudioClip clip = AudioClip.Create("Synth_Quack", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateClickClip()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = t < 0.4f ? 640f : 880f;
            phase += 2f * Mathf.PI * freq / sampleRate;
            float val = Mathf.Sin(phase) > 0f ? 0.8f : -0.8f; // Square wave
            float env = 1f - t;
            samples[i] = val * env * 0.2f;
        }

        AudioClip clip = AudioClip.Create("Synth_Click", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GeneratePunchClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(160f, 30f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float saw = (float)(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f)));
            float noise = Random.Range(-0.3f, 0.3f);
            float env = Mathf.Pow(1f - t, 1.8f);
            samples[i] = (saw + noise) * env * 0.75f;
        }

        AudioClip clip = AudioClip.Create("Synth_Punch", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateExplosionClip()
    {
        int sampleRate = 44100;
        float duration = 0.55f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float noise = Random.Range(-1f, 1f);
            float env = Mathf.Pow(1f - t, 2.2f);
            samples[i] = noise * env * 0.6f;
        }

        AudioClip clip = AudioClip.Create("Synth_Explosion", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
