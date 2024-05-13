using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BindLocationUIController : MonoBehaviour
{
    public GameObject ContentContainer;
    public GameObject ConfirmationContainer;

    public Button SubmitButton;
    public Button BackButton;

    void Awake()
    {
        ContentContainer = GameObject.Find("ContentContainer");

        ConfirmationContainer = GameObject.Find("ConfirmationContainer");
        ConfirmationContainer.SetActive(false);

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        SubmitButton = GameObject.Find("SubmitButton").GetComponent<Button>();
        SubmitButton.onClick.AddListener(onClickSubmit);

        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void onClickSubmit(){
        // TODO: handle bind location request
        Debug.Log("Bind location request submitted");

        // pop confirmation dialog
        ContentContainer.SetActive(false);
        ConfirmationContainer.SetActive(true);

        StartCoroutine(waitThenReturn());
    }

    private IEnumerator waitThenReturn(){
        yield return new WaitForSeconds(1);
        UIManager.instance.GoBack();
    }
}
