using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using EasyButtons;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private List<StringStringPair> listDebug = new();

    private Dictionary<string, string> playerData = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeDictionaryFromList();// todo remove, not to have to set some data every time; I set first data in inspector which should go to dicitonary at start

        RebuildDebugList();
    }

    public string GetValue(string key)
    {
        return playerData.TryGetValue(key, out var value) ? value : null;
    }

    public void SetValue(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        playerData[key] = value;

        RebuildDebugList();
    }

    public void RemoveKey(string key)
    {
        if (playerData.Remove(key))
        {
            RebuildDebugList();
        }
    }

    [Button]
    public string GetJson()
    {
        var json = JsonConvert.SerializeObject(playerData); //needs Newtonsoft.Json: Package Manager -> Add package by name -> com.unity.nuget.newtonsoft-json
        Debug.Log("GetJson: " + json);
        return json;
    }

    [Button]
    public void SetJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            playerData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            RebuildDebugList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to deserialize player data: " + e.Message);
        }
    }

    private void InitializeDictionaryFromList()
    {
        foreach (var pair in listDebug)
        {
            if (!string.IsNullOrEmpty(pair.key) && !playerData.ContainsKey(pair.key))
            {
                playerData[pair.key] = pair.value;
            }
        }
    }

    private void RebuildDebugList()
    {
#if UNITY_EDITOR
        listDebug.Clear();
        foreach (var kvp in playerData)
        {
            listDebug.Add(new StringStringPair { key = kvp.Key, value = kvp.Value });
        }
#endif
    }

    [System.Serializable]
    public struct StringStringPair
    {
        public string key;
        public string value;
    }
}
