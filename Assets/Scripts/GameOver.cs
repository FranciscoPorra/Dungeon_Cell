using UnityEngine;

public class SimpleLoseLog : MonoBehaviour
{
    public string mensaje = "¡Perdiste! El guardia te vio con el ladrillo.";

    public void MostrarDerrota()
    {
        Debug.Log(mensaje);
    }
}