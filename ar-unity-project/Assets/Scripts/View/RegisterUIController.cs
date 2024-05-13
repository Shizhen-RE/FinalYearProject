using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RegisterUIController : MonoBehaviour
{
    public static RegisterUIController instance;
    
    public Button RegisterButton;
    public Button Register_NextButton;
    public Button BackButton;

    public TMP_InputField Register_Email;
    public TMP_InputField Register_Password;
    public TMP_InputField Register_ConfirmPassword;
    public TMP_Text Register_WarningText;
    public TMP_Text Register_ConfirmText;

    void Awake()
    {
        if (instance == null){
            instance = this;

        }else if (instance != null){
            Debug.Log("RegisterUIController: instance already exists, destroying object");
            Destroy(this);
        }

        Register_Email = GameObject.Find("Register_Email").GetComponent<TMP_InputField>();
        Register_Password = GameObject.Find("Register_Password").GetComponent<TMP_InputField>();
        Register_ConfirmPassword = GameObject.Find("Register_ConfirmPassword").GetComponent<TMP_InputField>();
        Register_WarningText = GameObject.Find("Register_WarningText").GetComponent<TMP_Text>();
        Register_ConfirmText = GameObject.Find("Register_ConfirmText").GetComponent<TMP_Text>();

        RegisterButton = GameObject.Find("RegisterButton").GetComponent<Button>();
        RegisterButton.onClick.AddListener(() => {FirebaseManager.instance.OnClickRegister(Register_Email.text, Register_Password.text, Register_ConfirmPassword.text);});

        Register_NextButton = GameObject.Find("Register_NextButton").GetComponent<Button>();
        Register_NextButton.onClick.AddListener(UIManager.instance.Goto_NewUserScene);

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

    }

    // Start is called before the first frame update
    void Start()
    {
        Register_NextButton.gameObject.SetActive(false);

        // clear warnings
        Register_WarningText.text = "";
        Register_ConfirmText.text = "";

        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
