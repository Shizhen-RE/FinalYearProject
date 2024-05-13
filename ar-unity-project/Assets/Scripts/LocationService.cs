using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

class LocationService : MonoBehaviour
{

    public static LocationService instance;

    
    private bool is_enabled = false;
    private TMP_Text testTextbox;

    void Awake(){
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);

        }else if (instance != null){
            Debug.Log("LocationService instance already exists, destroying object");
            Destroy(this);
        }

        TMP_Text testTextbox = GameObject.Find("LocationText").GetComponent<TMP_Text>();

        // if don't have permission for location data, request for permission
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation)) {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.CoarseLocation);
        }

        // check if user has location service enabled
        is_enabled = UnityEngine.Input.location.isEnabledByUser;
        if (!is_enabled) {
            Debug.LogFormat("Android Location not enabled");
            if (testTextbox != null){
                testTextbox.text = "Location not enabled";
            }
        }else{
            // Start service
            UnityEngine.Input.location.Start(500f, 500f);
        }
    }

    void Start(){
        
    }

    void Update(){
        Vector3 currentLocation = GetCurrentLocation();
        //testTextbox.text = "lat=" + currentLocation.x.ToString() + ", lon=" + currentLocation.y.ToString();
    }

    
    public Vector3 GetCurrentLocation(){
        // TODO: get current device location with Google's geospatial 

        if (UnityEngine.Input.location.status != LocationServiceStatus.Running) {
            Debug.LogFormat("Unable to determine device location. Failed with status {0}", UnityEngine.Input.location.status);
            if (testTextbox != null){
                testTextbox.text = "Unable to determine device location.";
            }
            return new Vector3(0, 0, 0);
        }
        float latitude = UnityEngine.Input.location.lastData.latitude;
        float longitude = UnityEngine.Input.location.lastData.longitude;
        float altitude = UnityEngine.Input.location.lastData.altitude;
        return new Vector3(latitude, longitude, altitude);
    }

    public void Stop(){
        UnityEngine.Input.location.Stop();
    }

}