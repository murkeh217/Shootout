using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class RoguelikeGameManager : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector references ù drag these in from the scene
    // -------------------------------------------------------

    [Header("Core Data")]
    public RunData runData;

    [Header("Dungeon")]
    public DungeonGenerator dungeonGenerator;

    [Header("Player")]
    public GameObject juTPSPlayer;
    public Rigidbody playerRigidbody;

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

    [Header("UI")]
    public GameObject loadingScreen;   // optional ù a simple black panel to hide generation
    public HUDManager hudManager;      // assign if you have one

    [Header("NavMesh")]
    public NavMeshBaker navMeshBaker;

    [Header("Room System")]
    public RoomSpawner roomSpawner;

    [Header("Exit")]
    public GameObject exitPrefab; // optional: if null, a simple trigger is spawned

    [Tooltip("How many rooms must be cleared before Exit appears. If 0, uses 3 + floor (clamped).")]
    public int roomsToClearOverride = 0;

    [Header("Perks")]
    public PerkChoiceUI perkChoiceUI; // optional; if null, one is created at runtime

    [Header("Loot")]
    public LootSpawner lootSpawner; // optional; if null, one is created at runtime

    // -------------------------------------------------------
    // Internal state
    // -------------------------------------------------------

    private bool isTransitioning = false;
    private int roomsClearedThisFloor = 0;
    private int roomsToClearThisFloor = 0;
    private GameObject activeExit;

    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        // Enforce a single instance
        if (FindObjectsOfType<RoguelikeGameManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartNewRun();
    }

    // -------------------------------------------------------
    // Public API ù called by UI buttons or other scripts
    // -------------------------------------------------------

    // Call from your main menu "New Game" button
    public void StartNewRun()
    {
        runData.StartNewRun();
        StartCoroutine(LoadFloor());
    }

    // Call from your floor exit trigger when the player reaches it
    public void OnPlayerReachedExit()
    {
        if (isTransitioning) return;
        StartCoroutine(AdvanceToNextFloor());
    }

    // Call from PlayerHealth when currentHP reaches 0
    public void OnPlayerDied()
    {
        if (isTransitioning) return;
        StartCoroutine(HandleDeath());
    }

    // -------------------------------------------------------
    // Floor loading
    // -------------------------------------------------------

    private IEnumerator LoadFloor()
    {
        isTransitioning = true;
        roomsClearedThisFloor = 0;
        activeExit = null;

        // Show loading screen so the player doesn't see
        // the dungeon being built tile by tile
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Wait one frame for any previous Destroy() calls to finish
        yield return null;

        // 1. Clean up the previous floor if one exists
        dungeonGenerator.cleanDungeons();
        yield return null;

        // 2. Seed Unity's RNG using this floor's unique seed
        // GetFloorSeed() returns seed + (floor * 100)
        // so every floor on every run is different
        int floorSeed = runData.GetFloorSeed();
        Random.InitState(floorSeed);
        Debug.Log($"[GameManager] Generating floor {runData.floor} with seed {floorSeed}");

        // 3. Set spawn position on the dungeon generator
        // The first room always generates near world origin
        dungeonGenerator.playerSpawnPosX = 0f;
        dungeonGenerator.playerSpawnPosY = 1f;   // above floor so player doesn't clip
        dungeonGenerator.playerSpawnPosZ = 0f;

        // 4. Zero player velocity BEFORE the dungeon generator
        // moves the player ù prevents physics fighting the teleport
        ZeroPlayerVelocity();

        // 5. Generate the dungeon
        // Parameters: startX, startZ, entranceDoorI, entranceDoorJ,
        //             entranceDoorPlace, pastRoomSizeI, pastRoomSizeJ, lockedSide
        // All -1 / 0 means "fresh start, no entrance constraints"
        dungeonGenerator.GenDungeon(0, 0, -1, -1, -1, 0, 0, -1);

        // 6. Wait a frame for all Instantiate calls to settle
        yield return null;

        // 7. Bake NavMesh so enemies can pathfind
        if (navMeshBaker != null)
        {
            yield return StartCoroutine(navMeshBaker.BakeAsync());
        }
        else if (navMeshSurface != null)
        {
            // Fallback if scene didn't wire NavMeshBaker component.
            navMeshSurface.RemoveData();
            navMeshSurface.BuildNavMesh();
            yield return null;
        }
        else
        {
            Debug.LogWarning("[GameManager] No NavMeshBaker/NavMeshSurface assigned. Enemies may not pathfind.");
        }

        // 7b. Spawn room triggers / enemies AFTER NavMesh is ready
        // (RoomSpawner expects the dungeon objects already exist and NavMesh is baked)
        if (roomSpawner != null)
        {
            roomSpawner.SpawnRoomsForFloor();
            SetupFloorProgression();
        }
        else
        {
            Debug.LogWarning("[GameManager] RoomSpawner not assigned; no rooms/enemies will spawn.");
        }

        // 8. Hide loading screen ù dungeon and NavMesh are ready
        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        // 9. Update HUD with new floor number
        if (hudManager != null)
            hudManager.UpdateFloorDisplay(runData.floor);

        if (hudManager != null && runData != null)
            hudManager.UpdateHealthDisplay(runData.currentHP, runData.GetEffectiveMaxHP());
    
        isTransitioning = false;

        Debug.Log($"[GameManager] Floor {runData.floor} ready.");
    }

    private void SetupFloorProgression()
    {
        if (roomSpawner == null) return;

        roomsToClearThisFloor = roomsToClearOverride > 0
            ? roomsToClearOverride
            : Mathf.Clamp(3 + runData.floor, 3, 8);

        // Subscribe to all non-start rooms.
        int roomIndex = 0;
        foreach (Room room in roomSpawner.ActiveRooms)
        {
            if (room == null) continue;
            if (room.IsStartRoom) continue;

            room.OnRoomCleared -= OnRoomCleared; // avoid double-subscribe if any
            room.OnRoomCleared += OnRoomCleared;
            roomIndex++;
        }

        Debug.Log($"[GameManager] Clear {roomsToClearThisFloor} rooms to unlock Exit.");
    }

    private void OnRoomCleared()
    {
        roomsClearedThisFloor++;
        Debug.Log($"[GameManager] Rooms cleared: {roomsClearedThisFloor}/{roomsToClearThisFloor}");

        TrySpawnRoomClearLoot();

        // Reward: offer a perk choice after each cleared room (v1).
        TryOfferPerkChoice();

        if (roomsClearedThisFloor >= roomsToClearThisFloor && activeExit == null)
        {
            SpawnExitForCurrentFloor();
        }
    }

    private void TrySpawnRoomClearLoot()
    {
        if (dungeonGenerator == null) return;
        if (runData == null) return;

        if (lootSpawner == null)
        {
            GameObject go = new GameObject("LootSpawner");
            lootSpawner = go.AddComponent<LootSpawner>();
            lootSpawner.runData = runData;
            lootSpawner.dungeonGenerator = dungeonGenerator;
        }

        // Spawn near origin-ish room progression; using roomsClearedThisFloor as salt.
        Vector3 pos = Vector3.zero;
        if (roomSpawner != null && roomSpawner.ActiveRooms.Count > 0)
        {
            // choose a non-start room near the middle if possible
            Room chosen = null;
            float best = float.PositiveInfinity;
            foreach (Room r in roomSpawner.ActiveRooms)
            {
                if (r == null) continue;
                if (r.IsStartRoom) continue;
                float d = Mathf.Abs(r.Centre.sqrMagnitude - 200f); // arbitrary "mid" distance
                if (d < best)
                {
                    best = d;
                    chosen = r;
                }
            }
            if (chosen != null) pos = chosen.Centre;
        }
        pos.y = dungeonGenerator.transformY;

        lootSpawner.TrySpawnRoomClearLoot(pos, roomsClearedThisFloor * 31 + runData.floor * 101);
    }

    private void TryOfferPerkChoice()
    {
        if (runData == null) return;
        if (isTransitioning) return;

        if (perkChoiceUI == null)
        {
            GameObject go = new GameObject("PerkChoiceUIHost");
            perkChoiceUI = go.AddComponent<PerkChoiceUI>();
            perkChoiceUI.runData = runData;
        }

        if (perkChoiceUI.IsOpen) return;

        PerkDefinition[] choices = PerkRoller.Roll3(runData);
        perkChoiceUI.ShowChoices(choices, perk =>
        {
            runData.ApplyPerk(perk);
            if (hudManager != null)
                hudManager.UpdateHealthDisplay(runData.currentHP, runData.GetEffectiveMaxHP());
        });
    }

    private void SpawnExitForCurrentFloor()
    {
        if (roomSpawner == null || roomSpawner.ActiveRooms.Count == 0)
        {
            Debug.LogWarning("[GameManager] Can't spawn Exit: no rooms found.");
            return;
        }

        // Place exit in the farthest non-start room from origin (usually deep in the layout).
        Room bestRoom = null;
        float best = float.NegativeInfinity;
        foreach (Room room in roomSpawner.ActiveRooms)
        {
            if (room == null) continue;
            if (room.IsStartRoom) continue;

            float score = room.Centre.sqrMagnitude;
            if (score > best)
            {
                best = score;
                bestRoom = room;
            }
        }

        Vector3 pos = bestRoom != null ? bestRoom.Centre : Vector3.zero;
        pos.y = dungeonGenerator != null ? dungeonGenerator.transformY + 0.25f : pos.y;

        if (exitPrefab != null)
        {
            activeExit = Instantiate(exitPrefab, pos, Quaternion.identity);
        }
        else
        {
            activeExit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            activeExit.name = "Exit";
            activeExit.transform.position = pos;
            activeExit.transform.localScale = new Vector3(1.5f, 0.25f, 1.5f);
            Destroy(activeExit.GetComponent<Collider>()); // replace with trigger
            CapsuleCollider trigger = activeExit.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.0f;
            trigger.height = 2.0f;
        }

        if (activeExit.GetComponent<ExitTrigger>() == null)
            activeExit.AddComponent<ExitTrigger>().gameManager = this;

        // Make sure it gets cleaned up by dungeon cleanup (floor change)
        if (dungeonGenerator != null && dungeonGenerator.objectsToCleanDungeon != null)
            dungeonGenerator.objectsToCleanDungeon.Add(activeExit);

        Debug.Log("[GameManager] Exit spawned.");
    }

    private IEnumerator AdvanceToNextFloor()
    {
        isTransitioning = true;

        // Optional: play a transition animation or sound here
        yield return new WaitForSeconds(0.5f);

        // Advance the floor counter in RunData
        runData.AdvanceFloor();

        roomSpawner.Cleanup();

        // Load the next floor
        yield return StartCoroutine(LoadFloor());
    }

    // -------------------------------------------------------
    // Death handling
    // -------------------------------------------------------

    private IEnumerator HandleDeath()
    {
        isTransitioning = true;

        // Brief pause before showing death screen
        yield return new WaitForSeconds(1.5f);

        // Store final stats before wiping
        // (death screen reads these before WipeRun() clears them)
        int finalFloor = runData.floor;
        int finalKills = runData.totalKills;
        string duration = runData.GetRunDuration();

        Debug.Log($"[GameManager] Player died. Floor: {finalFloor}, Kills: {finalKills}, Time: {duration}");

        // Wipe all run data ù permadeath
        runData.WipeRun();

        // Show death screen
        // DeathScreen reads the values passed to it before RunData was wiped
        ShowDeathScreen(finalFloor, finalKills, duration);
    }

    private void ShowDeathScreen(int floor, int kills, string duration)
    {
        // If you have a DeathScreen script, call it here
        // Example:
        // deathScreen.Show(floor, kills, duration);

        // For now, just log ù replace with your UI call
        Debug.Log($"[GameManager] Show death screen ù Floor: {floor}, Kills: {kills}, Time: {duration}");
    }

    // -------------------------------------------------------
    // NavMesh baking
    // -------------------------------------------------------

    private IEnumerator BakeNavMesh()
    {
        if (navMeshSurface == null)
        {
            // Try to find one in the scene if not assigned
            navMeshSurface = FindObjectOfType<NavMeshSurface>();
        }

        if (navMeshSurface == null)
        {
            Debug.LogWarning("[GameManager] No NavMeshSurface found. Enemies won't pathfind.");
            yield break;
        }

        // Small delay ù lets all dungeon GameObjects finish
        // being placed before the bake reads the geometry
        yield return new WaitForSeconds(0.1f);

        navMeshSurface.BuildNavMesh();

        // Wait one more frame for the bake to register
        yield return null;

        Debug.Log("[GameManager] NavMesh baked.");

        // Spawn rooms and enemies AFTER NavMesh is baked
        roomSpawner.SpawnRoomsForFloor();
    }

    // -------------------------------------------------------
    // Player helpers
    // -------------------------------------------------------

    private void ZeroPlayerVelocity()
    {
        if (playerRigidbody == null)
        {
            // Try to find it if not assigned
            playerRigidbody = juTPSPlayer?.GetComponent<Rigidbody>();
        }

        if (playerRigidbody == null) return;

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    // -------------------------------------------------------
    // Convenience: get floor-specific seeds for other systems
    // These match RunData's GetEnemySeed() / GetLootSeed()
    // so enemy spawner and loot table stay in sync
    // -------------------------------------------------------

    public int GetEnemySeedForCurrentFloor()
    {
        return runData.GetEnemySeed();
    }

    public int GetLootSeedForCurrentFloor()
    {
        return runData.GetLootSeed();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Find all rooms and clear them for testing
            foreach (var room in FindObjectsOfType<Room>())
            {
                foreach (var enemy in FindObjectsOfType<EnemyHealth>())
                    enemy.TakeDamage(9999);
            }
        }
    }
}