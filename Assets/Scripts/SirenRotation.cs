using UnityEngine;

/// <summary>
/// Gira el pivote de las sirenas de emergencia sobre su propio eje Y.
/// Se asigna unicamente al objeto Siren_Pivot.
/// </summary>
public class SirenRotation : MonoBehaviour
{
    [Tooltip("Velocidad de giro de las sirenas en grados por segundo.")]
    public float rotationSpeed = 180f;

    void Update()
    {
        // Rotate recibe grados (no radianes) y Time.deltaTime lo hace independiente del framerate.
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}
