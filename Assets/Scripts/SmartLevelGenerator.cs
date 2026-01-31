using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SmartLevelGenerator : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] int width = 20; // You can now safely increase this!
    [SerializeField] int height = 20;
    [SerializeField] float cellSize = 2f;
    
    [Header("Maze Settings")]
    [SerializeField] int extraBranches = 10;
    [SerializeField] int branchLength = 4;

    [Header("Prefabs (MUST NOT HAVE NETWORK OBJECTS)")]
    [SerializeField] GameObject redWallPrefab;
    [SerializeField] GameObject blueWallPrefab;
    [SerializeField] GameObject floorPrefab;
    [SerializeField] GameObject borderWallPrefab;

    // This variable syncs ONE number across the network
    public NetworkVariable<int> levelSeed = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    int[,] grid;
    bool isMapGenerated = false;

    public override void OnNetworkSpawn()
    {
        // 1. Listen for the Seed to change (This triggers generation on Client)
        levelSeed.OnValueChanged += OnSeedChanged;

        if (IsServer)
        {
            // Server picks a random seed and sets it.
            // This change is automatically sent to all Clients.
            levelSeed.Value = Random.Range(1000, 9999);
        }
        else
        {
            // If Client joins late and seed is already set, generate immediately
            if (levelSeed.Value != 0)
            {
                GenerateMap(levelSeed.Value);
            }
        }
    }

    void OnSeedChanged(int oldSeed, int newSeed)
    {
        if (!isMapGenerated && newSeed != 0)
        {
            GenerateMap(newSeed);
        }
    }

    void GenerateMap(int seed)
    {
        Debug.Log($"Generating Map with Seed: {seed}");
        
        // CRITICAL: Initialize Unity's Random with the Synced Seed
        Random.InitState(seed);

        grid = new int[width, height];

        // 1. Fill Grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = (Random.value > 0.5f) ? 1 : 2;
            }
        }

        // 2. Carve Paths
        CarvePath(0, 0, width - 1, height - 1);
        CarvePath(0, height - 1, width - 1, 0);
        AddDeadEnds();

        // 3. Spawn Visuals (Local Instantiate only!)
        SpawnVisualsLocal();
        SpawnFloorLocal();
        SpawnBordersLocal();

        isMapGenerated = true;
    }

    // Note: No longer an IEnumerator because local spawning is fast!
    void SpawnVisualsLocal()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == 0) continue;

                Vector3 pos = new Vector3(x * cellSize, 1.5f, y * cellSize);
                GameObject prefab = (grid[x, y] == 1) ? redWallPrefab : blueWallPrefab;

                // NORMAL INSTANTIATE (No Network Spawn)
                GameObject go = Instantiate(prefab, pos, Quaternion.identity);
                go.transform.parent = transform; // Keep hierarchy clean
            }
        }
    }

    void SpawnFloorLocal()
    {
        float centerX = (width * cellSize) / 2f - (cellSize / 2f);
        float centerZ = (height * cellSize) / 2f - (cellSize / 2f);
        Vector3 floorPos = new Vector3(centerX, 0f, centerZ);

        GameObject floor = Instantiate(floorPrefab, floorPos, Quaternion.identity);
        floor.transform.localScale = new Vector3((width + 2) * cellSize, 0.2f, (height + 2) * cellSize);
        floor.transform.parent = transform;
    }

    void SpawnBordersLocal()
    {
        float mapWidth = width * cellSize;
        float mapHeight = height * cellSize;
        float centerX = (mapWidth / 2f) - (cellSize / 2f);
        float centerZ = (mapHeight / 2f) - (cellSize / 2f);
        float thickness = cellSize; 
        float wallH = 4f;

        // Create the 4 border walls
        CreateBorder(new Vector3(-cellSize, wallH/2, centerZ), new Vector3(thickness, wallH, mapHeight + thickness*2)); // Left
        CreateBorder(new Vector3(mapWidth, wallH/2, centerZ), new Vector3(thickness, wallH, mapHeight + thickness*2));  // Right
        CreateBorder(new Vector3(centerX, wallH/2, -cellSize), new Vector3(mapWidth, wallH, thickness)); // Bottom
        CreateBorder(new Vector3(centerX, wallH/2, mapHeight), new Vector3(mapWidth, wallH, thickness)); // Top
    }

    void CreateBorder(Vector3 pos, Vector3 scale)
    {
        GameObject wall = Instantiate(borderWallPrefab, pos, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.transform.parent = transform;
    }

    // --- LOGIC HELPERS (Same as before) ---
    void AddDeadEnds()
    {
        int branchesAdded = 0;
        int attempts = 0;
        while (branchesAdded < extraBranches && attempts < 100)
        {
            attempts++;
            int rx = Random.Range(1, width - 1);
            int ry = Random.Range(1, height - 1);

            if (grid[rx, ry] == 0)
            {
                int targetX = rx + Random.Range(-branchLength, branchLength);
                int targetY = ry + Random.Range(-branchLength, branchLength);
                targetX = Mathf.Clamp(targetX, 1, width - 2);
                targetY = Mathf.Clamp(targetY, 1, height - 2);
                CarvePath(rx, ry, targetX, targetY);
                branchesAdded++;
            }
        }
    }

    void CarvePath(int startX, int startY, int endX, int endY)
    {
        int currX = startX;
        int currY = startY;
        int safetyTrigger = 0;
        int maxSteps = width * height;

        while ((currX != endX || currY != endY) && safetyTrigger < maxSteps)
        {
            safetyTrigger++;
            grid[currX, currY] = 0; 
            if (Random.value > 0.5f && currX != endX) currX += (endX > currX) ? 1 : -1;
            else if (currY != endY) currY += (endY > currY) ? 1 : -1;
        }
        grid[endX, endY] = 0;
    }
    
    // --- POSITION HELPER FOR PLAYERS ---
    public Vector3 GetSpawnPosition(ulong clientId)
    {
        if (clientId == 0) return new Vector3(0, 2f, 0); // Host
        else return new Vector3((width - 1) * cellSize, 2f, (height - 1) * cellSize); // Client
    }
}