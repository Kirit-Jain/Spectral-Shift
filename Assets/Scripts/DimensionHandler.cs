using Unity.Netcode;
using UnityEngine;

public class DimensionHandler : NetworkBehaviour
{
    [SerializeField] Camera playerCamera;
    
    string redLayer = "RedDimension";
    string blueLayer = "BlueDimension";
    string redPlayerLayer = "RedPlayer";
    string bluePlayerLayer = "BluePlayer";

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (playerCamera) playerCamera.enabled = false;
            if (playerCamera && playerCamera.TryGetComponent<AudioListener>(out var listener))
            {
                listener.enabled = false;
            }

            SetTeamlayer();
            return;
        }

        if (playerCamera)
        {
            playerCamera.enabled = true;
            playerCamera.gameObject.SetActive(true);
            playerCamera.tag = "MainCamera";
            if (playerCamera.TryGetComponent<AudioListener>(out var listener))
            {
                listener.enabled = true;
            }

            SetTeamlayer();
            UpdateView();
        }
    }

    void SetTeamlayer ()
    {
        bool isRedTeam = OwnerClientId % 2 == 0;
        if (isRedTeam)
        {
            gameObject.layer = LayerMask.NameToLayer(redPlayerLayer);
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer(bluePlayerLayer);
        }
    }

    void UpdateView()
    {
        int redId = LayerMask.NameToLayer(redLayer);
        int blueId = LayerMask.NameToLayer(blueLayer);
        bool isRedTeam = OwnerClientId % 2 == 0;

        if (isRedTeam)
        {
            // Red Team: Hide Blue Walls
            if(playerCamera) playerCamera.cullingMask = ~(1 << blueId);
            SetColor(Color.red);
        }
        else
        {
            // Blue Team: Hide Red Walls
            if(playerCamera) playerCamera.cullingMask = ~(1 << redId);
            SetColor(Color.blue);
        }
    }

    void SetColor(Color c)
    {
        var renderer = GetComponentInChildren<MeshRenderer>();
        if(renderer != null) renderer.material.color = c;
    }
}