using UnityEngine;

public abstract class PickupBase : MonoBehaviour
{
    public bool destroyOnPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (TryApply(other.gameObject))
        {
            if (destroyOnPickup)
                Destroy(gameObject);
        }
    }

    protected abstract bool TryApply(GameObject player);
}

