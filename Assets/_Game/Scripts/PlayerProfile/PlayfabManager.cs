using EasyButtons;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayfabManager : MonoBehaviour
{
    public static PlayfabManager Instance;

    [SerializeField] private bool logInAtStart;
    [Space]
    [SerializeField] private string email;
    [SerializeField] private string password;
    [SerializeField] private string username;
    [Space]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject AddMainPasswordPanel;

    [Header("Statistics")]
    [SerializeField] private int receivedPlayerKills;
    [SerializeField] private int playerKillsToSet;

    [Header("PlayerData")]
    [SerializeField] private string receivedSkinsString = "";
    [SerializeField] private string skinsStringToSet = "";

    [Header("Debug")]
    [SerializeField] private bool isClientLoggedIn;
    [SerializeField] private bool isClientLoggedInB;
    [SerializeField] private bool isLoggedIn;
    [SerializeField] private bool isEntityLoggedIn;
    [SerializeField] private bool isInternetOn;
    [SerializeField] private string playfabId;

    private const string PLAYER_KILLS_STATS_KEY = "PlayerKills";
    private const string SKINS_PLAYER_DATA_KEY = "Skins";
    private const string PLAYER_DATA_KEY = "PlayerData";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        this.transform.SetParent(null);
        DontDestroyOnLoad(this.gameObject);

        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "154DB9";
        }
    }

    private void Start()
    {
        if(logInAtStart)
        {
            LogIn();
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        isLoggedIn = PlayFabSettings.staticPlayer != null && !string.IsNullOrEmpty(PlayFabSettings.staticPlayer.PlayFabId);
        isClientLoggedIn = PlayFabClientAPI.IsClientLoggedIn();
        isEntityLoggedIn = PlayFabSettings.staticPlayer.IsEntityLoggedIn();
        isClientLoggedInB = PlayFabSettings.staticPlayer.IsClientLoggedIn();
        isInternetOn = Application.internetReachability != NetworkReachability.NotReachable;
        playfabId = PlayFabSettings.staticPlayer != null ? PlayFabSettings.staticPlayer.PlayFabId : "[staticPlayer null]";
#endif
    }

    [Button]
    private void LogIn()
    {
#if UNITY_STANDALONE
        if (PlayerPrefs.HasKey("EMAIL"))
        {
            email = PlayerPrefs.GetString("EMAIL");
            Debug.Log("logging to Playfab with email " + email + "...");
            password = PlayerPrefs.GetString("PASSWORD");
            var request = new LoginWithEmailAddressRequest { Email = email, Password = password };
            PlayFabClientAPI.LoginWithEmailAddress(request, OnEmailLoginSuccess, OnEmailLoginFailure);
        }
        else
        {
            Debug.Log("LogIn() no email set");
        }
#endif
#if UNITY_ANDROID
        string id = GetMobileId();
        Debug.Log("logging to Playfab with id " + id + "...", this);
        var requestAndroid = new LoginWithAndroidDeviceIDRequest { AndroidDeviceId = id, CreateAccount = true };
        PlayFabClientAPI.LoginWithAndroidDeviceID(requestAndroid, OnLoginMobileSuccess,
            error => { Debug.LogError(error.GenerateErrorReport()); 
            }
        );
#endif
#if UNITY_IOS
        var requestIOS = new LoginWithIOSDeviceIDRequest { DeviceId = GetMobileId(), CreateAccount = true };
        PlayFabClientAPI.LoginWithIOSDeviceID(requestIOS, OnLoginMobileSuccess, 
            error => {Debug.LogError(error.GenerateErrorReport());}
            );
#endif
    }

    private void OnLoginMobileSuccess(LoginResult result)
    {
        Debug.Log($"Login Success, result.PlayFabId {result.PlayFabId}", this);

        if (loginPanel != null)
            loginPanel.SetActive(false);

        GetStats();
        GetPlayerData();
        SetPlayerID();
    }

    private void OnEmailLoginSuccess(LoginResult result)
    {
        Debug.Log("mail Login Success", this);
        PlayerPrefs.SetString("EMAIL", email);
        PlayerPrefs.SetString("PASSWORD", password);

        if(loginPanel != null)
            loginPanel.SetActive(false);

        GetStats();
        GetPlayerData();
        SetPlayerID();
    }

    private void SetPlayerID()
    {
        Debug.Log("SetPlayerID() " + PlayFabSettings.staticPlayer.PlayFabId, this);
        //CrashlyticsManager.OnPlayfabIdSet(PlayFabSettings.staticPlayer.PlayFabId);
    }

    private void OnEmailLoginFailure(PlayFabError error)
    {
        var registerRequest = new RegisterPlayFabUserRequest { 
            Email = email, 
            Password = password, 
            Username = username 
        };

        //couldn't log in, register
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest,
            result =>
            {
                Debug.Log("Register Success!");
                PlayerPrefs.SetString("EMAIL", email);
                PlayerPrefs.SetString("PASSWORD", password);

                PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = username
                },
                    result =>
                    {
                        Debug.Log("Display name updated successfully: " + result.DisplayName, this);
                    },
                    error =>
                    {
                        Debug.LogError("Error updating display name: " + error.GenerateErrorReport(), this);
                    });
                GetStats();
                GetPlayerData(); // probably not needed here because just-registered player doesn't get any skins
                loginPanel?.SetActive(false);
            },
            error => { Debug.LogError(error.GenerateErrorReport()); }
            );
    }

    private string GetMobileId()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(deviceId))
        {
            DateTime now = DateTime.Now;
            int tenths = now.Millisecond / 100;//.Millisecond gives the full 3-digit value (e.g., 726). Dividing by 100 gives the first digit (7 in this case)
            string dateNow = now.ToString("yyyy-MM-dd_HH:mm:ss") + "." + tenths;

            deviceId = "UnknownDevice_" + PlayerPrefs.GetString("DATE_NOW_LOGIN", dateNow);
            PlayerPrefs.SetString("DATE_NOW_LOGIN", dateNow);
        }
        return deviceId;
    }

    public void OnCLogInClicked()
    {
        var request = new LoginWithEmailAddressRequest { Email = email, Password = password };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnEmailLoginSuccess, OnEmailLoginFailure);
    }

    //we have already created account with device id, but user clicked add mail and password to his account
    public void OnAddEmailPasswordClicked()
    {
        var addLoginRequest = new AddUsernamePasswordRequest { Email = email, Password = password, Username = username };
        PlayFabClientAPI.AddUsernamePassword(addLoginRequest,
            result =>
            {
                Debug.Log("OnClickAddLogin() Success", this);
                PlayerPrefs.SetString("EMAIL", email);
                PlayerPrefs.SetString("PASSWORD", password);
                AddMainPasswordPanel.SetActive(false);
            },
            error => { Debug.LogError(error.GenerateErrorReport()); }
            );
    }

    /// <summary>
    /// updating statistics by client (commented because changed to sending request to update statistics by server, so leater we can check on server if updated statistics aren't too big / too often, add sanity checks.
    /// 
    /// For cheat prevention client is not able to update stats. to update login to playfab > YourGame > Settings (on left) > API Features > Allow client to post player statistics > Save  button
    /// </summary>
    [Button]
    private void SetStatistics()
    {
        var sentStats = new List<StatisticUpdate>
        {
            new StatisticUpdate { StatisticName = PLAYER_KILLS_STATS_KEY, Value = playerKillsToSet },
        };
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = sentStats
        },
        result => { 
            Debug.Log("SetStats() successfullly sent: " + string.Join(", ", sentStats.Select(x => x.StatisticName + " " + x.Value)), this); 
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }

    //private void SetStatistics_CloudFunction()
    //{
    //    PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
    //    {
    //        FunctionName = "UpdatePlayerStats",
    //        FunctionParameter = new
    //        {
    //            kills = this.playerKills,
    //        },
    //        GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
    //    },
    //    result =>
    //    {
    //        Debug.Log("StartCloudUpdatePlayerStats successful", this);
    //        //commented becuse of build erors
    //        //Debug.Log(PlayFab.PfEditor.Json.JsonWrapper.SerializeObject(result.FunctionResult), this);
    //        //JsonObject jsonResult = (JsonObject)result.FunctionResult;
    //        //object messageValue;
    //        //jsonResult.TryGetValue("messageValue", out messageValue); // note how "messageValue" directly //corresponds to the JSON values set in CloudScript
    //        //Debug.Log((string)messageValue, this);
    //    },
    //    error =>{Debug.Log(error.GenerateErrorReport());}
    //    );
    //}

    [Button]
    private void GetStats()
    {
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(),
        result =>
        {
            foreach (var stat in result.Statistics)
            {
                switch (stat.StatisticName)
                {
                    case PLAYER_KILLS_STATS_KEY:
                        receivedPlayerKills = stat.Value;
                        break;
                }
            }
            Debug.Log("GetStats() received: " + string.Join(", ", result.Statistics.Select(x => x.StatisticName + " " + x.Value), this)
            );
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }

    [Button]
    private void GetLeaderboard()
    {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = PLAYER_KILLS_STATS_KEY,
            StartPosition = 0, // top 10 players
            MaxResultsCount = 10, // top 10 players
            //ProfileConstraints = new PlayerProfileViewConstraints {} // set on PlayFab (Playfab > Settings > Client Profile Options) so no need to set here 
        },
        result =>
        {
            Debug.Log($"GetLeaderboard() received: {string.Join(", ", result.Leaderboard.Select(x => x.DisplayName + " " + x.StatValue))}", this);
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }

    [Button]
    private void GetPlayerData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest()
        {
            PlayFabId = PlayFabSettings.staticPlayer.PlayFabId, // Get data for the current player. you can also set it in login & register success callback "string playFabId = result.PlayFabId;"
            Keys = null, // Get all keys
        },
        result =>
        {
            string log = "";
            foreach (var data in result.Data)
            {
                log += data.Key + ": " + data.Value.Value + ", ";
            }
            Debug.Log("GetPlayerData(): received " + log, this);

            if (result.Data == null)
            {
                Debug.LogError("GetPlayerData(): PlayerData null", this);
                return;
            }

            if (!result.Data.ContainsKey(PLAYER_DATA_KEY))
            {
                Debug.LogError(PLAYER_DATA_KEY + " not received", this);
            }
            else
            {
                PlayerProfileManager.Instance.SetJson(result.Data[PLAYER_DATA_KEY].Value);
            }

            if (!result.Data.ContainsKey(SKINS_PLAYER_DATA_KEY))
            {
                Debug.LogError(SKINS_PLAYER_DATA_KEY + " not received", this);
            }
            else
            {
                receivedSkinsString = result.Data[SKINS_PLAYER_DATA_KEY].Value;
            }
        },
        error => { Debug.Log(error.GenerateErrorReport()); });

    }

    [Button]
    private void SetPlayerData()
    {
        string playerDataJson = PlayerProfileManager.Instance.GetJson();
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>()
            {
                { PLAYER_DATA_KEY, playerDataJson },
                { SKINS_PLAYER_DATA_KEY, skinsStringToSet }
            }
        },
        result =>
        {
            Debug.Log("SetPlayerData() sent successfully: " + skinsStringToSet + ", " + playerDataJson, this);
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }
}
