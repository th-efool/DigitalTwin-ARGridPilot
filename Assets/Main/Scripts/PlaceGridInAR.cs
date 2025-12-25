using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceGrid_AR : MonoBehaviour
{
    public GameObject gridPrefab;
    public bool useExistingGrid = false;

    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;

    GameObject placedGrid;
    static readonly List<ARRaycastHit> hits = new();

    bool placementLocked = false;    // <<< NEW

    public void SetRotationY(float angleY)
    {
        if (placedGrid == null) return;
        placedGrid.transform.rotation = Quaternion.Euler(0f, angleY, 0f);
    }

    public void SetScale(float scaleValue)
    {
        if (placedGrid == null) return;
        placedGrid.transform.localScale = Vector3.one * scaleValue;
    }

    public GridMaster GetGridMaster()
    {
        if (placedGrid == null) return null;
        return placedGrid.GetComponentInChildren<GridMaster>();
    }
    void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;
    }

    void Update()
    {
        // STOP placement if calibration is confirmed
        if (placementLocked) return;

        if (!TryGetTap(out Vector2 pos)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (raycastManager == null) return;

        if (!raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon)) return;

        var hit = hits[0];
        PlaceAt(hit.pose.position, hit.pose.rotation);
    }

    void PlaceAt(Vector3 pos, Quaternion rot)
    {
        if (placedGrid == null)
        {
            if (useExistingGrid)
            {
                placedGrid = gridPrefab;
                placedGrid.transform.SetPositionAndRotation(pos, rot);
            }
            else
            {
                placedGrid = Instantiate(gridPrefab, pos, rot);
                SetScale(0.6f);

            }
            return;
        }

        // free-move mode until user confirms
        placedGrid.transform.SetPositionAndRotation(pos, rot);
    }

    // --------------------------
    // CALIBRATION DONE BUTTON
    // --------------------------
    public void ConfirmPlacement()
    {
        placementLocked = true;   // <<< lock movement

        // Disable plane manager & hide planes
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var p in planeManager.trackables)
                p.gameObject.SetActive(false);
        }
    }

    bool TryGetTap(out Vector2 pos)
    {
        pos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            pos = ts.primaryTouch.position.ReadValue();
            return true;
        }
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pos = mouse.position.ReadValue();
            return true;
        }
        return false;
#else
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pos = Input.GetTouch(0).position;
            return true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            return true;
        }
        return false;
#endif
    }
}
