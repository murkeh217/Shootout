using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayfabManager : MonoBehaviour
{
    public static PlayfabManager PFC;

    [SerializeField] private string userEmail;
    [SerializeField] private string userPassword;
    [SerializeField] private string userName;
    [Space]
    public GameObject LoginPanel;
    public GameObject AddLoginPanel;
    public GameObject RecoverButton;
    [Space]
    public int playerKills;
    public int playerWinsAsMarine;
    public int playerWinsAsAlien;
    [Space]
    public string skinsString = "";
    [Space]
    public bool isClientLoggedIn;
    public bool isClientLoggedInB;
    public bool isLoggedIn;
    public bool isEntityLoggedIn;
    public bool isInternetOn;

    private void Update()
    {
#if UNITY_EDITOR
        isLoggedIn = PlayFabSettings.staticPlayer != null && !string.IsNullOrEmpty(PlayFabSettings.staticPlayer.PlayFabId);
        isClientLoggedIn = PlayFabClientAPI.IsClientLoggedIn();
        isEntityLoggedIn = PlayFabSettings.staticPlayer.IsEntityLoggedIn();
        isClientLoggedInB = PlayFabSettings.staticPlayer.IsClientLoggedIn();
        isInternetOn = Application.internetReachability != NetworkReachability.NotReachable;
#endif
    }

    private void OnEnable()
    {
        if (PlayfabManager.PFC == null)
        {
            PlayfabManager.PFC = this;
        }
        else
        {
            if (PlayfabManager.PFC != this)
            {
                Destroy(this.gameObject);
            }
        }
        this.transform.SetParent(null);
        DontDestroyOnLoad(this.gameObject);
    }

    public void Start2() // playfab manager disabled not to spam console
    {
        //Note: Setting title Id here can be skipped if you have set the value in Editor Extensions already.
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "148F45"; // Please change this value to your own titleId from PlayFab Game Manager
        }

        string id = ReturnMobileID();
        Debug.Log("login device id: " + id);


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
#if UNITY_ANDROID
            Debug.Log("logging to Playfab with id " + id + "...");
            var requestAndroid = new LoginWithAndroidDeviceIDRequest { AndroidDeviceId = id, CreateAccount = true };
            PlayFabClientAPI.LoginWithAndroidDeviceID(requestAndroid, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif
#if UNITY_IOS
            var requestIOS = new LoginWithIOSDeviceIDRequest { DeviceId = ReturnMobileID(), CreateAccount = true };
            PlayFabClientAPI.LoginWithIOSDeviceID(requestIOS, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif
        }
    }

    private void OnEmailLoginSuccess(LoginResult result)
    {
        Debug.Log("mail Login Success");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        if(LoginPanel != null)
            LoginPanel.SetActive(false);
        if (RecoverButton != null)
            RecoverButton.SetActive(false);
        GetStats();
        GetPlayerData();
        //CrashlyticsManager.OnPlayfabIdSet(result.PlayFabId);
    }

    private void OnEmailLoginFailure(PlayFabError error)
    {
        var registerRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = userName };
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnRegisterFailure);//couldn't log in, register
    }

    private void OnLoginMobileSuccess(LoginResult result)
    {
        Debug.Log("Login Success, you made your first successful API call!");
        LoginPanel?.SetActive(false);
        GetStats();
        GetPlayerData();
        //CrashlyticsManager.OnPlayfabIdSet(result.PlayFabId);
    }

    private void OnLoginMobileFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("Register Success, you made your first successful API call!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);

        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = userName
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
        LoginPanel?.SetActive(false);
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    public void GetUserEmail(string emailIn)
    {
        userEmail = emailIn;
    }

    public void GetUserPassword(string passwordIn)
    {
        userPassword = passwordIn;
    }

    public void GetUserName(string userNameIn)
    {
        userName = userNameIn;
    }

    public void OnClickLogIn()
    {
        var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnEmailLoginSuccess, OnEmailLoginFailure);
    }

    public static string ReturnMobileID()
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

    public void OpenAddLogin()
    {
        AddLoginPanel.SetActive(true);
    }

    public void OnClickAddLogin()
    {
        //we have account created through device id, but user clicked add mail and password to his account
        var addLoginRequest = new AddUsernamePasswordRequest { Email = userEmail, Password = userPassword, Username = userName };
        PlayFabClientAPI.AddUsernamePassword(addLoginRequest, OnAddLoginSuccess, OnRegisterFailure);
    }

    private void OnAddLoginSuccess(AddUsernamePasswordResult result)
    {
        Debug.Log("Add Login Success, you made your first successful API call!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        AddLoginPanel.SetActive(false);
    }

    /// <summary>
    /// updating statistics by client (commented because changed to sending request to update statistics by server, so leater we can check on server if updated statistics aren't too big / too often, add sanity checks.
    /// 
    /// For cheat prevention client is not able to update stats. to update login to playfab > YourGame > Settings (on left) > API Features > Allow client to post player statistics > Save  button
    /// </summary>
    //public void SetStats()
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

    public void StartCloudUpdatePlayerStats()
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

    public void GetStats()
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
            Debug.Log("Received Stats " +
                string.Join(", ", result.Statistics.Select(x => x.StatisticName + " " + x.Value))
            );
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }

    public void GetLeaderboard()
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

    //[Button]
    public void GetPlayerData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest()
        {
            PlayFabId = PlayFabSettings.staticPlayer.PlayFabId, // Get data for the current player. you can also set it in login & register success callback "string playFabId = result.PlayFabId;"
            Keys = null, // Get all keys
        },
        result =>
        {
            if (result.Data == null || !result.Data.ContainsKey("Skins"))
            {
                Debug.Log("Skins not set");
                return;
            }
            Debug.Log("Received skins string: " + result.Data["Skins"].Value);
            skinsString = result.Data["Skins"].Value;
        },
        error => { Debug.Log(error.GenerateErrorReport()); });

    }

    //[Button]
    public void SetPlayerData()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>()
            {
                { "Skins", skinsString }
            }
        },
        result =>
        {
            Debug.Log("Updated skins: " + skinsString);
        },
        error => { Debug.Log(error.GenerateErrorReport()); });
    }
}
