using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Controlador_Fisica_Bloque : MonoBehaviour
{
    // Atributos de instancia
    private Rigidbody rigid_body;
    private BoxCollider box_collider;
    private XRGrabInteractable xr_grab_interactable;

    private float peso_del_objeto;
    private float velocidad_del_objeto;

    private static float densidad_del_material = 1000.0f;
    private static float velocidad_base = 5.0f;

    private bool bloque_esta_siendo_agarrado = false;
    private Vector3 escala_inicial;

    [Header("Fatiga y Temblor Progresivo")]
    [SerializeField] private float tiempoBaseResistencia = 240f;
    [SerializeField] private float tiempoMinimoSostener = 3.0f;
    [SerializeField] private float tiempoMaximoSostener = 7.0f;
    [SerializeField, Range(0f, 0.9f)] private float inicioTemblorProgreso = 0.5f;
    [SerializeField] private float temblorMaximoMetros = 0.005f;

    private float tiempoSosteniendo = 0f;
    private float duracionLimiteActual = 5f;
    private IXRSelectInteractor interactorActual;

    [Header("Vibración Progresiva en Mandos")]
    [SerializeField] private float vibracionMinima = 0.05f;
    [SerializeField] private float vibracionMaxima = 0.85f;
    [SerializeField] private float intervaloPulsoHaptico = 0.05f;
    [SerializeField] private bool vibrarSoloDesdeInicioTemblor = true;
    [SerializeField] private bool mostrarLogProgresion = true;

    private float temporizadorPulsoHaptico = 0f;

    void Start()
    {
        box_collider = GetComponent<BoxCollider>();
        rigid_body = GetComponent<Rigidbody>();
        xr_grab_interactable = GetComponent<XRGrabInteractable>();

        velocidad_del_objeto = velocidad_base;
        Calcular_Velocidad_del_Objeto();

        xr_grab_interactable.selectEntered.AddListener(Agarro_Bloque);
        xr_grab_interactable.selectExited.AddListener(Suelto_Bloque);

        xr_grab_interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        xr_grab_interactable.trackRotation = false;

        escala_inicial = transform.localScale;
    }

    public void Calcular_Velocidad_del_Objeto()
    {
        peso_del_objeto = Calcular_Peso_del_Objeto();
        velocidad_del_objeto = Mathf.Max(0.125f, (velocidad_base / peso_del_objeto));
        Debug.Log("Nombre: " + gameObject.name + " -------- Masa: " + peso_del_objeto + " -------- Velocidad: " + velocidad_del_objeto);
        rigid_body.mass = peso_del_objeto;
    }

    private float Calcular_Peso_del_Objeto()
    {
        Vector3 Tamanio = box_collider.size;
        Vector3 Escala = transform.localScale;
        float Volumen = (Escala.x) * (Escala.y) * (Escala.z);
        float Peso = (Volumen * densidad_del_material);
        return Peso;
    }

    private void Update()
    {
        if (!bloque_esta_siendo_agarrado) return;

        tiempoSosteniendo += Time.deltaTime;
        float progresoTotal = Mathf.Clamp01(tiempoSosteniendo / Mathf.Max(duracionLimiteActual, 0.01f));

        // Barra decreciente (1f llena -> 0f agotada)
        if (Barra_Fatiga_UI.Instancia != null)
        {
            Barra_Fatiga_UI.Instancia.ActualizarProgreso(1f - progresoTotal);
        }

        float divisor = Mathf.Max(1f - inicioTemblorProgreso, 0.001f);
        float progresoPostInicio = progresoTotal > inicioTemblorProgreso
            ? Mathf.Clamp01((progresoTotal - inicioTemblorProgreso) / divisor)
            : 0f;

        float intensidadActual = 0f;
        if (progresoPostInicio > 0f)
        {
            float fatiga = Mathf.Pow(progresoPostInicio, 4f);
            intensidadActual = fatiga * temblorMaximoMetros;
        }

        if (Temblor_Camara.Instancia != null)
        {
            Temblor_Camara.Instancia.EstablecerTemblorContinuo(intensidadActual);
        }

        ActualizarVibracionHaptica(progresoTotal, progresoPostInicio);

        if (tiempoSosteniendo >= duracionLimiteActual)
        {
            ForzarSuelta();
        }
    }

    private void ActualizarVibracionHaptica(float progresoTotal, float progresoPostInicio)
    {
        bool debeVibrar = vibrarSoloDesdeInicioTemblor
            ? (progresoTotal > inicioTemblorProgreso)
            : (progresoTotal > 0f);

        if (!debeVibrar)
        {
            temporizadorPulsoHaptico = 0f;
            return;
        }

        float baseCurva = vibrarSoloDesdeInicioTemblor ? progresoPostInicio : progresoTotal;
        float factorVibracion = Mathf.Pow(Mathf.Clamp01(baseCurva), 4f);

        temporizadorPulsoHaptico += Time.deltaTime;
        if (temporizadorPulsoHaptico >= intervaloPulsoHaptico)
        {
            temporizadorPulsoHaptico = 0f;
            float amplitudActual = Mathf.Lerp(vibracionMinima, vibracionMaxima, factorVibracion);
            EnviarPulsoHaptico(amplitudActual, intervaloPulsoHaptico * 1.2f);

            if (mostrarLogProgresion)
            {
                string medidor = new string('|', Mathf.Clamp(Mathf.RoundToInt(factorVibracion * 20f), 0, 20));
                Debug.Log($"[Haptics] prog={progresoTotal:F2} | pPost={progresoPostInicio:F2} | factor={factorVibracion:F3} | amp={amplitudActual:F3} | [{medidor,-20}]");
            }
        }
    }

    private void EnviarPulsoHaptico(float amplitud, float duracion)
    {
        if (interactorActual is XRBaseControllerInteractor controllerInteractor)
        {
            controllerInteractor.SendHapticImpulse(Mathf.Clamp01(amplitud), duracion);
        }
    }

    private void DetenerVibracionHaptica()
    {
        temporizadorPulsoHaptico = 0f;
        EnviarPulsoHaptico(0f, 0.01f);

        if (mostrarLogProgresion)
        {
            Debug.Log("[Haptics] Detenida (bloque soltado)");
        }
    }

    public void FixedUpdate()
    {
        if (bloque_esta_siendo_agarrado)
        {
            if (rigid_body.velocity.magnitude > velocidad_del_objeto)
            {
                rigid_body.velocity = rigid_body.velocity.normalized * velocidad_del_objeto;
            }
        }
    }

    private void Agarro_Bloque(SelectEnterEventArgs args)
    {
        bloque_esta_siendo_agarrado = true;
        interactorActual = args.interactorObject;
        tiempoSosteniendo = 0f;
        temporizadorPulsoHaptico = 0f;

        duracionLimiteActual = Mathf.Clamp(
            tiempoBaseResistencia / Mathf.Max(peso_del_objeto, 0.1f),
            tiempoMinimoSostener,
            tiempoMaximoSostener
        );

        Debug.Log($"{gameObject.name} -> Peso: {peso_del_objeto} | Tiempo max sostener: {duracionLimiteActual:F2}s");

        if (Barra_Fatiga_UI.Instancia != null)
        {
            Barra_Fatiga_UI.Instancia.Mostrar();
            Barra_Fatiga_UI.Instancia.ActualizarProgreso(1f);
        }

        float intensidad = Mathf.Clamp(peso_del_objeto * 0.02f, 0.15f, 0.8f);
        float duracion = 0.1f;

        if (args.interactorObject is XRBaseControllerInteractor controllerInteractor)
        {
            controllerInteractor.SendHapticImpulse(intensidad, duracion);
            Debug.Log("Pulso enviado -> intensidad: " + intensidad);
        }
        else
        {
            Debug.LogWarning("El interactor NO es XRBaseControllerInteractor");
        }
    }

    private void ForzarSuelta()
    {
        if (xr_grab_interactable != null && xr_grab_interactable.interactionManager != null && interactorActual != null)
        {
            xr_grab_interactable.interactionManager.SelectExit(interactorActual, xr_grab_interactable);
        }

        LimpiarEstadoAgarre();
    }

    private void Suelto_Bloque(SelectExitEventArgs args)
    {
        LimpiarEstadoAgarre();
        transform.localScale = escala_inicial;
    }

    private void LimpiarEstadoAgarre()
    {
        if (!bloque_esta_siendo_agarrado && interactorActual == null) return;

        DetenerVibracionHaptica();

        bloque_esta_siendo_agarrado = false;
        interactorActual = null;
        tiempoSosteniendo = 0f;

        if (Temblor_Camara.Instancia != null)
        {
            Temblor_Camara.Instancia.DetenerTemblorContinuo();
        }

        if (Barra_Fatiga_UI.Instancia != null)
        {
            Barra_Fatiga_UI.Instancia.Ocultar();
        }
    }
}