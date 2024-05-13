using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserProfileUIController : MonoBehaviour
{
    public Button BackButton;
    public RawImage AvatarImage;
    public Image AvatarBorder;
    public Button AvatarButton;
    public TMP_InputField Username;
    public TMP_InputField Email;
    //public TMP_InputField HomeAddress;
    //public TMP_InputField Birthday;
    public Button EditButton;
    public Button DoneButton;

    private string avatarImagePath = "";

    void Awake(){
        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        EditButton = GameObject.Find("EditButton").GetComponent<Button>();
        EditButton.onClick.AddListener(onClickEdit);

        DoneButton = GameObject.Find("DoneButton").GetComponent<Button>();
        DoneButton.onClick.AddListener(onClickDone);

        AvatarButton = GameObject.Find("ProfilePic").GetComponent<Button>();
        AvatarButton.onClick.AddListener(() => StartCoroutine(AvatarRoutine()));

        AvatarImage = GameObject.Find("ProfilePic").GetComponent<RawImage>();

        AvatarBorder = GameObject.Find("AvatarBorder").GetComponent<Image>();

        Username = GameObject.Find("Username").GetComponent<TMP_InputField>();

        Email = GameObject.Find("Email").GetComponent<TMP_InputField>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // get user profile pic from firebase
        if (FirebaseManager.instance.user != null){
            StartCoroutine(updateAvatarImage());
            Username.text = FirebaseManager.instance.user.DisplayName;
            Email.text = FirebaseManager.instance.user.Email;
        }

        // update text display size according to user preference
        UIManager.UpdateTextSize();

        // disable UI editing
        DoneButton.gameObject.SetActive(false);
        AvatarButton.interactable = false;
        Username.interactable = false;
        Email.interactable = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void onClickEdit(){
        // enable input
        AvatarButton.interactable = true;
        Username.interactable = true;
        Email.interactable = true;

        // highlight editable components
        AvatarBorder.color = UIManager.THEME_COLOR;
        Username.GetComponent<Image>().color = UIManager.THEME_COLOR;
        Email.GetComponent<Image>().color = UIManager.THEME_COLOR;

        EditButton.gameObject.SetActive(false);
        DoneButton.gameObject.SetActive(true);
    }

    private void onClickDone(){
        // disable input
        AvatarButton.interactable = false;
        Username.interactable = false;
        Email.interactable = false;

        // remove highlight
        AvatarBorder.color = Color.white;
        Username.GetComponent<Image>().color = Color.white;
        Email.GetComponent<Image>().color = Color.white;

        // update user profile in firebase
        Firebase.Auth.FirebaseUser user = FirebaseManager.instance.user;
        if (Username.text != user.DisplayName || !String.IsNullOrEmpty(avatarImagePath)){
            StartCoroutine(UserManager.instance.UpdateUser(false, Username.text, avatarImagePath));
        }
        if (Email.text != user.Email){
            StartCoroutine(UserManager.instance.SetUserEmail(Email.text));
        }

        EditButton.gameObject.SetActive(true);
        DoneButton.gameObject.SetActive(false);
    }

    IEnumerator AvatarRoutine(){
        IEnumerator<(string, Texture2D)> request = Utils.PickImageFromAlbum();
        (string, Texture2D) result = (null, null);
        while (request.MoveNext()) {
            result = request.Current;
            yield return null;
        }

        if (result != (null, null)){
            avatarImagePath = result.Item1;
            AvatarImage.texture = result.Item2;
            Debug.LogFormat("Update user photo URL: {0}", avatarImagePath);
        }else{
            Debug.LogError("Cannot load the image.");
        }
    }

    private IEnumerator updateAvatarImage(){
        yield return UserManager.instance.GetUserProfileTexture();
        AvatarImage.texture = UserManager.instance.userProfileTexture;
    }

}
