using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserLocationsUIController : MonoBehaviour
{
    // prefab
    public GameObject ListItemPrefab;
    public GameObject DiaglogPrefab;

    // UI components - Base canvas
    public Button BackButton;
    public GameObject Content;
    public GameObject BaseCanvas;

    // UI components - Expanded view canvas
    private GameObject ExpandedViewCanvas;
    private Button DetailsBackButton;
    private Button DeleteButton;
    private Button EditButton;
    private Button DoneButton;
    private TMP_InputField DetailsLocationName;
    private TMP_Text DetailsLocationCoordinates;
    private TMP_Text DetailsStreetAddress;
    private Toggle AllowPostingToggle;


    // variables
    private List<Location> locationsList;
    private int currentExpandedID;

    private GameObject dialogBox;
    
    void Awake()
    {
        // Base canvas
        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        Content = GameObject.Find("Content");

        BaseCanvas = GameObject.Find("BaseCanvas");

        // Expanded view canvas
        ExpandedViewCanvas = GameObject.Find("ExpandedViewCanvas");

        DetailsBackButton = GameObject.Find("DetailsBackButton").GetComponent<Button>();
        DetailsBackButton.onClick.AddListener(() => {
            currentExpandedID = -1;
            BaseCanvas.SetActive(true);
            ExpandedViewCanvas.SetActive(false);
        });

        DeleteButton = GameObject.Find("DeleteButton").GetComponent<Button>();
        DeleteButton.onClick.AddListener(onClickDelete);

        EditButton = GameObject.Find("EditButton").GetComponent<Button>();
        EditButton.onClick.AddListener(onClickEdit);

        DoneButton = GameObject.Find("DoneButton").GetComponent<Button>();
        DoneButton.onClick.AddListener(onClickDone);

        DetailsLocationName = GameObject.Find("details_name").GetComponent<TMP_InputField>();
        DetailsLocationCoordinates = GameObject.Find("details_coordinates").GetComponent<TMP_Text>();
        DetailsStreetAddress = GameObject.Find("details_streetAddress").GetComponent<TMP_Text>();

        AllowPostingToggle = GameObject.Find("AllowPostingToggle").GetComponent<Toggle>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // get user's bound locations
        locationsList = UserManager.instance.GetUserLocations();

        // render locations list
        renderLocationsList();

        // update text display size according to user preference
        UIManager.UpdateTextSize();

        // hide details view
        DoneButton.gameObject.SetActive(false);
        ExpandedViewCanvas.SetActive(false);
    }

    private void renderLocationsList()
    {
        for (int i = 0; i < locationsList.Count; i++) {
            createListItem(locationsList[i], i);
        }
    }

    private void createListItem(Location item, int id)
    {
        GameObject listItem = (GameObject) Instantiate( ListItemPrefab );
        listItem.name = id.ToString();

        // set parent
        listItem.GetComponent<RectTransform>().SetParent(Content.GetComponent<RectTransform>());
        listItem.transform.localScale = Vector2.one; // reset prefab scale after set parent to scroll rect

        // update content
        TMP_Text LocationName = GameObject.Find("LocationName").GetComponent<TMP_Text>();
        LocationName.text = item.Name;
        LocationName.name += id.ToString();

        TMP_Text StreetAddress = GameObject.Find("StreetAddress").GetComponent<TMP_Text>();
        StreetAddress.text = item.StreetAddress;
        StreetAddress.name += id.ToString();

        Button expandButton = listItem.GetComponent<Button>();
        // expandButton.name += id.ToString();
        expandButton.onClick.AddListener(() => {onClickExpand(id);});
    }

    private void onClickExpand(int id)
    {
        Location location = locationsList[id];
        currentExpandedID = id;

        // go to expanded view
        BaseCanvas.SetActive(false);
        ExpandedViewCanvas.SetActive(true);

        // update content
        DetailsLocationName.text = location.Name;
        DetailsLocationCoordinates.text = "(" + location.Coordinates[0].ToString() + "," + location.Coordinates[1].ToString() + ")";
        DetailsStreetAddress.text = location.StreetAddress;
        AllowPostingToggle.isOn = location.Public;
    }

    private void onClickDelete()
    {
        // pop up confirmation dialog box
        dialogBox = Dialog.Create(name: "DeleteActionDialog", prefab: DiaglogPrefab);
        Dialog.Load(
            title: "Warning",
            body: "Are you sure you want to delete this location?",
            button1Text: "Yes",
            button2Text: "No",
            button2Action: onClickCancelDelete
        );
    
        GameObject.Find("DialogButton1").GetComponent<Button>().onClick.AddListener(() => {
            StartCoroutine(onClickConfirmDelete(currentExpandedID));
        });
    }

    private IEnumerator onClickConfirmDelete(int id)
    {
        // call backend API to delete location from user
        UserManager.instance.DeleteUserLocation(locationsList[id].ID);

        // update dialog box
        Dialog.Load(
            "",
            "Deleted!"
        );

        // wait for 1s
        yield return new WaitForSeconds(1);

        // destroy the rendered dialog box
        Destroy(dialogBox);

        // go back to base canvas
        ExpandedViewCanvas.SetActive(false);
        BaseCanvas.SetActive(true);

        // delete rendered gameobject for the list item
        Destroy(GameObject.Find(id.ToString()));
    }

    private void onClickCancelDelete(){
        Destroy(GameObject.Find("DeleteActionDialog"));
    }

    private void onClickEdit(){
        DoneButton.gameObject.SetActive(true);
        EditButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        // highlight components
        DetailsLocationName.interactable = true;
        DetailsLocationName.GetComponent<Image>().color = UIManager.THEME_COLOR;
        AllowPostingToggle.interactable = true;
    }

    private void onClickDone(){
        DoneButton.gameObject.SetActive(false);
        EditButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);
        DetailsLocationName.interactable = false;
        DetailsLocationName.GetComponent<Image>().color = Color.white;
        AllowPostingToggle.interactable = false;

        // update Location object
        Location location = locationsList[currentExpandedID];
        bool updated = false;
        if (location.Name != DetailsLocationName.text){
            updated = true;
            location.Name = DetailsLocationName.text;
        }
        if (location.Public != AllowPostingToggle.isOn){
            updated = true;
            location.Public = AllowPostingToggle.isOn;
        }

        // update user location to the backend
        if (updated){
            UserManager.instance.UpdateUserLocation(location);
        }
    }
}
