using UnityEngine;
using UnityEngine.SceneManagement;

public class SameSceneFlowController : MonoBehaviour
{
    [Header("Pantallas UI (World Space Canvas)")]
    public GameObject mainMenuPanel;
    public GameObject gameOverPanel;

    [Header("Sistemas del Juego")]
    public GuardInspectionController guardController;
    [Tooltip("Opcional: objeto raíz que agrupa el muro o los ladrillos interactuables")]
    public GameObject bricksRoot;

    [Header("Posicionamiento en VR")]
    public Camera playerCamera;
    public float panelDistance = 1.6f;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Start()
    {
        // Estado inicial: solo se muestra el menú principal
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (bricksRoot != null)
            bricksRoot.SetActive(false);

        if (guardController != null)
            guardController.StopGameplay();
    }

    // Conectar al botón "Iniciar" del MainMenuPanel
    public void StartGame()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (bricksRoot != null)
            bricksRoot.SetActive(true);

        if (guardController != null)
            guardController.StartGameplay();
    }

    // Conectar al evento "On Player Caught" de GuardInspectionController
    public void ShowGameOver()
    {
        if (guardController != null)
            guardController.StopGameplay();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            PlacePanelInFrontOfPlayer(gameOverPanel.transform);
        }
    }

    // Conectar al botón "Reintentar" del GameOverPanel
    public void RetryGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private void PlacePanelInFrontOfPlayer(Transform panelTransform)
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || panelTransform == null) return;

        Vector3 flatForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = playerCamera.transform.forward;

        panelTransform.position = playerCamera.transform.position + flatForward * panelDistance;
        panelTransform.rotation = Quaternion.LookRotation(panelTransform.position - playerCamera.transform.position);
    }
}