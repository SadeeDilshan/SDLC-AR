using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class PlaceAndInteractAR : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject sdlcStagePrefab;
    public StepThroughEngine stepEngine;
    public Slider scaleSlider;

    private GameObject spawnedStage;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        if (scaleSlider != null)
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
    }

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        // 1. Check if user tapped an existing 3D SDLC Phase Block
        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            SDLCElement element = hitInfo.collider.GetComponent<SDLCElement>();
            if (element != null && stepEngine != null)
            {
                stepEngine.SelectStepDirectly(element.stepIndex);
                return;
            }
        }

        // 2. Otherwise, Raycast against AR Planes to place or move the stage
        if (raycastManager != null && raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            if (spawnedStage == null)
            {
                spawnedStage = Instantiate(sdlcStagePrefab, hitPose.position, hitPose.rotation);
            }
            else
            {
                spawnedStage.transform.position = hitPose.position;
            }
        }
    }

    public void OnScaleChanged(float value)
    {
        if (spawnedStage != null)
            spawnedStage.transform.localScale = Vector3.one * value;
    }
}