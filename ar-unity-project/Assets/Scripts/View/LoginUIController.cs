using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUIController : MonoBehaviour
{
    public static LoginUIController instance;

    public TMP_InputField Login_Email;
    public TMP_InputField Login_Password;
    public TMP_Text Login_WarningText;
    public TMP_Text Login_ConfirmText;

    public Button LoginButton;
    public Button Login_NewUserButton;
    public Button Login_ForgotPasswordButton;

    void Awake()
    {
        if (instance == null){
            instance = this;
            
        }else if (instance != null){
            Debug.Log("LoginUIController: instance already exists, destroying object");
            Destroy(this);
        }

        Login_Email = GameObject.Find("Login_Email").GetComponent<TMP_InputField>();
        Login_Password = GameObject.Find("Login_Password").GetComponent<TMP_InputField>();
        Login_Password.inputType = TMP_InputField.InputType.Password;
        Login_WarningText = GameObject.Find("Login_WarningText").GetComponent<TMP_Text>();
        Login_ConfirmText = GameObject.Find("Login_ConfirmText").GetComponent<TMP_Text>();

        LoginButton = GameObject.Find("LoginButton").GetComponent<Button>();
        LoginButton.onClick.AddListener(() => {FirebaseManager.instance.OnClickLogin(Login_Email.text, Login_Password.text);});

        Login_NewUserButton = GameObject.Find("Login_NewUserButton").GetComponent<Button>();
        Login_NewUserButton.onClick.AddListener(UIManager.instance.Goto_RegisterScene);

        Login_ForgotPasswordButton = GameObject.Find("Login_ForgotPasswordButton").GetComponent<Button>();
        Login_ForgotPasswordButton.onClick.AddListener(UIManager.instance.Goto_ResetPasswordScene);

        // clear warnings
        Login_WarningText.text = "";
        Login_ConfirmText.text = "";

    }

    void Start(){
        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
