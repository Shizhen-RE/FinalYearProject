using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    private Button BackButton;
    private Button SmallFontSizeButton;
    private Button NormalFontSizeButton;
    private Button LargeFontSizeButton;
    private Toggle SoundToggle;
    private Toggle RecommendationToggle;
    private Toggle CommentDisplayToggle;
    private Slider DisplayDensitySlider;
    private Slider DisplaySpeedSlider;

    private int previousFontSize;

    void Awake(){
        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        SmallFontSizeButton = GameObject.Find("SmallFontSize").GetComponent<Button>();
        SmallFontSizeButton.onClick.AddListener(onClickSmallFontSize);
        NormalFontSizeButton = GameObject.Find("NormalFontSize").GetComponent<Button>();
        NormalFontSizeButton.onClick.AddListener(onClickNormalFontSize);
        LargeFontSizeButton = GameObject.Find("LargeFontSize").GetComponent<Button>();
        LargeFontSizeButton.onClick.AddListener(onClickLargeFontSize);

        // TODO: save sound settings in PlayerPreference
        SoundToggle = GameObject.Find("SoundToggle").GetComponent<Toggle>();
        SoundToggle.onValueChanged.AddListener(delegate {
            if (SoundToggle.isOn){
                AudioListener.pause = true;
            }else{
                AudioListener.pause = false;
            }
        });

        RecommendationToggle = GameObject.Find("RecommendationToggle").GetComponent<Toggle>();
        RecommendationToggle.onValueChanged.AddListener(delegate {
            PlayerPrefs.SetInt(UIManager.ENABLE_RECOMMENDATION, Convert.ToInt32(RecommendationToggle.isOn));
            PlayerPrefs.Save();
        });

        CommentDisplayToggle = GameObject.Find("CommentDisplayToggle").GetComponent<Toggle>();
        CommentDisplayToggle.onValueChanged.AddListener(delegate {
            PlayerPrefs.SetInt(UIManager.DISPLAY_COMMENTS, Convert.ToInt32(CommentDisplayToggle.isOn));
            PlayerPrefs.Save();
        });

        DisplayDensitySlider = GameObject.Find("DisplayDensitySlider").GetComponent<Slider>();
        DisplayDensitySlider.value = PlayerPrefs.GetFloat(UIManager.DISPLAY_DENSITY);
        DisplayDensitySlider.onValueChanged.AddListener(delegate {
            displayDensityHandler();
        });

        DisplaySpeedSlider = GameObject.Find("DisplaySpeedSlider").GetComponent<Slider>();
        DisplaySpeedSlider.value = PlayerPrefs.GetFloat(UIManager.DISPLAY_SPEED);
        DisplaySpeedSlider.onValueChanged.AddListener(delegate {
            displaySpeedHandler();
        });
    }

    // Start is called before the first frame update
    void Start()
    {
        // toggle the recommendation checkbox
        toggleRecommendationMode();

        // toggle the comment display checkbox
        toggleCommentDisplay();

        // highlight the remembered font size preference
        toggleFontSizeButtons();

        // update display
        previousFontSize = UIManager.NORMAL_FONT_SIZE;
        updateTextSize();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void toggleRecommendationMode(){
        if (!PlayerPrefs.HasKey(UIManager.ENABLE_RECOMMENDATION)){
            PlayerPrefs.SetInt(UIManager.ENABLE_RECOMMENDATION, 1);
            PlayerPrefs.Save();
        }

        bool status = PlayerPrefs.GetInt(UIManager.ENABLE_RECOMMENDATION) == 1 ? true : false;
        RecommendationToggle.isOn = status;
    }

    private void toggleCommentDisplay(){
        if (!PlayerPrefs.HasKey(UIManager.DISPLAY_COMMENTS)){
            PlayerPrefs.SetInt(UIManager.DISPLAY_COMMENTS, 1);
            PlayerPrefs.Save();
        }

        bool status = PlayerPrefs.GetInt(UIManager.DISPLAY_COMMENTS) == 1 ? true : false;
        CommentDisplayToggle.isOn = status;
    }

    private void onClickSmallFontSize(){
        previousFontSize = PlayerPrefs.GetInt(UIManager.TEXT_SIZE);

        // save text size preference 
        PlayerPrefs.SetInt(UIManager.TEXT_SIZE, UIManager.SMALL_FONT_SIZE);
        PlayerPrefs.Save();

        // highlight font size button
        toggleFontSizeButtons();

        // update display
        updateTextSize();
    }

    private void displayDensityHandler(){
        float value = DisplayDensitySlider.value;

        Debug.LogFormat("Display density changed to: {0}", value);

        // update user preference
        PlayerPrefs.SetFloat(UIManager.DISPLAY_DENSITY, value);
        PlayerPrefs.Save();
    }

    private void displaySpeedHandler(){
        float value = DisplaySpeedSlider.value;

        Debug.LogFormat("Display speed changed to: {0}", value);

        // update user preference
        PlayerPrefs.SetFloat(UIManager.DISPLAY_SPEED, value);
        PlayerPrefs.Save();
    }

    private void onClickNormalFontSize(){
        previousFontSize = PlayerPrefs.GetInt(UIManager.TEXT_SIZE);

        // save text size preference 
        PlayerPrefs.SetInt(UIManager.TEXT_SIZE, UIManager.NORMAL_FONT_SIZE);
        PlayerPrefs.Save();

        // highlight font size button
        toggleFontSizeButtons();

        // update display
        updateTextSize();
    }

    private void onClickLargeFontSize(){
        previousFontSize = PlayerPrefs.GetInt(UIManager.TEXT_SIZE);

        // save text size preference 
        PlayerPrefs.SetInt(UIManager.TEXT_SIZE, UIManager.LARGE_FONT_SIZE);
        PlayerPrefs.Save();

        // highlight font size button
        toggleFontSizeButtons();

        // update display
        updateTextSize();
    }

    private void toggleFontSizeButtons(){
        // deactivate all
        SmallFontSizeButton.GetComponent<Image>().color = Color.white;
        NormalFontSizeButton.GetComponent<Image>().color = Color.white;
        LargeFontSizeButton.GetComponent<Image>().color = Color.white;

        if (!PlayerPrefs.HasKey(UIManager.TEXT_SIZE)){
            PlayerPrefs.SetInt(UIManager.TEXT_SIZE, UIManager.NORMAL_FONT_SIZE);
            PlayerPrefs.Save();
        }

        // highlight the remembered/selected font size
        switch(PlayerPrefs.GetInt(UIManager.TEXT_SIZE)){
            case UIManager.SMALL_FONT_SIZE:
                SmallFontSizeButton.GetComponent<Image>().color = UIManager.THEME_COLOR;
                break;
            case UIManager.NORMAL_FONT_SIZE:
                NormalFontSizeButton.GetComponent<Image>().color = UIManager.THEME_COLOR;
                break;
            case UIManager.LARGE_FONT_SIZE:
                LargeFontSizeButton.GetComponent<Image>().color = UIManager.THEME_COLOR;
                break;
            default:
                break;
        }
    }

    // update font size of the text labels in scene with regards to the user selected value
    private void updateTextSize(){
        int textSize = PlayerPrefs.GetInt(UIManager.TEXT_SIZE);

        TMP_Text[] textLabels = FindObjectsOfType<TMP_Text>();
        foreach (TMP_Text label in textLabels){
            label.fontSize = (label.fontSize/previousFontSize) * textSize;
        }
    }
}
