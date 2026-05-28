using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    // -------------------------------------------------------
    // State
    // -------------------------------------------------------

    public enum RoomState { Inactive, Active, Cleared }

    public RoomState State { get; private set; } = RoomState.Inactive;

    // Fired when all enemies are dead
    public event Action OnRoomCleared;

    // Fired when the player first enters
    public event Action OnRoomActivated;

    // -------------------------------------------------------
    // References — populated by RoomSpawner at generation time
    // -------------------------------------------------------

    private List<EnemyHealth> enemies = new List<EnemyHealth>();
    private List<DoorController> doors = new List<DoorController>();

    public Vector3 Centre { get; private set; }
    public bool IsStartRoom { get; private set; }

    public void Initialise(Vector3 centre, bool isStartRoom)
    {
        Centre = centre;
        IsStartRoom = isStartRoom;
    }

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    // Called by RoomSpawner after it places enemies in this room
    public void RegisterEnemy(EnemyHealth enemy)
    {
        if (enemy == null) return;
        enemies.Add(enemy);
        enemy.OnDied += OnEnemyDied;
    }

    // Called by RoomSpawner to link corridor doors to this room
    public void RegisterDoor(DoorController door)
    {
        if (door == null) return;
        doors.Add(door);
    }

    // Called by RoomTrigger when the player walks in
    public void PlayerEntered()
    {
        if (State != RoomState.Inactive) return;

        State = RoomState.Active;
        OnRoomActivated?.Invoke();

        // Lock all doors when combat starts
        if (enemies.Count > 0)
        {
            foreach (var door in doors)
                door.Lock();

            Debug.Log($"[Room] {name} activated — {enemies.Count} enemies, doors locked.");
        }
        else
        {
            // Empty room — clear immediately, no door lock
            ClearRoom();
        }
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private void OnEnemyDied(EnemyHealth enemy)
    {
        enemy.OnDied -= OnEnemyDied;
        enemies.Remove(enemy);

        Debug.Log($"[Room] {name} — {enemies.Count} enemies remaining.");

        if (enemies.Count == 0)
            ClearRoom();
    }

    private void ClearRoom()
    {
        if (State == RoomState.Cleared) return;

        State = RoomState.Cleared;

        // Unlock all doors
        foreach (var door in doors)
            door.Unlock();

        OnRoomCleared?.Invoke();

        Debug.Log($"[Room] {name} cleared!");
    }
}