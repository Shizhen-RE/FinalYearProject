using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubscriptionUIController : MonoBehaviour
{
    // prefab
    public GameObject ButtonPrefab;

    // UI components - base canvas
    public GameObject RecommendedTopicsCanvas;
    public GameObject RecommendedList_Panel;
    public Button MoreTopicsButton;
    public Button NextButton;
    public Button BackButton;
    public Button OKButton;

    // UI components - more topics canvas
    public GameObject MoreTopicsCanvas;
    public GameObject TopicsList_Panel;
    public Button MoreTopicsBackButton;
    public Button MoreTopicsOKButton;
    public TMP_InputField SearchBarInput;
    public Button SearchButton;

    // UI components - pop up
    private GameObject dialogObject;

    // variable lists
    private List<Topic> recommended_topics = null;
    private List<Topic> all_topics = null;
    private List<Topic> subscribed_topics = null;
    private List<Topic> display_topics = null;
    private List<string> selected_topics = new List<string>(); // id of the selected topics

    // Presets for dynamic button rendering
    private Color BUTTON_COLOR = Color.white;
    private Color BUTTON_HIGHLIGHT_COLOR = UIManager.THEME_COLOR;
    private int BUTTON_HEIGHT = 100;
    private int MIN_BUTTON_WIDTH = 150;
    private int LAYOUT_PADDING = 20;
    private Vector3 LAYOUT_START_POS = new Vector3(20, -100, 0);
    private int HORRIZONTAL_END = 1020;

    // button layout variable
    private Vector3 current_pos;


    void Awake()
    {
        // base canvas
        RecommendedTopicsCanvas = GameObject.Find("Canvas");
        RecommendedList_Panel = GameObject.Find("RecommendedTopicsListPanel");

        NextButton = GameObject.Find("NextButton").GetComponent<Button>();
        NextButton.onClick.AddListener(() => {
            StartCoroutine(onClickOK());
        });
        //NextButton.gameObject.SetActive(false);

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(() => {
            UIManager.instance.GoBack();
        });

        OKButton = GameObject.Find("OKButton").GetComponent<Button>();
        OKButton.onClick.AddListener(()=>{
            StartCoroutine(onClickOK());
        });

        MoreTopicsButton = GameObject.Find("MoreTopicsButton").GetComponent<Button>();
        MoreTopicsButton.onClick.AddListener(()=>{
            StartCoroutine(onClickMoreTopics());
        });

        // more topics canvas
        MoreTopicsCanvas = GameObject.Find("MoreTopicsCanvas");

        SearchBarInput = GameObject.Find("SearchBarInput").GetComponent<TMP_InputField>();
        SearchBarInput.onValueChanged.AddListener((newVal) => onRefreshSearch(newVal));

        SearchButton = GameObject.Find("SearchButton").GetComponent<Button>();
        SearchButton.onClick.AddListener(() => {onClickSearchTopic(SearchBarInput.text);});

        TopicsList_Panel = GameObject.Find("TopicsListPanel");

        MoreTopicsBackButton = GameObject.Find("MoreTopicsBackButton").GetComponent<Button>();
        MoreTopicsBackButton.onClick.AddListener(() => {
            MoreTopicsCanvas.SetActive(false);
            RecommendedTopicsCanvas.SetActive(true);
        });

        MoreTopicsOKButton = GameObject.Find("MoreTopicsOKButton").GetComponent<Button>();
        MoreTopicsOKButton.onClick.AddListener(()=>{
            StartCoroutine(onClickOK());
        });

    }

    // Start is called before the first frame update
    void Start()
    {
        // update text display size according to user preference
        UIManager.UpdateTextSize();

        if (UIManager.instance.getPreviousScene() == UIManager.MAIN_SCENE){
            NextButton.gameObject.SetActive(false);
        }else{
            OKButton.gameObject.SetActive(false);
        }

        // hide the more topics canvas at start
        MoreTopicsCanvas.SetActive(false);

        // TEST:
        // all_topics = MockAllTopics();
        // recommended_topics = MockRecommendedTopics(all_topics);

        // create game objects for topics display
        StartCoroutine(renderRecommended());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator getSubscribedTopics(){
        yield return TopicManager.instance.GetSubscribedTopics(callback: (topics)=>{subscribed_topics = topics;});
    }

    private IEnumerator getAllTopics(){
        yield return TopicManager.instance.GetTopicsList(callback: (topics)=>{all_topics = topics;});
    }

    private IEnumerator getRecommendedTopics(){
        yield return TopicManager.instance.GetRecommendedTopics(callback: (topics)=>{recommended_topics = topics;});
    }

    private void getSubcribedAndAllTopics(){
        StartCoroutine( getSubscribedTopics() );
        StartCoroutine( getAllTopics() );
    }

    private bool checkSubscribedAndAllTopics(){
        return all_topics == null || subscribed_topics == null;
    }

    private bool errorSubscribedAndAllTopics(){
        // TODO: check if either returned error during request
        return false;
    }

    private IEnumerator onClickMoreTopics(){
        Debug.Log("Show more topics");

        MoreTopicsCanvas.SetActive(true);
        RecommendedTopicsCanvas.SetActive(false);

        // wait for all topics and subscribed topics
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: getSubcribedAndAllTopics,
            loadingCondition: checkSubscribedAndAllTopics,
            errorCondition: errorSubscribedAndAllTopics,
            timeoutSeconds: 30.0f
        );

        // remove subscribed_topics from all_topics for display
        all_topics = all_topics
                    .Where(x => !subscribed_topics.Any(y => y.ID == x.ID))
                    .ToList();
        
        // creating a seperate list from all_topics for search
        display_topics = new List<Topic>(all_topics);

        renderTopics(display_topics);

        // highlight recommended topics that have been selected in the base canvas
        highlightAllTopics(selected_topics);
    }

    private IEnumerator onClickOK(){
        if ((selected_topics != null) && (selected_topics.Any())){
            UserManager.instance.SubscribeAll(selected_topics);

            // pop up confirmation 
            dialogObject = Dialog.Create(name: "InfoDialog");
            Dialog.Load(
                title: "",
                body: "Successfully subscribed to " + selected_topics.Count.ToString() + " topic(s)!"
            );

            // wait for 3s
            yield return new WaitForSeconds(3);

            // destroy the rendered dialog box
            Destroy(dialogObject);

            selected_topics.Clear();
        }

        UIManager.instance.Goto_MainScene();
        yield return null;
    }

    // Currently just a copy of onClickSearchTopic, Later maybe different implementations
    // (Maybe click search go to backend to refetch, on refresh just search in already cached results)
    private void onRefreshSearch(string topic) {
        Debug.LogFormat("SubscriptionUIManager: Search for topic: {0}", topic);

        // clear previous results.
        display_topics.Clear();

        Regex r = new Regex(topic, RegexOptions.IgnoreCase);

        foreach (Topic t in all_topics){
            if (r.IsMatch(t.Name)){
                display_topics.Add(t);
            }
        }

        if (display_topics.Count != 0){
            Debug.Log("Found topics");
        }else{
            Debug.Log("Topics not found");
        }

        renderTopics(display_topics);
    }

    private void onClickSearchTopic(string topic){
        Debug.LogFormat("SubscriptionUIManager: Search for topic: {0}", topic);

        // clear previous results.
        display_topics.Clear();

        Regex r = new Regex(topic, RegexOptions.IgnoreCase);

        foreach (Topic t in all_topics){
            if (r.IsMatch(t.Name)){
                display_topics.Add(t);
            }
        }

        if (display_topics.Count != 0){
            Debug.Log("Found topics");
        }else{
            Debug.Log("Topics not found");
        }

        renderTopics(display_topics);
    }

    private bool errorRecommendedTopics(){
        //TODO: check if error during request
        return false;
    }

    private IEnumerator renderRecommended()
    {
        // recommended topics should have excluded subscribed topics already
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{ StartCoroutine( getRecommendedTopics() ); },
            loadingCondition: ()=>{ return recommended_topics == null; },
            errorCondition: errorRecommendedTopics,
            timeoutSeconds: 30.0f
        );

        current_pos = LAYOUT_START_POS;
        recommended_topics.ForEach(topic => createTopicButton(topic, RecommendedList_Panel));
    }

    private void renderTopics(List<Topic> render_list){
        clearAll(TopicsList_Panel);
        current_pos = LAYOUT_START_POS;
        render_list.ForEach(topic => createTopicButton(topic, TopicsList_Panel));
    }

    private void createTopicButton(Topic topic, GameObject panel)
    {
        //GameObject topicButtonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        GameObject topicButtonObject = (GameObject) Instantiate( ButtonPrefab );
        topicButtonObject.name = topic.ID;

        // set button text
        TMP_Text buttonText = topicButtonObject.GetComponentInChildren<TMP_Text>();
        buttonText.text = topic.Name;
        buttonText.fontSize = PlayerPrefs.GetInt(UIManager.TEXT_SIZE);

        RectTransform buttonTextTransform = buttonText.GetComponent<RectTransform>();
        float preferredWidth = LayoutUtility.GetPreferredWidth(buttonTextTransform);

        // compute button size
        float buttonWidth = preferredWidth + (4*LAYOUT_PADDING);
        if (buttonWidth < MIN_BUTTON_WIDTH) {
            buttonWidth = MIN_BUTTON_WIDTH;
        }

        Vector2 buttonSize = new Vector2((float)buttonWidth, BUTTON_HEIGHT);

        // compute button position
        Vector3 buttonPosition = LAYOUT_START_POS;
        if (current_pos == LAYOUT_START_POS) {
            // first button to place
            current_pos.x += (buttonSize.x + LAYOUT_PADDING);
        } else {
            if (current_pos.x + buttonSize.x >= HORRIZONTAL_END) {
                // start a new line
                current_pos.x = LAYOUT_START_POS.x;
                current_pos.y -= (BUTTON_HEIGHT + LAYOUT_PADDING);
            }
            buttonPosition = current_pos;
            current_pos.x += (buttonSize.x + LAYOUT_PADDING);
        }

        // set button size and position
        RectTransform buttonRectTransform = topicButtonObject.GetComponent<RectTransform>();
        buttonRectTransform.SetParent(panel.GetComponent<RectTransform>());
        buttonRectTransform.sizeDelta = buttonSize;
        buttonRectTransform.anchoredPosition = buttonPosition;
        buttonRectTransform.localScale = Vector2.one;
        // Debug.LogFormat("button position ({0}, {1}, {2})", buttonPosition.x, buttonPosition.y, buttonPosition.z);

        buttonTextTransform.sizeDelta = buttonSize;

        // set button action
        Button topicButton = topicButtonObject.GetComponent<Button>();
        topicButton.onClick.AddListener(() => {onClickTopic(topic.ID, topic.Name);});
    }

    private void onClickTopic(string topic_id, string topic_name)
    {
        Debug.Log(topic_id + " clicked");
        Image topic_buttonImage = GameObject.Find(topic_id).GetComponent<Image>();

        // toggle button color
        if (topic_buttonImage.color.ToString() == BUTTON_HIGHLIGHT_COLOR.ToString()){
            // remove highlight and deselect topic
            dehighlightTopic(topic_id);
            selected_topics.Remove(topic_id);

        }else if (topic_buttonImage.color.ToString() == BUTTON_COLOR.ToString()){
            // highlight and select topic
            highlightTopic(topic_id);
            selected_topics.Add(topic_id);
        }

    }

    private void dehighlightTopic(string topic_id){
        Image topic_buttonImage = GameObject.Find(topic_id).GetComponent<Image>();
        TMP_Text topic_buttonText = GameObject.Find(topic_id).GetComponentInChildren<TMP_Text>();
        topic_buttonImage.color = BUTTON_COLOR;
        topic_buttonText.color = Color.black;
    }
    
    private void highlightTopic(string topic_id){
        Image topic_buttonImage = GameObject.Find(topic_id).GetComponent<Image>();
        TMP_Text topic_buttonText = GameObject.Find(topic_id).GetComponentInChildren<TMP_Text>();
        topic_buttonImage.color = BUTTON_HIGHLIGHT_COLOR;
        topic_buttonText.color = Color.white;
    }

    private void highlightAllTopics(List<string> topic_id_list){
        topic_id_list.ForEach(topic_id => highlightTopic(topic_id));
    }

    private void clearAll(GameObject panel) {
        // clear all buttons instantiated
        foreach (Transform button in panel.GetComponent<RectTransform>()) {
            GameObject.Destroy(button.gameObject);
        }
    }


    /* ----------------------------------- Mock Data Section ---------------------------------- */
    // private System.Random rnd = new System.Random();  // random generator
    // private List<Topic> MockAllTopics() {
    //     List<Topic> all = new List<Topic>();
    //     for (int i = 0; i < 20; i++) {  // adjust here for number of randoms
    //         Topic topic = new Topic();
    //         topic.ID = "TOPIC_" + i;
    //         topic.MessageCount = rnd.Next(100);
    //         topic.Name = RandomString(rnd.Next(15));
    //         all.Add(topic);
    //     }
    //     return all;
    // }
    // private List<Topic> MockRecommendedTopics(List<Topic> all_topics) {
    //     List<Topic> recommend = new List<Topic>(4);
    //     for (int i = 0; i < 10; i++) {
    //         recommend.Add(all_topics[rnd.Next(all_topics.Count)]);
    //     }
    //     return recommend;
    // }
    // private string RandomString(int length)
    // {
    //     const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    //     return new string(Enumerable.Repeat(chars, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
    // }
}
