using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    // UI scene names
    public const string LOGIN_SCENE = "LoginScene";
    public const string REGISTER_SCENE = "RegisterScene";
    public const string NEW_USER_SCENE = "NewUserScene";
    public const string RESET_PWD_SCENE = "ResetPasswordScene";
    public const string SUBSCRIPTION_SCENE = "SubscriptionScene";
    public const string MAIN_SCENE = "ARScene";
    public const string PUBLISH_SCENE = "PublishScene";
    public const string PUBLISH_AR_SCENE = "PublishARScene";
    public const string USER_HOMEPAGE = "UserHomePageScene";
    public const string USER_PROFILE = "UserProfileScene";
    public const string USER_SUBSCRIPTIONS = "UserSubscriptionsScene";
    public const string USER_TOPICS = "UserTopicsScene";
    public const string COMMENT_HISTORY = "CommentHistoryScene";
    public const string POST_HISTORY = "PostHistoryScene";
    public const string USER_LOCATIONS = "UserLocationsScene";
    public const string SETTINGS_SCENE = "SettingsScene";
    public const string BIND_LOCATION_SCENE = "BindLocationScene";

    // state change variables
    private string currentSceneName;
    private string previousSceneName;

    // text display font sizes
    public const string TEXT_SIZE = "TextSize"; // key in PlayerPrefs
    public const int LARGE_FONT_SIZE = 70;
    public const int NORMAL_FONT_SIZE = 60;
    public const int SMALL_FONT_SIZE = 50;

    // recommendation mode enabling status
    public const string ENABLE_RECOMMENDATION = "EnableRecommendation"; // key in PlayerPrefs

    // comment display enabling status
    public const string DISPLAY_COMMENTS = "DisplayComments"; // key in PlayerPrefs

    // content display density
    public const string DISPLAY_DENSITY = "DisplayDensity"; // key in PlayerPrefs
    // comment display speed
    public const string DISPLAY_SPEED = "DisplaySpeed"; // key in PlayerPrefs

    // display colors
    public static Color THEME_COLOR = new Color(153/255.0f, 101/255.0f, 255/255.0f);
    public static Color THEME_COLOR_LIGHT = new Color(0.956f, 0.939f, 0.991f);
    public static Color LIGHT_GREY = new Color(159/255.0f, 159/255.0f, 159/255.0f);
    public static Color RED = new Color(255/255.0f, 106/255.0f, 101/255.0f);

    // loading animation
    public static string LoadErrorDialogName = "LoadErrorDialog";
    private static GameObject loadingAnimation = null;
    private static GameObject loadErrorDialog = null;

    private void Awake()
    {
        if (instance == null){
            instance = this;
            currentSceneName = LOGIN_SCENE;
            // write text size settings to user preference if not already exists
            if (!PlayerPrefs.HasKey(TEXT_SIZE)){
                PlayerPrefs.SetInt(TEXT_SIZE, NORMAL_FONT_SIZE);
            }
            if (!PlayerPrefs.HasKey(ENABLE_RECOMMENDATION)){
                PlayerPrefs.SetInt(ENABLE_RECOMMENDATION, Convert.ToInt32(true));
            }
            if (!PlayerPrefs.HasKey(DISPLAY_DENSITY)){
                PlayerPrefs.SetFloat(DISPLAY_DENSITY, 0.5f);
            }
            if (!PlayerPrefs.HasKey(DISPLAY_SPEED)){
                PlayerPrefs.SetFloat(DISPLAY_SPEED, 0.5f);
            }
            PlayerPrefs.Save();

            DontDestroyOnLoad(gameObject);

        }else if (instance != null){
            Debug.Log("UIManager instance already exists, destroying object");
            Destroy(this);
        }
    }

    private void Start(){

    }

    // update TMP_Text font size in the current scene according to user preference
    public static void UpdateTextSize(){
        int textSize = PlayerPrefs.GetInt(TEXT_SIZE);
        
        TMP_Text[] textLabels = FindObjectsOfType<TMP_Text>();
        foreach (TMP_Text label in textLabels)
        {
            label.fontSize = (label.fontSize/NORMAL_FONT_SIZE) * textSize;
        }
    }

    public IEnumerator ShowLoadingOnCondition(
        Action tryAction, 
        Func<bool> loadingCondition, 
        Func<bool> errorCondition, 
        float timeoutSeconds,
        bool skipFirstInvoke = false
    ){
        yield return tryLoading(tryAction, loadingCondition, errorCondition, timeoutSeconds, skipFirstInvoke);

        // if timed out or action resulted in error, pop up error dialog with retry option
        if (loadingCondition() || errorCondition()){
            loadErrorDialog = Dialog.Create(name: LoadErrorDialogName);
            Dialog.Load(
                title: "Error",
                button1Text: "Retry"
            );

            GameObject.Find("DialogButton1").GetComponent<Button>().onClick.AddListener(()=>{
                StartCoroutine(retryLoading(tryAction, loadingCondition, errorCondition, timeoutSeconds, skipFirstInvoke));
            });
            
            if (loadingCondition()){
                Dialog.SetBody("Request timed out, please retry.");
            }else{
                Dialog.SetBody("Error happened during request, please retry.");
            }

            while(loadingCondition() || errorCondition()){
                // Debug.Log("Waiting to clear load error");
                yield return new WaitForSeconds(1);
            }

        }else{
            Debug.Log("Load success.");
        }
    }

    public static void ShowLoading(){
        if (loadingAnimation == null){
            // Debug.Log("Instantiating loading animation");
            loadingAnimation = GameObject.Instantiate( Resources.Load("LoadingAnimationCanvas") ) as GameObject;
        }
    }

    public static void HideLoading(){
        Destroy(loadingAnimation);
        loadingAnimation = null;
    }

    private IEnumerator tryLoading(
        Action tryAction, 
        Func<bool> loadingCondition, 
        Func<bool> errorCondition, 
        float timeoutSeconds,
        bool skipFirstInvoke
    ){
        // calculate timeout timestamp
        float currentTime = Time.realtimeSinceStartup;
        float timeout = currentTime + timeoutSeconds;

        // instantiate loading animation
        ShowLoading();
        
        // invoke action that needs loading
        if (loadingCondition() && !skipFirstInvoke){
            tryAction();
        }

    
        // wait while action still on-going and not yet timeout 
        while (loadingCondition() && currentTime < timeout){
            // Debug.Log("Loading");
            currentTime = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(1);
        }
        
        // hide the loading animation when waiting is done
        HideLoading();
    }

    private IEnumerator retryLoading(
        Action tryAction, 
        Func<bool> loadingCondition, 
        Func<bool> errorCondition,
        float timeoutSeconds,
        bool skipFirstInvoke
    ){
        loadErrorDialog.SetActive(false);

        yield return tryLoading(tryAction, loadingCondition, errorCondition, timeoutSeconds, skipFirstInvoke);

        if (loadingCondition() || errorCondition()){
            loadErrorDialog.SetActive(true);
        }else{
            Debug.Log("Retry load success");
        }
    }

    public string getCurrentScene(){
        return currentSceneName;
    }

    public string getPreviousScene(){
        return previousSceneName;
    }

    public void Goto_LoginScene(){

        SceneManager.LoadScene(LOGIN_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = LOGIN_SCENE;
    }

    public void Goto_RegisterScene(){
        
        SceneManager.LoadScene(REGISTER_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = REGISTER_SCENE;
    }

    public void Goto_ResetPasswordScene(){
        
        SceneManager.LoadScene(RESET_PWD_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = RESET_PWD_SCENE;
    }

    public void Goto_NewUserScene(){
        
        SceneManager.LoadScene(NEW_USER_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = NEW_USER_SCENE;
    }

    public void Goto_SubscriptionScene(){
        SceneManager.LoadScene(SUBSCRIPTION_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = SUBSCRIPTION_SCENE;
    }

    public void Goto_MainScene(){
        SceneManager.LoadScene(MAIN_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = MAIN_SCENE;
    }

    public void Goto_PublishScene(){
        SceneManager.LoadScene(PUBLISH_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = PUBLISH_SCENE;
    }

    public void Goto_PublishARScene(){
        SceneManager.LoadScene(PUBLISH_AR_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = PUBLISH_AR_SCENE;
    }

    public void Goto_UserHomepage(){
        SceneManager.LoadScene(USER_HOMEPAGE);

        previousSceneName = currentSceneName;
        currentSceneName = USER_HOMEPAGE;
    }

    public void Goto_UserProfile(){
        SceneManager.LoadScene(USER_PROFILE);

        previousSceneName = currentSceneName;
        currentSceneName = USER_PROFILE;
    }

    public void Goto_UserSubscriptions(){
        SceneManager.LoadScene(USER_SUBSCRIPTIONS);

        previousSceneName = currentSceneName;
        currentSceneName = USER_SUBSCRIPTIONS;
    }

    public void Goto_UserTopics(){
        SceneManager.LoadScene(USER_TOPICS);

        previousSceneName = currentSceneName;
        currentSceneName = USER_TOPICS;
    }


    public void Goto_CommentHistory(){
        SceneManager.LoadScene(COMMENT_HISTORY);

        previousSceneName = currentSceneName;
        currentSceneName = COMMENT_HISTORY;
    }

    public void Goto_PostHistory(){
        SceneManager.LoadScene(POST_HISTORY);

        previousSceneName = currentSceneName;
        currentSceneName = POST_HISTORY;
    }

    public void Goto_UserLocations(){
        SceneManager.LoadScene(USER_LOCATIONS);

        previousSceneName = currentSceneName;
        currentSceneName = USER_LOCATIONS;
    }

    public void Goto_SettingsScene(){
        SceneManager.LoadScene(SETTINGS_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = SETTINGS_SCENE;
    }

    public void Goto_BindLocationScene(){
        SceneManager.LoadScene(BIND_LOCATION_SCENE);

        previousSceneName = currentSceneName;
        currentSceneName = BIND_LOCATION_SCENE;
    }

    public void GoBack(){
        SceneManager.LoadScene(previousSceneName);

        Debug.LogFormat("Previous: {0}, Current: {1}", previousSceneName, currentSceneName);
        
        string temp = currentSceneName;
        currentSceneName = previousSceneName;
        previousSceneName = temp;
    }
}