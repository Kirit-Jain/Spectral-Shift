using System.Collections.Generic;
using System.Linq; 
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Networking.Transport.Relay;

public class RelayManager : MonoBehaviour
{
    public string JoinCode { get; private set; }

    private async void Start()
    {
        var options = new InitializationOptions();
        options.SetProfile("Player_" + Random.Range(0, 10000));
        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateRelay()
    {
        try
        {
            Debug.Log("Fetching available regions...");
            
            // 1. Get the list of VALID regions from Unity
            List<Region> regions = await RelayService.Instance.ListRegionsAsync();

            // 2. Find a Europe region (Stable middle ground)
            // We search the list so we only pick an ID that actually exists.
            string targetRegion = regions[0].Id; // Default fallback
            foreach (var region in regions)
            {
                Debug.Log($"Found Region: {region.Id}"); // Check Console to see what you have!
                if (region.Id.Contains("europe"))
                {
                    targetRegion = region.Id;
                    break;
                }
            }

            Debug.Log($"Creating Relay in VALID Region: {targetRegion}");

            // 3. Create Allocation using the ID we just found
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3, targetRegion);
            Debug.Log($"HOST STARTED IN REGION: {allocation.Region}");

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Game Created! Join Code: {JoinCode}");

            // 4. Force UDP (dtls) for Windows Stability
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);
            transport.UseWebSockets = false; 

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        joinCode = joinCode.Trim().ToUpper();
        try
        {
            Debug.Log($"Joining Code: {joinCode}");
            
            // Client finds the region automatically from the Code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"CLIENT ATTEMPTING TO JOIN REGION: {joinAllocation.Region}");
            Debug.Log($"Found Room in Region: {joinAllocation.Region}");

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);
            transport.UseWebSockets = false;

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
}