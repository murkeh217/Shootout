using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    [Header("References")]
    public RunData runData;
    public DungeonGenerator dungeonGenerator;

    [Header("Prefabs (optional)")]
    public GameObject healthPickupPrefab;
    public GameObject ammoPickupPrefab;
    public GameObject currencyPickupPrefab;

    [Header("Chances")]
    [Range(0f, 1f)] public float dropChanceOnRoomClear = 0.50f;
    [Range(0f, 1f)] public float healthWeight = 0.30f;
    [Range(0f, 1f)] public float ammoWeight = 0.40f;
    [Range(0f, 1f)] public float currencyWeight = 0.30f;

    public void TrySpawnRoomClearLoot(Vector3 position, int roomIndexSalt)
    {
        if (runData == null) return;

        int seed = runData.GetLootSeed() + roomIndexSalt;
        Random.InitState(seed);

        if (Random.value > dropChanceOnRoomClear) return;

        float roll = Random.value;
        float total = healthWeight + ammoWeight + currencyWeight;
        if (total <= 0.0001f) total = 1f;
        roll *= total;

        GameObject prefab = null;
        if (roll < healthWeight) prefab = healthPickupPrefab;
        else if (roll < healthWeight + ammoWeight) prefab = ammoPickupPrefab;
        else prefab = currencyPickupPrefab;

        GameObject spawned;
        if (prefab != null)
        {
            spawned = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            // Fallback: spawn a simple primitive with the correct pickup script.
            spawned = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawned.transform.position = position + Vector3.up * 0.5f;
            SphereCollider col = spawned.GetComponent<SphereCollider>();
            col.isTrigger = true;

            if (roll < healthWeight) spawned.AddComponent<HealthPickup>().runData = runData;
            else if (roll < healthWeight + ammoWeight) spawned.AddComponent<AmmoPickup>().runData = runData;
            else spawned.AddComponent<CurrencyPickup>().runData = runData;
        }

        if (dungeonGenerator != null && dungeonGenerator.objectsToCleanDungeon != null)
            dungeonGenerator.objectsToCleanDungeon.Add(spawned);
    }
}

