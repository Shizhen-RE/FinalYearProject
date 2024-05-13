using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Dialog : MonoBehaviour
{
    public static GameObject Create(string name, GameObject prefab = null){
        GameObject dialogObject;
        if (prefab != null){
            dialogObject = GameObject.Instantiate( prefab ) as GameObject;
        }else{
            dialogObject = GameObject.Instantiate( Resources.Load("DialogCanvas") ) as GameObject;
        }

        dialogObject.name = name;
        return dialogObject;
    }

    public static void Load(string title = "", string body = "", string button1Text = "", string button2Text = "",
                            UnityAction button1Action = null, UnityAction button2Action = null){

        // error check 
        if ( String.IsNullOrEmpty(title) && String.IsNullOrEmpty(body) )
        {
            Debug.LogError("Error instantiating dialog box.");
            return;
        }

        // instantiate dialog
        GameObject.Find("DialogTitle").GetComponent<TMP_Text>().text = title;
        GameObject.Find("DialogBody").GetComponent<TMP_Text>().text = body;

        if (!String.IsNullOrEmpty(button1Text)){
            GameObject.Find("Button1Text").GetComponent<TMP_Text>().text = button1Text;
            if (button1Action != null){
                GameObject.Find("DialogButton1").GetComponent<Button>().onClick.AddListener(button1Action);
            }
        }else{
            // destroy button1
            GameObject.Find("DialogButton1").SetActive(false);
        }

        if (!String.IsNullOrEmpty(button2Text)){
            GameObject.Find("Button2Text").GetComponent<TMP_Text>().text = button2Text;
            if (button2Action != null){
                GameObject.Find("DialogButton2").GetComponent<Button>().onClick.AddListener(button2Action);
            }
        }else{
            // destroy button2
            GameObject.Find("DialogButton2").SetActive(false);
        }
        
        // adjust font size
        UIManager.UpdateTextSize();
    }

    public static void SetBody(string body){
        GameObject.Find("DialogBody").GetComponent<TMP_Text>().text = body;
    }
}