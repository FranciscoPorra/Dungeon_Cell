using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GuardInspectionController : MonoBehaviour
{
    [Header("Referencias en la Puerta")]
    public GameObject guardPeekVisual;      // Guardia en la mirilla
    public VisionConeFilter visionCone;     // Componente en el objeto con SphereCollider
    public GameObject coneVisualMesh;       // Malla o luz opcional del cono

    [Header("Tiempos (segundos)")]
    public float minWaitBetweenChecks = 8f;
    public float maxWaitBetweenChecks = 16f;
    public float inspectionDuration = 3f;

    [Header("Ladrillos a Vigilar")]
    public bool autoFindBricksOnStart = true;
    public List<EscapableBrick> bricks = new List<EscapableBrick>();

    [Header("Derrota")]
    public UnityEvent onPlayerCaught;

    private bool isInspecting = false;
    private bool gameOverTriggered = false;

    void Start()
    {
        if (autoFindBricksOnStart && bricks.Count == 0)
        {
            bricks.AddRange(FindObjectsByType<EscapableBrick>(FindObjectsSortMode.None));
        }

        SetInspectionActive(false);
        StartCoroutine(InspectionRoutine());
    }

    private IEnumerator InspectionRoutine()
    {
        while (!gameOverTriggered)
        {
            float waitTime = Random.Range(minWaitBetweenChecks, maxWaitBetweenChecks);
            yield return new WaitForSeconds(waitTime);

            if (gameOverTriggered) yield break;

            SetInspectionActive(true);

            float timer = 0f;
            while (timer < inspectionDuration && !gameOverTriggered)
            {
                if (HasHeldBrickInVisionCone())
                {
                    TriggerGameOver();
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            SetInspectionActive(false);
        }
    }

    private void SetInspectionActive(bool active)
    {
        isInspecting = active;

        if (guardPeekVisual != null)
            guardPeekVisual.SetActive(active);

        if (coneVisualMesh != null)
            coneVisualMesh.SetActive(active);

        if (visionCone != null)
            visionCone.gameObject.SetActive(active);
    }

    private bool HasHeldBrickInVisionCone()
    {
        if (!isInspecting || visionCone == null)
            return false;

        foreach (var brick in bricks)
        {
            if (brick == null || !brick.gameObject.activeInHierarchy)
                continue;

            if (brick.IsHeldByPlayer && visionCone.IsColliderInsideCone(brick.BrickCollider))
            {
                return true;
            }
        }

        return false;
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;

        Debug.Log("¡El guardia te vio sosteniendo un ladrillo!");
        onPlayerCaught?.Invoke();
    }
}