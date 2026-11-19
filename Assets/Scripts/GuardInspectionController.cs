using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(AudioSource))]
public class GuardInspectionController : MonoBehaviour
{
    [Header("Referencias en la Puerta")]
    public GameObject guardVisual;          // Guardia que aparece en la rendija
    public VisionConeFilter visionCone;    // Componente del objeto con SphereCollider
    public MeshRenderer coneRayRenderer;    // MeshRenderer del cono de visión (rayo de luz)
    public GameObject visionConeVisual;     // Opcional: otro objeto visual extra
    public Light coneSpotLight;             // Opcional: luz puntual o de foco
    public AudioSource audioSource;         // Fuente de audio (idealmente 3D en la puerta)

    [Header("Tiempos de Ronda (segundos)")]
    public float minWaitTime = 8f;
    public float maxWaitTime = 16f;
    public float warningLeadTime = 1.5f;  // Tiempo de aviso antes de mostrar guardia y cono
    public float inspectionDuration = 3f;

    [Header("Sonidos de Inmersión (MP3 / AudioClips)")]
    [Tooltip("Sonidos aleatorios antes de que aparezca el guardia (pasos, cerrojo, etc.)")]
    public List<AudioClip> approachSounds = new List<AudioClip>();

    [Tooltip("Sonidos aleatorios cuando el guardia termina y se va")]
    public List<AudioClip> leaveSounds = new List<AudioClip>();

    [Header("Ladrillos a Vigilar (cargados en el Inspector)")]
    public List<XRGrabInteractable> bricksToWatch = new List<XRGrabInteractable>();

    [Header("Derrota")]
    public UnityEvent onPlayerCaught;

    private bool isInspecting = false;
    private bool gameEnded = false;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (coneRayRenderer == null && visionCone != null)
            coneRayRenderer = visionCone.GetComponent<MeshRenderer>();
    }

    void Start()
    {
        SetInspectionState(false);
        StartCoroutine(InspectionLoop());
    }

    private IEnumerator InspectionLoop()
    {
        while (!gameEnded)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            if (gameEnded) yield break;

            // 1. Aviso previo: reproduce un sonido aleatorio y da margen al jugador
            PlayRandomClip(approachSounds);

            if (warningLeadTime > 0f)
            {
                float warnElapsed = 0f;
                while (warnElapsed < warningLeadTime && !gameEnded)
                {
                    warnElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (gameEnded) yield break;

            // 2. Aparece el guardia y se activa el cono de visión
            SetInspectionState(true);

            float elapsed = 0f;
            while (elapsed < inspectionDuration && !gameEnded)
            {
                if (IsPlayerCaughtWithBrick())
                {
                    TriggerGameOver();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. El guardia se va: oculta guardia/cono y reproduce sonido de retirada
            SetInspectionState(false);
            PlayRandomClip(leaveSounds);
        }
    }

    private void PlayRandomClip(List<AudioClip> clips)
    {
        if (audioSource == null || clips == null || clips.Count == 0)
            return;

        List<AudioClip> validClips = new List<AudioClip>();
        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i] != null)
                validClips.Add(clips[i]);
        }

        if (validClips.Count == 0) return;

        int index = Random.Range(0, validClips.Count);
        audioSource.PlayOneShot(validClips[index]);
    }

    private void SetInspectionState(bool active)
    {
        isInspecting = active;

        if (guardVisual != null)
            guardVisual.SetActive(active);

        if (coneRayRenderer != null)
            coneRayRenderer.enabled = active;

        if (visionConeVisual != null)
            visionConeVisual.SetActive(active);

        if (coneSpotLight != null)
            coneSpotLight.enabled = active;

        if (visionCone != null)
            visionCone.enabled = active;
    }

    private bool IsPlayerCaughtWithBrick()
    {
        if (!isInspecting) return false;

        foreach (var brick in bricksToWatch)
        {
            if (brick == null || !brick.gameObject.activeInHierarchy) continue;

            // 1. Está siendo agarrado actualmente
            if (brick.isSelected)
                return true;

            // 2. Está dentro del cono de visión
            if (visionCone != null)
            {
                Collider brickCol = brick.GetComponent<Collider>();
                if (brickCol != null && visionCone.IsColliderInsideCone(brickCol))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void TriggerGameOver()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("¡El guardia vio un ladrillo! Fin de la partida.");
        onPlayerCaught?.Invoke();
    }
}