using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserHomePageUIController : MonoBehaviour
{
    public RawImage AvatarImage;
    public TMP_Text Username;
    public TMP_Text CurrentLocation;
    public TMP_Text TotalLikes;

    public Button BackButton;
    public Button UserProfileButton;
    public Button SubscriptionsButton;
    public Button TopicsButton;
    public Button CommentHistoryButton;
    public Button PostHistoryButton;
    public Button LocationsButton;
    public Button LogoutButton;


    void Awake(){
        AvatarImage = GameObject.Find("ProfilePic").GetComponent<RawImage>();

        Username = GameObject.Find("Username").GetComponent<TMP_Text>();
        CurrentLocation = GameObject.Find("CurrentLocation").GetComponent<TMP_Text>();
        TotalLikes = GameObject.Find("TotalLikes").GetComponent<TMP_Text>();

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.Goto_MainScene);

        UserProfileButton = GameObject.Find("UserProfileButton").GetComponent<Button>();
        UserProfileButton.onClick.AddListener(UIManager.instance.Goto_UserProfile);

        SubscriptionsButton = GameObject.Find("SubscriptionsButton").GetComponent<Button>();
        SubscriptionsButton.onClick.AddListener(UIManager.instance.Goto_UserSubscriptions);

        TopicsButton = GameObject.Find("TopicsButton").GetComponent<Button>();
        TopicsButton.onClick.AddListener(UIManager.instance.Goto_UserTopics);

        CommentHistoryButton = GameObject.Find("CommentHistoryButton").GetComponent<Button>();
        CommentHistoryButton.onClick.AddListener(UIManager.instance.Goto_CommentHistory);

        PostHistoryButton = GameObject.Find("PostHistoryButton").GetComponent<Button>();
        PostHistoryButton.onClick.AddListener(UIManager.instance.Goto_PostHistory);

        LocationsButton = GameObject.Find("LocationsButton").GetComponent<Button>();
        LocationsButton.onClick.AddListener(UIManager.instance.Goto_UserLocations);

        LogoutButton = GameObject.Find("LogoutButton").GetComponent<Button>();
        LogoutButton.onClick.AddListener(()=>{
            // logout user
            FirebaseManager.instance.auth.SignOut();
            // go to login scene
            UIManager.instance.Goto_LoginScene();
        });

    }

    // Start is called before the first frame update
    void Start()
    {
        // get user profile pic from firebase
        if (FirebaseManager.instance.user != null){
            StartCoroutine(updateAvatarImage());
            Username.text = FirebaseManager.instance.user.DisplayName;
        }

        // get user db meta
        DBUser user = UserManager.instance.GetDBUser();
        if (user != null){
            TotalLikes.text = user.TotalLikes;
            // maybe display more stuff here?
        }

        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    // Update is called once per frame
    void Update()
    {
        // get user current location
        if (ARContentManager.instance.tracking){
            CurrentLocation.text = ARContentManager.instance.latitude.ToString("0.000000") + ", " +
                                   ARContentManager.instance.longitude.ToString("0.000000");
        }else{
            CurrentLocation.text = "ERROR";
        }
    }

    private IEnumerator updateAvatarImage(){
        yield return UserManager.instance.GetUserProfileTexture();
        AvatarImage.texture = UserManager.instance.userProfileTexture;
    }

}
