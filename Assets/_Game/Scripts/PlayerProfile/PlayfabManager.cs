using EasyButtons;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    [SerializeField] private int playerKills;
    [SerializeField] private int playerWinsAsMarine;
    [SerializeField] private int playerWinsAsAlien;

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
            userEmail = PlayerPrefs.GetString("EMAIL");
            Debug.Log("logging to Playfab with email " + userEmail + "...");
            userPassword = PlayerPrefs.GetString("PASSWORD");
            var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword };
            PlayFabClientAPI.LoginWithEmailAddress(request, OnEmailLoginSuccess, OnEmailLoginFailure);
        }
        else
        {
            Debug.Log("LogIn() no email set");
        }
#endif
#if UNITY_ANDROID
        string id = GetMobileId();
        Debug.Log("logging to Playfab with id " + id + "...");
        var requestAndroid = new LoginWithAndroidDeviceIDRequest { AndroidDeviceId = id, CreateAccount = true };
        PlayFabClientAPI.LoginWithAndroidDeviceID(requestAndroid, 
            result =>
            {
                Debug.Log($"Login Success, result.PlayFabId {result.PlayFabId}");

                if(loginPanel != null)
                    loginPanel.SetActive(false);

                GetStats();
                GetPlayerData();
                SetPlayerID();
            },
            error => { Debug.LogError(error.GenerateErrorReport()); }
        );
#endif
#if UNITY_IOS
        var requestIOS = new LoginWithIOSDeviceIDRequest { DeviceId = ReturnMobileID(), CreateAccount = true };
        PlayFabClientAPI.LoginWithIOSDeviceID(requestIOS, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif
    }

    private void OnEmailLoginSuccess(LoginResult result)
    {
        Debug.Log("mail Login Success");
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
        Debug.Log("SetPlayerID() " + PlayFabSettings.staticPlayer.PlayFabId);
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
                        Debug.Log("Display name updated successfully: " + result.DisplayName);
                    },
                    error =>
                    {
                        Debug.LogError("Error updating display name: " + error.GenerateErrorReport());
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
                Debug.Log("OnClickAddLogin() Success");
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
    //private void SetStats()
    //{
    //    var sentStats = new List<StatisticUpdate>
    //    {
    //        new StatisticUpdate { StatisticName = "Kills", Value = playerKills },
    //        new StatisticUpdate { StatisticName = "WinsAsMarine", Value = playerWinsAsMarine },
    //        new StatisticUpdate { StatisticName = "WinsAsAlien", Value = playerWinsAsAlien }
    //    };
    //    PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
    //    {
    //
    //        Statistics = sentStats
    //    },
    //    result => { 
    //        Debug.Log("Sent Stats " + string.Join(", ",
    //            sentStats.Select(x => x.StatisticName + " " + x.Value))
    //        ); 
    //    },
    //    error => { Debug.Log(error.GenerateErrorReport()); });
    //}

    private void SetStats_CloudFunction()
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "UpdatePlayerStats",
            FunctionParameter = new
            {
                kills = this.playerKills,
                winsAsMarine = this.playerWinsAsMarine,
                winsAsAlien = this.playerWinsAsAlien,
            },
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        },
        result =>
        {
            Debug.Log("StartCloudUpdatePlayerStats successful");
            //commented becuse of build erors
            //Debug.Log(PlayFab.PfEditor.Json.JsonWrapper.SerializeObject(result.FunctionResult));
            //JsonObject jsonResult = (JsonObject)result.FunctionResult;
            //object messageValue;
            //jsonResult.TryGetValue("messageValue", out messageValue); // note how "messageValue" directly corresponds to the JSON values set in CloudScript
            //Debug.Log((string)messageValue);
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
        }
        );
    }

    private void GetStats()
    {
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(),
        result =>
        {
            foreach (var stat in result.Statistics)
            {
                switch (stat.StatisticName)
                {
                    case "Kills":
                        playerKills = stat.Value;
                        break;
                    case "WinsAsMarine":
                        playerWinsAsMarine = stat.Value;
                        break;
                    case "WinsAsAlien":
                        playerWinsAsAlien = stat.Value;
                        break;
                }
            }
            Debug.Log("Received Stats " + string.Join(", ", result.Statistics.Select(x => x.StatisticName + " " + x.Value))
            );
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }

    [Button]
    private void GetLeaderboard()
    {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "Kills",
            StartPosition = 0, // top 10 players
            MaxResultsCount = 10, // top 10 players
            //ProfileConstraints = new PlayerProfileViewConstraints {} // set on PlayFab (Playfab > Settings > Client Profile Options) so no need to set here 
        },
        result =>
        {
            Debug.Log("Received Leaderboard " +
                string.Join(", ", result.Leaderboard.Select(x => x.DisplayName + " " + x.StatValue))
            );
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
                log += data.Key + ": " + data.Value.Value + "\n";
            }
            Debug.Log("Received PlayerData: " + log);

            if (result.Data == null || !result.Data.ContainsKey("Skins"))
            {
                Debug.Log("Skins not set");
                return;
            }
            receivedSkinsString = result.Data["Skins"].Value;
        },
        error => { Debug.Log(error.GenerateErrorReport()); });

    }

    [Button]
    private void SetPlayerData()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>()
            {
                { "Skins", skinsStringToSet }
            }
        },
        result =>
        {
            Debug.Log("Updated skins: " + skinsStringToSet);
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }
}
