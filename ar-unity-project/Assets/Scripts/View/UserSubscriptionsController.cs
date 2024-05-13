using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserSubscriptionsController : MonoBehaviour
{
    // prefab
    public GameObject ListItemPrefab;

    // UI components - base canvas
    public GameObject BaseCanvas;
    public Button BackButton;
    public Button FilterButton;
    public Button UnsubscribeButton;
    public Button DoneButton;
    public GameObject Content;

    // UI components - filter canvas
    public GameObject FilterCanvas;
    public Button FilterBackButton;
    public Button FilterDoneButton;
    public TMP_Dropdown TopicDropdown1;
    public TMP_Dropdown TopicDropdown2;
    public TMP_Dropdown TopicDropdown3;
    public TMP_Dropdown FilterDropdown;

    // variables
    private List<Topic> subscriptionsList = null; // tuple of (topic_name, message_count)
    private GameObject[] deleteButtons;
    private List<string> deletedTopics = new List<string>(); // list of topic IDs to be unsubscribed
    private int filter = 0;
    private String preferred1 = "";
    private String preferred2 = "";
    private String preferred3 = "";

    public const string FILTER = "Filter"; // key in PlayerPrefs
    public const string PREFERED1 = "Prefered1"; // key in PlayerPrefs
    public const string PREFERED2 = "Prefered2"; // key in PlayerPrefs
    public const string PREFERED3 = "Prefered3"; // key in PlayerPrefs


    void Awake()
    {
        BaseCanvas = GameObject.Find("Canvas");
        FilterCanvas = GameObject.Find("FilterCanvas");

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        FilterBackButton = GameObject.Find("FilterBackButton").GetComponent<Button>();
        FilterBackButton.onClick.AddListener(onClickFilterDone);

        FilterButton = GameObject.Find("FilterButton").GetComponent<Button>();
        FilterButton.onClick.AddListener(onClickFilter);

        UnsubscribeButton = GameObject.Find("UnsubscribeButton").GetComponent<Button>();
        UnsubscribeButton.onClick.AddListener(onClickUnsubscribe);

        DoneButton = GameObject.Find("DoneButton").GetComponent<Button>();
        DoneButton.onClick.AddListener(onClickDone);

        FilterDoneButton = GameObject.Find("FilterDoneButton").GetComponent<Button>();
        FilterDoneButton.onClick.AddListener(onClickFilterDone);

        TopicDropdown1 = GameObject.Find("TopicDropdown1").GetComponent<TMP_Dropdown>();
        TopicDropdown1.onValueChanged.AddListener(delegate {
            if (TopicDropdown1.value > 0) {
                preferred1 = TopicDropdown1.options[TopicDropdown1.value].text;
            } else {
                preferred1 = "";
            }
        });

        TopicDropdown2 = GameObject.Find("TopicDropdown2").GetComponent<TMP_Dropdown>();
        TopicDropdown2.onValueChanged.AddListener(delegate {
            if (TopicDropdown2.value > 0) {
                preferred2 = TopicDropdown2.options[TopicDropdown2.value].text;
            } else {
                preferred2 = "";
            }
        });

        TopicDropdown3 = GameObject.Find("TopicDropdown3").GetComponent<TMP_Dropdown>();
        TopicDropdown3.onValueChanged.AddListener(delegate {
            if (TopicDropdown3.value > 0) {
                preferred3 = TopicDropdown3.options[TopicDropdown3.value].text;
            } else {
                preferred3 = "";
            }
        });

        FilterDropdown = GameObject.Find("FilterDropdown").GetComponent<TMP_Dropdown>();
        FilterDropdown.onValueChanged.AddListener(delegate {
            filter = FilterDropdown.value;
        });
        
        Content = GameObject.Find("Content");
    }

    // Start is called before the first frame update
    void Start()
    {
        // render subscriptions list
        StartCoroutine(renderSubscriptionsList());

        DoneButton.gameObject.SetActive(false);
        FilterDoneButton.gameObject.SetActive(false);
        FilterCanvas.gameObject.SetActive(false);
    }

    private void onClickUnsubscribe()
    {
        // enter unsubscribe
        DoneButton.gameObject.SetActive(true);
        UnsubscribeButton.gameObject.SetActive(false);
        FilterButton.gameObject.SetActive(false);

        // enable delete topics from list
        foreach (GameObject button in deleteButtons) {
            button.SetActive(true);
        }
    }

    private void onClickDelete(string topic_id)
    {
        deletedTopics.Add(topic_id);
        GameObject.Find(topic_id).SetActive(false);
    }

    private void onClickDone()
    {
        // update backend with the unsubscribed topics
        UserManager.instance.UnsubscribeAll(deletedTopics);
        deletedTopics.Clear();

        // TODO: call ARContentManager to refresh display

        // exit unsubscribe
        DoneButton.gameObject.SetActive(false);
        UnsubscribeButton.gameObject.SetActive(true);
        FilterButton.gameObject.SetActive(true);

        // disable delete topics from list
        foreach (GameObject button in deleteButtons) {
            button.SetActive(false);
        }
    }

    private void onClickFilter()
    {

        // Read player preferences
        if (PlayerPrefs.HasKey(FILTER)) {
            filter = PlayerPrefs.GetInt(FILTER);
        }
        if (PlayerPrefs.HasKey(PREFERED1)) {
            preferred1 = PlayerPrefs.GetString(PREFERED1);
        }
        if (PlayerPrefs.HasKey(PREFERED2)) {
            preferred2 = PlayerPrefs.GetString(PREFERED2);
        }
        if (PlayerPrefs.HasKey(PREFERED3)) {
            preferred3 = PlayerPrefs.GetString(PREFERED3);
        }

        // Populate dropdown menus
        List<string> topicList = new List<string>();
        topicList.Add("<Select Topic>");
        subscriptionsList.ForEach(topic => {
            topicList.Add(topic.Name);
        });
        TopicDropdown1.AddOptions(topicList);
        TopicDropdown2.AddOptions(topicList);
        TopicDropdown3.AddOptions(topicList);

        // Select options from dropdown
        FilterDropdown.value = filter;
        if (topicList.Contains(preferred1)) {
            TopicDropdown1.value = topicList.IndexOf(preferred1);
        } else {
            TopicDropdown1.value = 0;
            preferred1 = "";
        }
        if (topicList.Contains(preferred2)) {
            TopicDropdown2.value = topicList.IndexOf(preferred2);
        } else {
            TopicDropdown2.value = 0;
            preferred2 = "";
        }
        if (topicList.Contains(preferred3)) {
            TopicDropdown3.value = topicList.IndexOf(preferred3);
        } else {
            TopicDropdown3.value = 0;
            preferred3 = "";
        }
        
        // enter filtering
        FilterDoneButton.gameObject.SetActive(true);
        UnsubscribeButton.gameObject.SetActive(false);
        FilterButton.gameObject.SetActive(false);
        FilterCanvas.gameObject.SetActive(true);
        BaseCanvas.gameObject.SetActive(false);

    }

    private void onClickFilterDone()
    {
        
        // Update player preferences
        PlayerPrefs.SetInt(FILTER, filter);
        PlayerPrefs.SetString(PREFERED1, preferred1);
        PlayerPrefs.SetString(PREFERED2, preferred2);
        PlayerPrefs.SetString(PREFERED3, preferred3);
        PlayerPrefs.Save();

        // exit filtering
        FilterDoneButton.gameObject.SetActive(false);
        UnsubscribeButton.gameObject.SetActive(true);
        FilterButton.gameObject.SetActive(true);
        FilterCanvas.gameObject.SetActive(false);
        BaseCanvas.gameObject.SetActive(true);

    }

    private IEnumerator getSubscriptionsList(){
        yield return TopicManager.instance.GetSubscribedTopics((topics)=>{subscriptionsList = topics;});
    }

    private IEnumerator renderSubscriptionsList()
    {
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{ StartCoroutine(getSubscriptionsList()); },
            loadingCondition: ()=>{ return subscriptionsList == null; },
            errorCondition: ()=>{ return false; /* TODO */},
            timeoutSeconds: 30.0f
        );

        subscriptionsList.ForEach(topic => createListItem(topic));

        // get all delete topic buttons
        deleteButtons = GameObject.FindGameObjectsWithTag("DeleteTopicButton");

        // update text display size according to user preference
        UIManager.UpdateTextSize();

        foreach (GameObject button in deleteButtons) {
            button.SetActive(false);
        }
    }

    private void createListItem(Topic topic)
    {
        // TODO: validate input, truncate message_count if number too large
        GameObject listItem = (GameObject) Instantiate(ListItemPrefab);
        listItem.name = topic.ID;

        // set parent
        listItem.GetComponent<RectTransform>().SetParent(Content.GetComponent<RectTransform>());
        listItem.transform.localScale = Vector2.one; // reset prefab scale after set parent to scroll rect

        // update content
        GameObject topic_label = GameObject.Find("Topic");
        topic_label.name = topic.ID;
        topic_label.GetComponent<TMP_Text>().text = topic.Name;
        GameObject messageCount = GameObject.Find("MessageCount");
        messageCount.name = topic.ID + "_messageCount";
        messageCount.GetComponent<TMP_Text>().text = topic.MessageCount.ToString();

        Button deleteButton = GameObject.Find("DeleteButton").GetComponent<Button>();
        deleteButton.gameObject.name = topic.ID + "_deleteButton";
        deleteButton.onClick.AddListener(() => {
            onClickDelete(topic.ID);
        });
    }

}
