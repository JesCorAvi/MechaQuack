using UnityEngine;

/// <summary>
/// Retroalimentación de pisada limpia y discreta:
/// Reproduce el sonido de pisada sin partículas toscas ni temblores molestos en la cámara.
/// </summary>
public class FootstepFX : MonoBehaviour
{
    [Header("Referencias de Patas")]
    [SerializeField] private ProceduralLeg leftLeg;
    [SerializeField] private ProceduralLeg rightLeg;

    [Header("Efectos Opcionales")]
    [Tooltip("Prefab de partículas personalizado (opcional; si está vacío, no emite nada)")]
    [SerializeField] private ParticleSystem optionalDustPrefab;
    [Tooltip("Intensidad de temblor de cámara por pisada (0 = desactivado)")]
    [SerializeField] private float stepShakeIntensity = 0f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stepAudioClip;
    [Range(0.7f, 1.3f)][SerializeField] private float pitchVariation = 0.08f;
    [Range(0f, 1f)][SerializeField] private float volume = 0.55f;

    private void Awake()
    {
        if (leftLeg == null || rightLeg == null)
        {
            var legs = GetComponentsInChildren<ProceduralLeg>();
            foreach (var l in legs)
            {
                if (l.name.ToLower().Contains("left") || l.transform.localPosition.x < 0)
                    leftLeg = l;
                else
                    rightLeg = l;
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
                audioSource.minDistance = 4f;
                audioSource.maxDistance = 50f;
            }
        }
    }

    private void OnEnable()
    {
        if (leftLeg != null) leftLeg.OnStepTouchdown += HandleFootTouchdown;
        if (rightLeg != null) rightLeg.OnStepTouchdown += HandleFootTouchdown;
    }

    private void OnDisable()
    {
        if (leftLeg != null) leftLeg.OnStepTouchdown -= HandleFootTouchdown;
        if (rightLeg != null) rightLeg.OnStepTouchdown -= HandleFootTouchdown;
    }

    private void HandleFootTouchdown(Vector3 contactPoint, Vector3 surfaceNormal)
    {
        // 1. Audio sutil y limpio de pisada
        PlayFootstepSound();

        // 2. Partículas solo si el usuario asignó un prefab específico
        if (optionalDustPrefab != null)
        {
            var ps = Instantiate(optionalDustPrefab, contactPoint, Quaternion.LookRotation(surfaceNormal));
            ps.Play();
            Destroy(ps.gameObject, 1.5f);
        }

        // 3. Shake solo si está explícitamente configurado mayor a cero
        if (stepShakeIntensity > 0.01f && MechaController.Instance != null)
        {
            MechaController.Instance.TriggerFootstepShake(stepShakeIntensity);
        }
    }

    private void PlayFootstepSound()
    {
        if (stepAudioClip != null && audioSource != null)
        {
            audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(stepAudioClip, volume);
        }
        else
        {
            ProceduralAudio.Instance?.PlayMechaStep();
        }
    }
}
