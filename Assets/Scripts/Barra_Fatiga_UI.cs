using UnityEngine;
using UnityEngine.UI;

public class Barra_Fatiga_UI : MonoBehaviour
{
    public static Barra_Fatiga_UI Instancia;

    [Header("Referencias UI")]
    [SerializeField] private GameObject panelContenedor;
    [SerializeField] private Image barraRelleno;

    [Header("Colores")]
    [SerializeField] private Color colorInicial = new Color(1f, 0.85f, 0.1f, 0.95f); // Amarillo
    [SerializeField] private Color colorCritico = new Color(1f, 0.25f, 0.1f, 0.95f); // Rojo al final
    [SerializeField] private bool cambiarColorAlFinal = true;

    private void Awake()
    {
        Instancia = this;
        Ocultar();
    }

    public void Mostrar()
    {
        if (panelContenedor != null && !panelContenedor.activeSelf)
        {
            panelContenedor.SetActive(true);
        }
    }

    public void Ocultar()
    {
        if (panelContenedor != null && panelContenedor.activeSelf)
        {
            panelContenedor.SetActive(false);
        }
    }

    /// <summary>
    /// restanteNormalizado: 1f = 100% resistencia, 0f = agotada.
    /// </summary>
    public void ActualizarProgreso(float restanteNormalizado)
    {
        float valor = Mathf.Clamp01(restanteNormalizado);

        if (barraRelleno != null)
        {
            RectTransform rt = barraRelleno.rectTransform;
            Vector3 escala = rt.localScale;
            escala.x = valor;
            rt.localScale = escala;

            if (cambiarColorAlFinal)
            {
                float tCritico = Mathf.InverseLerp(0.35f, 0f, valor);
                barraRelleno.color = Color.Lerp(colorInicial, colorCritico, tCritico);
            }
            else
            {
                barraRelleno.color = colorInicial;
            }
        }
    }
}