using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public NetworkVariable<float> health = new NetworkVariable<float>(100f);

    public void TakeDamage(float amount)
    {
        if(!IsServer) return;

        health.Value -= amount;
        Debug.Log($"Player {OwnerClientId} took damage. HP: {health.Value}");

        if (health.Value <= 0)
        {
            // Die / Respawn Logic
            health.Value = 100f;
            transform.position = Vector3.zero; // Respawn
        }
    }
}
