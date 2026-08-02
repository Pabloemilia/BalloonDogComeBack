using UnityEngine;

/// <summary>
/// Harici ses dosyası olmadan hafif prototip sesleri üretir.
/// Daha sonra gerçek sesler aynı çağrı noktalarına bağlanabilir.
/// </summary>
public sealed class GameAudioController : MonoBehaviour
{
    private static GameAudioController instance;
    private AudioSource source;
    private AudioClip pickupClip;
    private AudioClip hitClip;
    private AudioClip transformClip;
    private AudioClip finishClip;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        source = gameObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        pickupClip = BuildTone("Pickup", 740f, 0.12f, 0.18f, 1.55f);
        hitClip = BuildTone("Hit", 120f, 0.16f, 0.24f, 0.55f);
        transformClip = BuildTone("Transform", 320f, 0.2f, 0.2f, 1.8f);
        finishClip = BuildTone("Finish", 520f, 0.32f, 0.2f, 1.7f);
    }

    public static void PlayPickup() => Play(instance != null ? instance.pickupClip : null, 0.65f);
    public static void PlayHit() => Play(instance != null ? instance.hitClip : null, 0.75f);
    public static void PlayTransform() => Play(instance != null ? instance.transformClip : null, 0.65f);
    public static void PlayFinish() => Play(instance != null ? instance.finishClip : null, 0.8f);

    private static void Play(AudioClip clip, float volume)
    {
        if (instance != null && instance.source != null && clip != null)
        {
            instance.source.PlayOneShot(clip, volume);
        }
    }

    private static AudioClip BuildTone(
        string clipName,
        float frequency,
        float duration,
        float volume,
        float frequencyEndMultiplier)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float normalized = i / (float)sampleCount;
            float currentFrequency = Mathf.Lerp(frequency, frequency * frequencyEndMultiplier, normalized);
            float envelope = Mathf.Sin(normalized * Mathf.PI) * (1f - normalized * 0.35f);
            samples[i] = Mathf.Sin(t * currentFrequency * Mathf.PI * 2f) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
