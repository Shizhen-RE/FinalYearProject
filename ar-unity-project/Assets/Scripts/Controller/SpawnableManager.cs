using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnableManager: MonoBehaviour
{
    public GameObject TargetPrefab;

    public GameObject BaseCanvas;
    public GameObject InputCanvas;
    public EventSystem SceneEventSystem;

    GraphicRaycaster baseCanvasRaycaster;
    GraphicRaycaster inputCanvasRaycaster;

    [SerializeField]
    ARRaycastManager raycastManager;

    bool lastFrame = false;

    public GameObject target = null;
    public bool Initialized { get { return initialized; } }
    bool initialized = false;

    IEnumerator Start()
    {
        baseCanvasRaycaster = BaseCanvas.GetComponent<GraphicRaycaster>();
        inputCanvasRaycaster = InputCanvas.GetComponent<GraphicRaycaster>();

        /* Spawn target */
        target = Instantiate(TargetPrefab, Vector3.zero, Quaternion.identity);
        target.SetActive(false);

        /* Wait until target is placed */
        while (!SetTargetPosition())
            yield return null;

        /* Show target */
        target.SetActive(true);

        initialized = true;
    }

    void Update()
    {
        SetTargetPosition();
    }

    public void InstantiateAtTarget(GameObject prefab)
    {
        Transform transform = this.target.transform;
        GameObject target = Instantiate(prefab, transform.position, transform.rotation);
        target.transform.Find("Canvas").localScale = transform.Find("Canvas").localScale;
        Destroy(this.target);
        this.target = target;
    }

    bool SetTargetPosition()
    {
        if (Input.touchCount == 0)
            return false;

        Vector2 touchPosition = Input.GetTouch(0).position;

        /* Ignore UI interactions */
        List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData touchEventData = new PointerEventData(SceneEventSystem);
        touchEventData.position = touchPosition;
        baseCanvasRaycaster.Raycast(touchEventData, uiHits);
        inputCanvasRaycaster.Raycast(touchEventData, uiHits);
        if (uiHits.Count > 0) {
            lastFrame = true;
            return false;
        } else if (lastFrame) {
            /* Button released this frame */
            lastFrame = false;
            return false;
        }

        /* Handle AR plane interactions */
        List<ARRaycastHit> arHits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(touchPosition, arHits, TrackableType.Planes)) {
            Pose arHitPose = arHits[0].pose;
            target.transform.position = arHitPose.position;
            target.transform.rotation = arHitPose.rotation;
            return true;
        }

        return false;
    }
}