using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class Controlador_Fisica_Bloque : MonoBehaviour
{
    // Atributos de instancia
    private Rigidbody rigid_body;
    private BoxCollider box_collider;
    private XRGrabInteractable xr_grab_interactable;

    private float peso_del_objeto;
    private float velocidad_del_objeto;

    // Esto podria ser mas extensible, con distintos tipos de materiales
    private static float densidad_del_material = 1000.0f;
    private static float velocidad_base = 5.0f;

    // Inicialmente el bloque no esta siendo agarrado
    private bool bloque_esta_siendo_agarrado = false;

    // Para que no se deforme el bloque al soltarlo
    private Vector3 escala_inicial;

    [Header("Fatiga y Temblor Progresivo")]
    [SerializeField] private float tiempoBaseResistencia = 240f;
    [SerializeField] private float tiempoMinimoSostener = 4.0f;
    [SerializeField] private float tiempoMaximoSostener = 16.0f;
    [SerializeField, Range(0f, 0.9f)] private float inicioTemblorProgreso = 0.5f;
    [SerializeField] private float temblorMaximoMetros = 0.005f;

    private float tiempoSosteniendo = 0f;
    private float duracionLimiteActual = 5f;
    private IXRSelectInteractor interactorActual;

    void Start()
    {
        box_collider = GetComponent<BoxCollider>();
        rigid_body = GetComponent<Rigidbody>();
        xr_grab_interactable = GetComponent<XRGrabInteractable>();

        // Condicion inicial
        velocidad_del_objeto = velocidad_base;

        Calcular_Velocidad_del_Objeto();

        // Funciones de evento, Entered cuando tomo el bloque y Exited cuando lo suelto
        xr_grab_interactable.selectEntered.AddListener(Agarro_Bloque);
        xr_grab_interactable.selectExited.AddListener(Suelto_Bloque);

        // Permite que el motor de RV calcule la distancia elastica entre tu mano y el bloque, haciendo que los
        // objetos muy pesados se queden fisicamente rezagados o se sientan lentos al arrastrarlos bruscamente.
        xr_grab_interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        xr_grab_interactable.trackRotation = false;

        escala_inicial = transform.localScale;
    }

    public void Calcular_Velocidad_del_Objeto()
    {
        peso_del_objeto = Calcular_Peso_del_Objeto();

        velocidad_del_objeto = Mathf.Max(0.125f, (velocidad_base / peso_del_objeto));
        Debug.Log("Nombre: " + gameObject.name + " -------- Masa: " + peso_del_objeto + " -------- Velocidad: " + velocidad_del_objeto);
        // Piso de seguridad con el 0.5f, si el peso es muy grande, me puede hacer pelotas las fisicas

        rigid_body.mass = peso_del_objeto;
    }

    private float Calcular_Peso_del_Objeto()
    {
        Vector3 Tamanio = box_collider.size;
        Vector3 Escala = transform.localScale; // Obtengo tamanio real del gameobject --> PROBLEMAAAS
        float Volumen = (Escala.x) * (Escala.y) * (Escala.z);

        float Peso = (Volumen * densidad_del_material);

        return (Peso);
    }

    private void Update()
    {
        if (!bloque_esta_siendo_agarrado) return;

        tiempoSosteniendo += Time.deltaTime;
        float progresoTotal = Mathf.Clamp01(tiempoSosteniendo / duracionLimiteActual);

        // Barra amarilla decreciente (1f llena -> 0f agotada)
        if (Barra_Fatiga_UI.Instancia != null)
        {
            Barra_Fatiga_UI.Instancia.ActualizarProgreso(1f - progresoTotal);
        }

        float intensidadActual = 0f;
        if (progresoTotal > inicioTemblorProgreso)
        {
            float divisor = Mathf.Max(1f - inicioTemblorProgreso, 0.001f);
            float progresoPostInicio = Mathf.Clamp01((progresoTotal - inicioTemblorProgreso) / divisor);

            // Potencia 4: tarda bastante en levantar y crece de forma gradual
            float fatiga = Mathf.Pow(progresoPostInicio, 4f);
            intensidadActual = fatiga * temblorMaximoMetros;
        }

        if (Temblor_Camara.Instancia != null)
        {
            Temblor_Camara.Instancia.EstablecerTemblorContinuo(intensidadActual);
        }

        if (tiempoSosteniendo >= duracionLimiteActual)
        {
            ForzarSuelta();
        }
    }

    public void FixedUpdate()
    {
        if (bloque_esta_siendo_agarrado)
        {
            if (rigid_body.velocity.magnitude > velocidad_del_objeto)
            {
                /* Limitador de velocidad, cuando yo agarro algo
                Unity calcula cuanto movi yo mi mano mientras agarraba el objeto
                calcula esa distancia (un vector) y mueve el objeto tan rapido como yo lo movi
                pero si estableciese que la velocidad a la que se tiene que mover el objeto
                es siempre la misma, confundo a las fisicas, se deforma el objeto */
                rigid_body.velocity = rigid_body.velocity.normalized * velocidad_del_objeto;
            }
        }
    }

    private void Agarro_Bloque(SelectEnterEventArgs args)
    {
        bloque_esta_siendo_agarrado = true;
        interactorActual = args.interactorObject;
        tiempoSosteniendo = 0f;

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
        float duracion = 1f; // segundos

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
        /* if (rigid_body.velocity.magnitude > velocidad_del_objeto)
            {
                rigid_body.velocity = rigid_body.velocity.normalized * velocidad_del_objeto;
            } */
        // En lugar de un cero absoluto que choca contra el sistema de RV,
        // multiplicamos la velocidad actual por un factor de frenado suave (ej. 10%)
    }

    private void LimpiarEstadoAgarre()
    {
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