using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewUserUIController : MonoBehaviour
{
    public static NewUserUIController instance;

    // UI components
    public TMP_InputField NewUser_Username;
    public RawImage NewUser_ProfilePic;
    public TMP_Text NewUser_WarningText;
    public Button NewUser_ProfilePicButton;
    public Button OKButton;
    public Button NewUser_NextButton;
    public Button NewUser_BackButton;

    // variables
    public string ProfilePicPath;
    public Texture2D ProfilePicTexture;

    void Awake()
    {
        if (instance == null){
            instance = this;

        }else if (instance != null){
            Debug.Log("NewUserUIController: instance already exists, destroying object");
            Destroy(this);
        }

        NewUser_Username = GameObject.Find("NewUser_Username").GetComponent<TMP_InputField>();
        NewUser_WarningText = GameObject.Find("NewUser_WarningText").GetComponent<TMP_Text>();

        NewUser_ProfilePic = GameObject.Find("ProfilePic").GetComponent<RawImage>();
        NewUser_ProfilePicButton = GameObject.Find("ProfilePic").GetComponent<Button>();
        NewUser_ProfilePicButton.onClick.AddListener(() => StartCoroutine(AvatarRoutine()));

        OKButton = GameObject.Find("OKButton").GetComponent<Button>();
        OKButton.onClick.AddListener(onClickOK);

        NewUser_NextButton = GameObject.Find("NewUser_NextButton").GetComponent<Button>();
        NewUser_NextButton.onClick.AddListener(UIManager.instance.Goto_SubscriptionScene);
        NewUser_NextButton.gameObject.SetActive(false);

        NewUser_BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        NewUser_BackButton.onClick.AddListener(UIManager.instance.GoBack);

        // clear warnings
        NewUser_WarningText.text = "";
    }

    // Start is called before the first frame update
    void Start()
    {
        // update text display size according to user preference
        UIManager.UpdateTextSize();

    }

    IEnumerator AvatarRoutine(){
        IEnumerator<(string, Texture2D)> request = Utils.PickImageFromAlbum();
        (string, Texture2D) result = (null, null);
        while (request.MoveNext()) {
            result = request.Current;
            yield return null;
        }

        if (result != (null, null)){
            ProfilePicPath = result.Item1;
            ProfilePicTexture = result.Item2;
            NewUser_WarningText.text = "";
            NewUser_ProfilePic.texture = ProfilePicTexture;
            Debug.LogFormat("Update user photo URL: {0}", ProfilePicPath);
        }else{
            NewUser_WarningText.text = "Cannot load the image.";
        }
    }

    private void onClickOK(){
        string username = NewUser_Username.text;
        string path = ProfilePicPath;
        StartCoroutine(UserManager.instance.UpdateUser(true, username, path));
        NewUser_NextButton.gameObject.SetActive(true);
    }
}
