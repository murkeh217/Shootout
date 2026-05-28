using UnityEngine;

// Attach this to a trigger volume placed over the room floor.
// When the player walks in, it activates the room.
[RequireComponent(typeof(BoxCollider))]
public class RoomTrigger : MonoBehaviour
{
    public Room room;

    private bool triggered = false;

    private void Awake()
    {
        // Make sure the collider is a trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        room?.PlayerEntered();
    }
}