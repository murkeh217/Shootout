using System.Collections.Generic;
using UnityEngine;

public class RoomSpawner : MonoBehaviour
{
    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public RunData runData;

    [Header("Door prefab")]
    [Tooltip("A cube or flat mesh with DoorController + Collider")]
    public GameObject doorPrefab;

    [Header("Enemy prefabs ù override dungeon generator's own spawning")]
    public GameObject[] enemyPrefabs;

    [Header("Enemy scaling")]
    [Tooltip("HP multiplier added per floor. 0.15 = +15% per floor.")]
    public float hpPerFloor = 0.15f;

    [Tooltip("Chance per enemy to become elite (extra HP) every few floors.")]
    [Range(0f, 1f)] public float eliteChance = 0.10f;
    public int eliteEveryNFloors = 3;
    public float eliteHpMultiplier = 1.75f;

    // Tracks all rooms this floor for room-clear checks
    private List<Room> activeRooms = new List<Room>();
    public IReadOnlyList<Room> ActiveRooms => activeRooms;

    // -------------------------------------------------------
    // Call this from RoguelikeGameManager after GenDungeon()
    // and after NavMesh is baked
    // -------------------------------------------------------
    public void SpawnRoomsForFloor()
    {
        activeRooms.Clear();

        // Seed enemy placement independently from dungeon layout
        Random.InitState(runData.GetEnemySeed());

        // The dungeon generator tracks all objects it created
        // in objectsToCleanDungeon. We use tunelVector3Saver
        // (corridor tile positions) to find where corridors are,
        // then create door blockers at corridor entrances.
        SpawnDoorsAtCorridors();

        // Create trigger volumes over each room cluster
        // by grouping floor tiles into connected regions
        SpawnRoomTriggers();

        LinkDoorsToNearestRoom();
    }

    public void Cleanup()
    {
        activeRooms.Clear();
    }

    // -------------------------------------------------------
    // Door placement
    // -------------------------------------------------------

    private void SpawnDoorsAtCorridors()
    {
        if (doorPrefab == null)
        {
            Debug.LogWarning("[RoomSpawner] No door prefab assigned.");
            return;
        }

        // tunelVector3Saver holds the world positions of every
        // corridor tile the dungeon generator placed
        List<Vector3> corridorTiles = dungeonGenerator.tunelVector3Saver;

        if (corridorTiles == null || corridorTiles.Count == 0) return;

        // Find the first and last tile in each corridor run
        // and place a door at those positions
        // Simple approach: place a door at every 3rd corridor tile
        // to act as a blocker without over-crowding narrow passages
        for (int i = 0; i < corridorTiles.Count; i += 3)
        {
            Vector3 pos = corridorTiles[i];
            pos.y = dungeonGenerator.transformY; // align to floor level

            GameObject doorGO = Instantiate(doorPrefab, pos, Quaternion.identity);
            doorGO.name = $"Door_{i}";

            DoorController door = doorGO.GetComponent<DoorController>();
            if (door == null)
                door = doorGO.AddComponent<DoorController>();

            // Add to dungeon cleanup list so it gets destroyed on floor change
            dungeonGenerator.objectsToCleanDungeon.Add(doorGO);
        }
    }

    // -------------------------------------------------------
    // Room trigger placement
    // -------------------------------------------------------

    private void SpawnRoomTriggers()
    {
        // Group the dungeon's floor objects into rooms by proximity
        // Each cluster of tiles that are close together is one room
        List<GameObject> allObjects = dungeonGenerator.objectsToCleanDungeon;
        List<Vector3> floorPositions = new List<Vector3>();

        // Collect floor tile positions ù they sit at transformY
        float floorY = dungeonGenerator.transformY;
        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;
            // Floor tiles are at exactly transformY
            // Walls and props are offset ù filter by Y tolerance
            if (Mathf.Abs(go.transform.position.y - floorY) < 0.5f)
                floorPositions.Add(go.transform.position);
        }

        if (floorPositions.Count == 0) return;

        // Cluster floor tiles into rooms using a simple
        // distance-based grouping ù tiles within sizeOfInt*2
        // of each other belong to the same room
        float clusterRadius = dungeonGenerator.sizeOfInt * 2.5f;
        List<List<Vector3>> clusters = ClusterPositions(floorPositions, clusterRadius);

        Debug.Log($"[RoomSpawner] Found {clusters.Count} room clusters.");

