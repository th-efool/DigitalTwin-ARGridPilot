using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaceGrid_AROnly : MonoBehaviour
{
    [Tooltip("Grid prefab or scene object to place.")]
    public GameObject gridPrefab;
    [Tooltip("If true, will move the assigned gridPrefab (scene object) instead of instantiating.")]
    public bool useExistingGrid = false;
    [Tooltip("Allow reposition after initial placement.")]
    public bool allowReposition = true;
    [Tooltip("AR Camera (auto-filled if null)")]
    public Camera arCamera;

    GameObject placedGrid;

    // Reflection caches
    static Type s_ARRaycastManagerType;
    static Type s_ARRaycastHitType;
    static Type s_TrackableTypeEnum;
    static MethodInfo s_RaycastMethod;
    static PropertyInfo s_ARRaycastHit_pose;

    void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;
        EnsureARRaycastReflection();
    }

    void Reset()
    {
        if (arCamera == null && Camera.main != null) arCamera = Camera.main;
    }

    void EnsureARRaycastReflection()
    {
        if (s_RaycastMethod != null) return; // already cached

        // Try to get ARFoundation types via reflection
        // ARRraycastManager: "UnityEngine.XR.ARFoundation.ARRaycastManager, Unity.XR.ARFoundation"
        s_ARRaycastManagerType = Type.GetType("UnityEngine.XR.ARFoundation.ARRaycastManager, Unity.XR.ARFoundation");
        s_ARRaycastHitType     = Type.GetType("UnityEngine.XR.ARFoundation.ARRaycastHit, Unity.XR.ARFoundation");
        s_TrackableTypeEnum    = Type.GetType("UnityEngine.XR.ARSubsystems.TrackableType, Unity.XR.ARSubsystems");

        if (s_ARRaycastManagerType == null || s_ARRaycastHitType == null || s_TrackableTypeEnum == null)
        {
            // ARFoundation types not available — leave method null and we will not attempt AR raycasts.
            s_RaycastMethod = null;
            return;
        }

        // Raycast signature: bool Raycast(Vector2 screenPoint, List<ARRaycastHit> hitResults, TrackableType trackableTypes)
        // find method
        s_RaycastMethod = s_ARRaycastManagerType.GetMethod("Raycast", new Type[] {
            typeof(Vector2),
            typeof(List<>).MakeGenericType(s_ARRaycastHitType),
            s_TrackableTypeEnum
        });

        // property ARRaycastHit.pose
        s_ARRaycastHit_pose = s_ARRaycastHitType.GetProperty("pose", BindingFlags.Instance | BindingFlags.Public);
        // if any are null, treat as unavailable
        if (s_RaycastMethod == null || s_ARRaycastHit_pose == null) s_RaycastMethod = null;
    }

    void Update()
    {
        // only run if we have ARFoundation raycast available
        if (s_RaycastMethod == null)
        {
            // ensure we attempted find at least once
            EnsureARRaycastReflection();
            return;
        }

        if (!TryGetTapPosition(out Vector2 screenPos)) return;

        // ignore UI taps
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // find any ARRaycastManager instance in scene
        UnityEngine.Object[] managers = UnityEngine.Object.FindObjectsOfType(s_ARRaycastManagerType);
        if (managers == null || managers.Length == 0) return;
        var managerInstance = managers[0]; // use first found

        // create List<ARRaycastHit> dynamically
        Type listType = typeof(List<>).MakeGenericType(s_ARRaycastHitType);
        var hitList  = Activator.CreateInstance(listType);

        // TrackableType.PlaneWithinPolygon
        object trackablePlaneEnum = Enum.Parse(s_TrackableTypeEnum, "PlaneWithinPolygon");

        // Call Raycast
        object[] parameters = new object[] { screenPos, hitList, trackablePlaneEnum };
        bool raycastResult = (bool)s_RaycastMethod.Invoke(managerInstance, parameters);
        if (!raycastResult) return; // no AR hits -> DO NOT PLACE

        // check list.Count > 0
        int count = (int)listType.GetProperty("Count").GetValue(hitList, null);
        if (count <= 0) return;

        // get first hit: var first = hitList[0];
        object firstHit = listType.GetProperty("Item").GetValue(hitList, new object[] { 0 });

        // read pose: firstHit.pose (UnityEngine.Pose)
        object poseObj = s_ARRaycastHit_pose.GetValue(firstHit, null);
        Pose hitPose = (Pose)poseObj;

        // Use the screen ray direction and project it onto the hit plane to compute forward
        if (arCamera == null) arCamera = Camera.main;
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        Vector3 forwardOnPlane = ProjectOnPlane(ray.direction, hitPose.up);

        if (forwardOnPlane.sqrMagnitude < 1e-6f)
            forwardOnPlane = ProjectOnPlane(arCamera.transform.forward, hitPose.up);

        if (forwardOnPlane.sqrMagnitude < 1e-6f)
            forwardOnPlane = Vector3.forward;

        Quaternion rot = Quaternion.LookRotation(forwardOnPlane.normalized, hitPose.up);

        PlaceAtPose(hitPose.position, rot);
    }

    void PlaceAtPose(Vector3 position, Quaternion rotation)
    {
        if (placedGrid == null)
        {
            if (useExistingGrid)
            {
                if (gridPrefab == null)
                {
                    Debug.LogError("PlaceGrid_AROnly: useExistingGrid true but gridPrefab not assigned.");
                    return;
                }
                placedGrid = gridPrefab;
                placedGrid.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                if (gridPrefab == null)
                {
                    Debug.LogError("PlaceGrid_AROnly: gridPrefab not assigned.");
                    return;
                }
                placedGrid = Instantiate(gridPrefab, position, rotation);
            }

            // Parent to a simulated anchor for organization (no ARAnchor used)
            var anchorGO = new GameObject("SimulatedAnchor");
            anchorGO.transform.SetPositionAndRotation(position, rotation);
            placedGrid.transform.SetParent(anchorGO.transform, worldPositionStays: true);

            var gm = placedGrid.GetComponent<GridMaster>();
            if (gm != null) gm.UpdatePositions();

            Debug.Log("[PlaceGrid_AROnly] Placed grid at AR plane: " + position);
            return;
        }

        if (!allowReposition) return;

        // Move existing: replace simulated anchor
        var prev = placedGrid.transform.parent;
        if (prev != null && prev.gameObject.name == "SimulatedAnchor")
            Destroy(prev.gameObject);

        var newAnchor = new GameObject("SimulatedAnchor");
        newAnchor.transform.SetPositionAndRotation(position, rotation);
        placedGrid.transform.SetParent(newAnchor.transform, worldPositionStays: true);
        placedGrid.transform.SetPositionAndRotation(position, rotation);

        var gm2 = placedGrid.GetComponent<GridMaster>();
        if (gm2 != null) gm2.UpdatePositions();

        Debug.Log("[PlaceGrid_AROnly] Moved grid to AR plane: " + position);
    }

    // Try get tap position (supports new Input System and legacy)
    bool TryGetTapPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = touch.primaryTouch.position.ReadValue();
            return true;
        }
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }
        return false;
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                screenPos = t.position;
                return true;
            }
        }
        return false; // NO mouse fallback (explicit)
#endif
    }

    static Vector3 ProjectOnPlane(Vector3 vec, Vector3 planeNormal)
    {
        return vec - Vector3.Dot(vec, planeNormal) * planeNormal;
    }
}
