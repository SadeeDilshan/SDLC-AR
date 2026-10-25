using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        Vector2 inputPos = Vector2.zero;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            inputPos = Input.GetTouch(0).position;
        else if (Input.GetMouseButtonDown(0))
            inputPos = Input.mousePosition;
        else
            return;

        // Ignore clicks on UI buttons (Next, Back, Slider, etc.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 1. If the stage is ALREADY spawned, check if user clicked a 3D Block to inspect it
        if (spawnedStage != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputPos);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                SDLCElement element = hitInfo.collider.GetComponent<SDLCElement>();
                if (element != null && stepEngine != null)
                {
                    stepEngine.SelectStepDirectly(element.stepIndex);
                }
            }
            // Return here so dragging/clicking doesn't spawn a 2nd model or teleport it!
            return;
        }

        // 2. If NOT spawned yet, Raycast against AR surface to place it for the first time
        if (raycastManager != null && raycastManager.Raycast(inputPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            spawnedStage = Instantiate(sdlcStagePrefab, hitPose.position, hitPose.rotation);

            // Connect the newly spawned clone's Orb & Cubes to StepThroughEngine!
            if (stepEngine != null)
            {
                stepEngine.BindSpawnedStage(spawnedStage);
            }
        }
    }

    public void OnScaleChanged(float value)
    {
        if (spawnedStage != null)
            spawnedStage.transform.localScale = Vector3.one * value;
    }
}