        foreach (List<Vector3> cluster in clusters)
        {
            if (cluster.Count < 4) continue; // skip tiny clusters (corridor tiles)

            CreateRoomFromCluster(cluster);
        }
    }

    private void CreateRoomFromCluster(List<Vector3> floorTiles)
    {
        // Find the bounds of this cluster
        Vector3 min = floorTiles[0];
        Vector3 max = floorTiles[0];

        foreach (Vector3 pos in floorTiles)
        {
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        Vector3 centre = (min + max) / 2f;
        Vector3 size = (max - min) + Vector3.one * dungeonGenerator.sizeOfInt;
        size.y = 3f; // tall enough to fully contain the player

        // Create the Room GameObject
        GameObject roomGO = new GameObject($"Room_{centre.x:0}_{centre.z:0}");
        roomGO.transform.position = centre;

        Room room = roomGO.AddComponent<Room>();

        // Create the trigger volume
        GameObject triggerGO = new GameObject("RoomTrigger");
        triggerGO.transform.parent = roomGO.transform;
        triggerGO.transform.position = centre;

        BoxCollider trigger = triggerGO.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = size;

        RoomTrigger roomTrigger = triggerGO.AddComponent<RoomTrigger>();
        roomTrigger.room = room;

        // Spawn enemies inside this room if it's not the start room
        // Start room = closest cluster to world origin
        bool isStartRoom = centre.magnitude < dungeonGenerator.sizeOfInt * 3f;
        room.Initialise(centre, isStartRoom);
        if (!isStartRoom && enemyPrefabs.Length > 0)
            SpawnEnemiesInRoom(room, centre, size);

        activeRooms.Add(room);

        // Add to cleanup list
        dungeonGenerator.objectsToCleanDungeon.Add(roomGO);

        Debug.Log($"[RoomSpawner] Created room at {centre}, {floorTiles.Count} tiles, start={isStartRoom}");
    }

    private void LinkDoorsToNearestRoom()
    {
        if (activeRooms.Count == 0) return;

        // Doors are expected to be added to objectsToCleanDungeon, so find them there.
        foreach (GameObject go in dungeonGenerator.objectsToCleanDungeon)
        {
            if (go == null) continue;

            DoorController door = go.GetComponent<DoorController>();
            if (door == null) continue;

            Room nearest = null;
            float best = float.PositiveInfinity;
            Vector3 p = door.transform.position;

            for (int i = 0; i < activeRooms.Count; i++)
            {
                Room room = activeRooms[i];
                float d = (room.Centre - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = room;
                }
            }

            if (nearest != null)
            {
                nearest.RegisterDoor(door);
            }
        }
    }

    private void SpawnEnemiesInRoom(Room room, Vector3 centre, Vector3 roomSize)
    {
        // Number of enemies scales with floor depth
        int minEnemies = 1 + runData.floor;
        int maxEnemies = 3 + runData.floor;
        int count = Random.Range(minEnemies, Mathf.Min(maxEnemies, 6));

        float spawnRadius = Mathf.Min(roomSize.x, roomSize.z) * 0.3f;

        // If enemyPrefabs not set in scene, fallback to DungeonGenerator enemies list.
        GameObject[] pool = (enemyPrefabs != null && enemyPrefabs.Length > 0)
            ? enemyPrefabs
            : (dungeonGenerator != null ? dungeonGenerator.enemies : null);

        if (pool == null || pool.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            // Random position within the room
            Vector3 spawnPos = centre + new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0f,
                Random.Range(-spawnRadius, spawnRadius)
            );
            spawnPos.y = dungeonGenerator.transformY;

            GameObject prefab = pool[Random.Range(0, pool.Length)];
            GameObject enemyGO = Instantiate(prefab, spawnPos, Quaternion.identity);

            EnemyHealth health = enemyGO.GetComponent<EnemyHealth>();
            if (health == null)
                health = enemyGO.AddComponent<EnemyHealth>();

            ApplyScaling(health);
            room.RegisterEnemy(health);

            // Add to cleanup so enemies are destroyed on floor change
            dungeonGenerator.objectsToCleanDungeon.Add(enemyGO);
        }
    }

    private void ApplyScaling(EnemyHealth health)
    {
        if (health == null || runData == null) return;

        int floor = Mathf.Max(0, runData.floor);
        float hpMult = 1f + (hpPerFloor * floor);

        bool elite = eliteEveryNFloors > 0
                     && floor > 0
                     && (floor % eliteEveryNFloors == 0)
                     && Random.value < eliteChance;

        if (elite) hpMult *= eliteHpMultiplier;

        int scaled = Mathf.Max(1, Mathf.RoundToInt(health.maxHP * hpMult));
        health.maxHP = scaled;
    }

    // -------------------------------------------------------
    // Simple distance-based clustering
    // -------------------------------------------------------

    private List<List<Vector3>> ClusterPositions(List<Vector3> positions, float radius)
    {
        List<List<Vector3>> clusters = new List<List<Vector3>>();
        List<bool> assigned = new List<bool>(new bool[positions.Count]);

        for (int i = 0; i < positions.Count; i++)
        {
            if (assigned[i]) continue;

            List<Vector3> cluster = new List<Vector3> { positions[i] };
            assigned[i] = true;

            for (int j = i + 1; j < positions.Count; j++)
            {
                if (assigned[j]) continue;

                if (Vector3.Distance(positions[i], positions[j]) <= radius)
                {
                    cluster.Add(positions[j]);
                    assigned[j] = true;
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }
}