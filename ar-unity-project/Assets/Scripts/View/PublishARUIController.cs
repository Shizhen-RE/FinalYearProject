using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

enum ARType
{
    Target,
    Text,
    Image
}

enum ARTextStyle
{
    Clear,
    Solid,
    Postit,

    /* Keep last */
    Count
}

public enum ARTextColor
{
    White,
    Black,
    Red,
    Orange,
    Yellow,
    Green,
    Blue,

    /* Keep last */
    Count
}

public class PublishARUIController : MonoBehaviour
{
    public SpawnableManager spawnableManager;
    public SwipeDetector swipeDetector;

    public List<GameObject> TextPrefabs = new List<GameObject>();
    public GameObject ImagePrefab;

    // UI components - base canvas
    public GameObject BaseCanvas;
    public Button BackButton;
    public Button TextInputButton;
    public Button ImageInputButton;
    public Button PlaceButton;

    // UI components - input canvas
    public GameObject InputCanvas;
    public TMP_Dropdown TopicsDropdown;
    public GameObject StyleContainer;
    public GameObject ColorContainer;
    public GameObject UploadButtonContainer;
    public TMP_Text TextInputErrorMessage;
    public GameObject PublishTextButtonContainer;
    public Button PublishTextButton;
    public TMP_Text ImageInputErrorMessage;
    public GameObject PublishImageButtonContainer;
    public Button PublishImageButton;
    public List<GameObject> TextStyleButtons = new List<GameObject>();
    public Slider SizeSlider;
    public EventTrigger CCWRotationTrigger;
    public EventTrigger CWRotationTrigger;
    public List<GameObject> TextColorButtons = new List<GameObject>();
    public Button ImageUploadButton;

    // UI components - info dialogs
    public GameObject OKDialog;
    public GameObject ErrorDialog;
    public Button DiscardButton;
    public Button RetryButton;

    // variables
    private Texture2D imageTexture;
    private List<Topic> topic_list;
    private List<string> topic_names = new List<string>();
    private string selected_topic;
    private GameObject selected_color_button;
    private GameObject selected_style_button;

    // default
    private string DEFAULT_TOPIC = "-- select topic --";
    private float[] DEFAULT_TEXT_COLOR = new float[] {Color.white.r, Color.white.g, Color.white.b};
    private string DEFAULT_TEXT_STYLE = "CLEAR_BG";
    private int DEFAULT_OBJ_HEIGHT = 100;
    private int DEFAULT_OBJ_WIDTH = 100;
    private int PREVIEW_LENGTH = 800;
    private int SCREENSHOT_PADDING = 100;

    ARType arType = ARType.Target;
    ARTextStyle arTextStyle = ARTextStyle.Clear;
    ARTextColor arTextColor = ARTextColor.White;

    string[] inputManagerStyles = new string[] { "CLEAR_BG", "SOLID_BG", "POSTIT" };

    int rotationDirection = 1; /* 1 or -1 */
    float rotationSpeed = 90.0f; /* degrees/second */
    bool rotating = false;

    void Awake()
    {
        /* Base canvas */
        BaseCanvas.SetActive(true);

        BackButton.gameObject.SetActive(true);
        BackButton.onClick.AddListener(UIManager.instance.Goto_PublishScene);

        TextInputButton.gameObject.SetActive(false);
        TextInputButton.onClick.AddListener(onClickTextInput);

        ImageInputButton.gameObject.SetActive(false);
        ImageInputButton.onClick.AddListener(onClickImageInput);

        PlaceButton.gameObject.SetActive(true);
        PlaceButton.onClick.AddListener(onClickPlace);

        /* Input canvas */
        InputCanvas.SetActive(false);

        TopicsDropdown.gameObject.SetActive(true);
        TopicsDropdown.onValueChanged.AddListener(delegate {
            if (TopicsDropdown.value == 0){
                selected_topic = "";
            }else{
                selected_topic = topic_list[TopicsDropdown.value-1].ID;
            }
        });

        StyleContainer.SetActive(false);
        ColorContainer.SetActive(false);
        UploadButtonContainer.SetActive(false);

        PublishTextButton.onClick.AddListener(onClickPublishText);
        PublishTextButtonContainer.SetActive(false);

        PublishImageButton.onClick.AddListener(onClickPublishImage);
        PublishImageButtonContainer.SetActive(false);

        StyleContainer.SetActive(false);
        for (int i = 0; i < (int)ARTextStyle.Count; i++) {
            int style = i; /* For lambda */
            Button button = TextStyleButtons[style].GetComponent<Button>();
            button.onClick.AddListener(() => onClickTextStyle((ARTextStyle)style));
        }

        SizeSlider.gameObject.SetActive(true);
        SizeSlider.onValueChanged.AddListener(delegate { onSizeChanged(); });

        EventTrigger.Entry entry;

        CCWRotationTrigger.gameObject.SetActive(true);
        /* Button down */
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => {
            rotationDirection = -1;
            rotating = true;
        });
        CCWRotationTrigger.triggers.Add(entry);
        /* Button up */
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((data) => rotating = false);
        CCWRotationTrigger.triggers.Add(entry);

