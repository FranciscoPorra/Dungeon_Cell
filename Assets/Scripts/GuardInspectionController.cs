using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(AudioSource))]
public class GuardInspectionController : MonoBehaviour
{
    [Header("Referencias en la Puerta")]
    public GameObject guardVisual;
    public VisionConeFilter visionCone;
    public MeshRenderer coneRayRenderer;
    public GameObject visionConeVisual;
    public Light coneSpotLight;
    public AudioSource audioSource;

    [Header("Inicio Automático")]
    [Tooltip("Déjalo desactivado si inicias la partida desde un botón 'Iniciar' en la misma escena")]
    public bool startImmediatelyOnStart = false;

    [Header("Tiempos de Ronda (segundos)")]
    public float minWaitTime = 8f;
    public float maxWaitTime = 16f;
    public float warningLeadTime = 1.5f;
    public float inspectionDuration = 3f;

    [Header("Sonidos de Inmersión (MP3 / AudioClips)")]
    public List<AudioClip> approachSounds = new List<AudioClip>();
    public List<AudioClip> leaveSounds = new List<AudioClip>();

    [Header("Ladrillos a Vigilar (cargados en el Inspector)")]
    public List<XRGrabInteractable> bricksToWatch = new List<XRGrabInteractable>();

    [Header("Derrota")]
    public UnityEvent onPlayerCaught;

    private bool isInspecting = false;
    private bool gameEnded = false;
    private Coroutine inspectionRoutine;

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

        if (startImmediatelyOnStart)
        {
            StartGameplay();
        }
    }

    public void StartGameplay()
    {
        gameEnded = false;
        SetInspectionState(false);

        if (inspectionRoutine != null)
            StopCoroutine(inspectionRoutine);

        inspectionRoutine = StartCoroutine(InspectionLoop());
    }

    public void StopGameplay()
    {
        gameEnded = true;
        SetInspectionState(false);

        if (inspectionRoutine != null)
        {
            StopCoroutine(inspectionRoutine);
            inspectionRoutine = null;
        }
    }

    private IEnumerator InspectionLoop()
    {
        while (!gameEnded)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            if (gameEnded) yield break;

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

            if (brick.isSelected)
                return true;

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
        SetInspectionState(false);

        Debug.Log("¡El guardia vio un ladrillo! Fin de la partida.");
        onPlayerCaught?.Invoke();
    }
}