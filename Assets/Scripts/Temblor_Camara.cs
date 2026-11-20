using System.Collections;
using UnityEngine;

public class Temblor_Camara : MonoBehaviour
{
    public static Temblor_Camara Instancia;

    private Vector3 posicionInicialLocal;
    private Vector3 offsetPulso = Vector3.zero;
    private float intensidadContinua = 0f;
    private Coroutine rutinaPulso;
    private Vector3 offsetSuavizado = Vector3.zero;

    private void Awake()
    {
        Instancia = this;
        posicionInicialLocal = transform.localPosition;
    }

    public void Sacudir(float intensidad, float duracion = 0.12f)
    {
        if (rutinaPulso != null)
        {
            StopCoroutine(rutinaPulso);
        }
        rutinaPulso = StartCoroutine(RutinaPulso(intensidad, duracion));
    }

    public void EstablecerTemblorContinuo(float intensidad)
    {
        intensidadContinua = Mathf.Max(0f, intensidad);
    }

    public void DetenerTemblorContinuo()
    {
        intensidadContinua = 0f;
    }

    private IEnumerator RutinaPulso(float intensidad, float duracion)
    {
        float transcurrido = 0f;

        while (transcurrido < duracion)
        {
            transcurrido += Time.deltaTime;
            float decaimiento = 1f - (transcurrido / duracion);
            offsetPulso = Random.insideUnitSphere * intensidad * decaimiento;
            yield return null;
        }

        offsetPulso = Vector3.zero;
        rutinaPulso = null;
    }

    private void LateUpdate()
    {
        Vector3 offsetContinuo = intensidadContinua > 0f
            ? Random.insideUnitSphere * intensidadContinua
            : Vector3.zero;

        Vector3 offsetObjetivo = offsetPulso + offsetContinuo;
        float factorSuavizado = 1f - Mathf.Exp(-18f * Time.deltaTime);
        offsetSuavizado = Vector3.Lerp(offsetSuavizado, offsetObjetivo, factorSuavizado);

        if (offsetSuavizado.sqrMagnitude > 0.00000001f || rutinaPulso != null)
        {
            transform.localPosition = posicionInicialLocal + offsetSuavizado;
        }
        else if (transform.localPosition != posicionInicialLocal)
        {
            transform.localPosition = posicionInicialLocal;
        }
    }
}