        CWRotationTrigger.gameObject.SetActive(true);
        /* Button down */
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => {
            rotationDirection = 1;
            rotating = true;
        });
        CWRotationTrigger.triggers.Add(entry);
        /* Button up */
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((data) => rotating = false);
        CWRotationTrigger.triggers.Add(entry);

        for (int i = 0; i < (int)ARTextColor.Count; i++) {
            int color = i; /* For lambda */
            Button button = TextColorButtons[color].GetComponent<Button>();
            button.onClick.AddListener(() => onClickTextColor((ARTextColor)color));
        }

        ImageUploadButton.gameObject.SetActive(true);
        ImageUploadButton.onClick.AddListener(() => StartCoroutine(UploadImageRoutine()));

        // info dialogs
        OKDialog.SetActive(false);
        ErrorDialog.SetActive(false);

        RetryButton.onClick.AddListener(publish);

        DiscardButton.onClick.AddListener(() => {
            InputManager.instance.message = new Message();
            UIManager.instance.Goto_MainScene();
        });

    }

    IEnumerator Start()
    {
        if (TopicManager.instance.topic_list == null)
            StartCoroutine(getTopicsForDropdown());
        else {
            topic_list = TopicManager.instance.topic_list;
            getTopicNames();
        }

        UIManager.UpdateTextSize();

        swipeDetector.Active = false;
        swipeDetector.OnSwipeDown.AddListener(() => InputCanvas.SetActive(false));
        swipeDetector.OnSwipeUp.AddListener(() => InputCanvas.SetActive(true));

        // update input manager with default settings
        InputManager.instance.message = new Message {
            User = FirebaseManager.instance.user.UserId,
            IsAR = true,
            Size = new float[] {DEFAULT_OBJ_WIDTH, DEFAULT_OBJ_HEIGHT},
            Color = (int)arTextColor, // white
            Scale = SizeSlider.value / 100.0f
        };

        /* Wait until target is placed */
        #if !UNITY_EDITOR
        PlaceButton.interactable = false;
        while (!spawnableManager.Initialized)
            yield return null;
        PlaceButton.interactable = true;
        #else
        yield return null;
        #endif
    }

    void Update()
    {
        if (rotating)
            spawnableManager.target.transform.Rotate(0.0f, rotationDirection * rotationSpeed * Time.deltaTime, 0.0f);
    }

    void onClickPlace()
    {
        PlaceButton.gameObject.SetActive(false);
        TextInputButton.gameObject.SetActive(true);
        ImageInputButton.gameObject.SetActive(true);
    }

    void onClickTextInput()
    {
        InputCanvas.SetActive(true);

        swipeDetector.Active = true;

        StyleContainer.SetActive(true);
        ColorContainer.SetActive(true);
        PublishTextButtonContainer.SetActive(true);
        UploadButtonContainer.SetActive(false);
        PublishImageButtonContainer.SetActive(false);

        if (arType != ARType.Text) {
            InstantiateTextPrefab();
            InputManager.instance.message.Style = DEFAULT_TEXT_STYLE;
            arType = ARType.Text;
        }
    }

    void onClickImageInput()
    {
        InputCanvas.SetActive(true);

        swipeDetector.Active = true;

        StyleContainer.SetActive(false);
        ColorContainer.SetActive(false);
        PublishTextButtonContainer.SetActive(false);
        UploadButtonContainer.SetActive(true);
        PublishImageButtonContainer.SetActive(true);

        if (arType != ARType.Image) {
            spawnableManager.InstantiateAtTarget(ImagePrefab);
            InputManager.instance.message.Style = "AR_IMAGE";
            arType = ARType.Image;
        }
    }

    void InstantiateTextPrefab()
    {
        spawnableManager.InstantiateAtTarget(TextPrefabs[(int)arTextStyle]);
        GameObject target = spawnableManager.target;
        GameObject content = target.transform.Find("Canvas/Content").gameObject;
        TMP_InputField textInput = content.GetComponent<TMP_InputField>();
        textInput.onSubmit.AddListener((string input) => InputManager.instance.message.Content = input);
        if (InputManager.instance.message.Style != "AR_IMAGE"){
            textInput.text = InputManager.instance.message.Content;
        }

        TextMeshProUGUI foreground = content.GetComponent<TextMeshProUGUI>();
        Color color = TextColorButtons[(int)arTextColor].transform.Find("Image").GetComponent<Image>().color;
        /* Set foreground color */
        switch (arTextStyle) {
        case ARTextStyle.Clear:
            foreground.color = color;
            break;
        case ARTextStyle.Solid:
        case ARTextStyle.Postit:
            switch (arTextColor) {
            case ARTextColor.White:
            case ARTextColor.Yellow:
                foreground.color = Color.black;
                break;
            default:
                foreground.color = Color.white;
                break;
            }
            break;
        }

        /* Set background color */
        switch (arTextStyle) {
        case ARTextStyle.Solid:
            target.transform.Find("Canvas/Background").GetComponent<Image>().color = color;
            break;
        case ARTextStyle.Postit:
            target.transform.Find("Canvas/Background").GetComponent<RawImage>().color = color;
            break;
        }
    }

    void onClickTextStyle(ARTextStyle arTextStyle)
    {
        if (this.arTextStyle == arTextStyle) return;

        TextStyleButtons[(int)this.arTextStyle].GetComponent<Image>().color = Color.white;
        TextStyleButtons[(int)arTextStyle].GetComponent<Image>().color = UIManager.THEME_COLOR;
        this.arTextStyle = arTextStyle;
        InstantiateTextPrefab();
        InputManager.instance.message.Style = inputManagerStyles[(int)arTextStyle];
    }

    void onClickTextColor(ARTextColor arTextColor)
    {
        if (this.arTextColor == arTextColor) return;

        TextColorButtons[(int)this.arTextColor].GetComponent<Image>().color = Color.black;
        TextColorButtons[(int)arTextColor].GetComponent<Image>().color = UIManager.THEME_COLOR;
        this.arTextColor = arTextColor;
        InstantiateTextPrefab();
        // Color color = TextColorButtons[(int)arTextColor].transform.Find("Image").GetComponent<Image>().color;
        InputManager.instance.message.Color = (int)arTextColor;
    }

    IEnumerator UploadImageRoutine()
    {
        IEnumerator<(string, Texture2D)> request = Utils.PickImageFromAlbum();
        (string, Texture2D) result = (null, null);
        while (request.MoveNext()) {
            result = request.Current;
            yield return null;
        }

        Texture2D texture = result.Item2;
        Debug.LogFormat("texture.format: {0}", texture.format.ToString());

        // spawnableManager.InstantiateAtTarget(ImagePrefab);
        GameObject target = spawnableManager.target;
        RawImage image = target.transform.Find("Canvas/Content").GetComponent<RawImage>();
        RectTransform transform = (RectTransform)image.transform;
        float aspectRatio = (float)texture.width/texture.height;
        /* Image's largest dimension corresponds to size slider */
        if (texture.width > texture.height) {
            transform.sizeDelta = new Vector2(transform.rect.width, transform.rect.height/aspectRatio);
        } else {
            transform.sizeDelta = new Vector2(transform.rect.width*aspectRatio, transform.rect.height);
        }
        image.texture = texture;

        InputManager.instance.message.Content = Convert.ToBase64String(
            ImageConversion.EncodeArrayToPNG(texture.GetRawTextureData(), texture.graphicsFormat, (uint)texture.width, (uint)texture.height)
        );
        InputManager.instance.message.ImageFormat = texture.format.ToString();
        InputManager.instance.message.Size = new float[]{texture.width, texture.height};   // origial width and height
    }

    void onSizeChanged()
    {
        /* Scale = Meters / 100 */
        float scale = SizeSlider.value / 100.0f;
        Debug.LogFormat("PublishARUIController: scale {0}", scale);
        spawnableManager.target.transform.Find("Canvas").localScale = new Vector3(scale, scale, 1.0f);
        InputManager.instance.message.Scale = scale;
    }

    void onClickPublishText()
    {
        if (String.IsNullOrEmpty(InputManager.instance.message.Content))
            TextInputErrorMessage.text = "Cannot publish empty message.";
        else if (String.IsNullOrEmpty(selected_topic))
            TextInputErrorMessage.text = "Please select a topic from the dropdown.";
        else {
            // TODO: validate input

            // clear error message
            TextInputErrorMessage.text = "";

            // take screenshot for preview
            StartCoroutine(screenshotThenPublish());
        }
    }

    void onClickPublishImage()
    {
        if (String.IsNullOrEmpty(InputManager.instance.message.Content))
            ImageInputErrorMessage.text = "Cannot publish empty message.";
        else if (String.IsNullOrEmpty(selected_topic))
            ImageInputErrorMessage.text = "Please select a topic from the dropdown.";
        else {
            // TODO: validate input image size

            // clear error message
            ImageInputErrorMessage.text = "";

            // take screenshot for preview
            StartCoroutine(screenshotThenPublish());
        }
    }

    IEnumerator getTopics()
    {
        yield return TopicManager.instance.GetTopicsList(callback: (topics)=>{topic_list = topics;});
    }

    IEnumerator getTopicsForDropdown()
    {
        // wait for all topics
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{StartCoroutine(getTopics());},
            loadingCondition: ()=>{return topic_list == null;},
            errorCondition: ()=>{return false; /* TODO */ },
            timeoutSeconds: 30.0f
        );

        getTopicNames();
    }

    void getTopicNames()
    {
        topic_list.Sort((l, r) => String.Compare(l.Name, r.Name)); // sort alphabetically
        foreach (Topic t in topic_list) {
            topic_names.Add(t.Name);
        }
        topic_names.Insert(0, DEFAULT_TOPIC);

        // set values for the topics dropdown
        TopicsDropdown.ClearOptions();
        TopicsDropdown.AddOptions(topic_names);
        TopicsDropdown.value = 0;
        selected_topic = "";
    }

    IEnumerator screenshotThenPublish()
    {
        yield return getPreviewScreenshot();

        // try to publish
        publish();
    }

    IEnumerator getPreviewScreenshot()
    {
        BaseCanvas.SetActive(false);
        InputCanvas.SetActive(false);

        yield return new WaitForEndOfFrame();

        /* Get largest possible square around center of screen */
        int length = Screen.height > Screen.width ? Screen.width : Screen.height;
        int left = (Screen.width / 2) - (length / 2);
        int top = (Screen.height / 2) - (length / 2);

        /* Take screenshot */
        Texture2D previewTexture = new Texture2D(length, length);
        previewTexture.ReadPixels(new Rect(left, top, length, length), 0, 0);
        previewTexture.Apply();

        /* Reduce size by converting to JPG */
        InputManager.instance.message.Preview = Convert.ToBase64String(
            ImageConversion.EncodeArrayToJPG(previewTexture.GetRawTextureData(),
                                             previewTexture.graphicsFormat,
                                             (uint)length, (uint)length)
        );

        BaseCanvas.SetActive(true);
    }

    void publish()
    {
        // spawnableManager.Retargeting = false;

        // update input manager
        #if UNITY_EDITOR
        InputManager.instance.message.Coordinates = new double[] {43.471754, -80.523660, 0.0};
        #else
        InputManager.instance.message.Coordinates = new double[] {
            ARContentManager.instance.latitude, ARContentManager.instance.longitude, ARContentManager.instance.altitude
        };
        #endif

        // get the object rotation
        Quaternion rotation = spawnableManager.target.transform.rotation;
        InputManager.instance.message.Rotation = new float[] {
            rotation.x, rotation.y, rotation.z, rotation.w
        };

        DateTime currentTime = DateTime.UtcNow;
        InputManager.instance.message.Timestamp = currentTime;
        InputManager.instance.message.Topic = selected_topic;

        // public the content
        if (InputManager.instance.Publish()) {
            // success
            ErrorDialog.SetActive(false);
            swipeDetector.Active = false;
            StartCoroutine(waitThenGotoMainScene());
        } else {
            // error
            ErrorDialog.SetActive(true);
        }
    }

    IEnumerator waitThenGotoMainScene()
    {
        OKDialog.SetActive(true);
        yield return new WaitForSeconds(1);
        UIManager.instance.Goto_MainScene();
    }
}
