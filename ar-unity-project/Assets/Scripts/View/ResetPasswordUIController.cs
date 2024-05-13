using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResetPasswordUIController : MonoBehaviour
{
    public static ResetPasswordUIController instance;
    
    public Button ResetPasswordButton;
    public Button BackButton;

    public TMP_InputField ResetPassword_Email;
    public TMP_InputField ResetPassword_ConfirmEmail;
    public TMP_Text ResetPassword_WarningText;
    public TMP_Text ResetPassword_ConfirmText;

    void Awake()
    {
        if (instance == null){
            instance = this;

        }else if (instance != null){
            Debug.Log("ResetPasswordUIController: instance already exists, destroying object");
            Destroy(this);
        }

        ResetPassword_Email = GameObject.Find("ResetPassword_Email").GetComponent<TMP_InputField>();
        ResetPassword_ConfirmEmail = GameObject.Find("ResetPassword_ConfirmEmail").GetComponent<TMP_InputField>();
        ResetPassword_WarningText = GameObject.Find("ResetPassword_WarningText").GetComponent<TMP_Text>();
        ResetPassword_ConfirmText = GameObject.Find("ResetPassword_ConfirmText").GetComponent<TMP_Text>();

        ResetPasswordButton = GameObject.Find("ResetPasswordButton").GetComponent<Button>();
        ResetPasswordButton.onClick.AddListener(() => {FirebaseManager.instance.OnClickResetPassword(ResetPassword_Email.text, ResetPassword_ConfirmEmail.text);});

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);
    }

    // Start is called before the first frame update
    void Start()
    {
        // clear warnings
        ResetPassword_WarningText.text = "";
        ResetPassword_ConfirmText.text = "";

        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
