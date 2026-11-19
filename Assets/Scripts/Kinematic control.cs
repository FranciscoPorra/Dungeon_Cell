using UnityEngine;

public class BrickStructuralSupport : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Configuración de Soporte Superior")]
    public LayerMask supportLayer;      // Capa de los ladrillos
    public float checkDistance = 0.6f;  // Distancia del raycast hacia arriba
    public float checkInterval = 0.2f;  // Cada cuántos segundos verifica
    private float timer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Empiezan congelados
        timer = Random.Range(0f, checkInterval); // Desfase para optimizar rendimiento en Quest
    }

    void Update()
    {
        if (!rb.isKinematic) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckSupport();
        }
    }

    void CheckSupport()
    {
        // Elevamos un poco el origen del rayo para que no nazca dentro del propio objeto
        Vector3 rayOrigin = transform.position + (Vector3.up * 0.1f);
        RaycastHit hit;
        
        // Lanzamos el rayo hacia arriba
        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, checkDistance, supportLayer))
        {
            
            Rigidbody upperRb = hit.collider.GetComponent<Rigidbody>();

            if (upperRb == null)
            {
                // Si choca contra algo sin Rigidbody (la pared fija superior)
                Debug.Log($"[{gameObject.name}] Se libera porque tocó un objeto sin Rigidbody (Fila superior).");
                ReleaseBrick();
            }
        }
        else
        {
            
            ReleaseBrick();
        }
    }

    public void ReleaseBrick()
    {
        rb.isKinematic = false;
        rb.WakeUp();
    }
}