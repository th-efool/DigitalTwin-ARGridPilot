using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems; // make sure this using is at top of file





public class GridMaster : MonoBehaviour
{
    public Material materialA;
    public Material materialB;

    public GameObject gridObject;
    public GameObject robotObject;
    public float cellSize = 1f;
    public float padding = 0.2f;
    public int Length = 3;
    public int currentRobotTile = 0;
    
    private int _centerTileIndex;
    private UnityEngine.Vector3[] locations;
    private GameObject[] grid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnGrid();
        _centerTileIndex = Mathf.FloorToInt(Length / 2f);
    }

    

void Update()
{
    // 1) Detect a single "tap" or left-click (supports new Input System + legacy + touch)
    bool gotTap = false;
    Vector2 screenPos = Vector2.zero;

    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    // New Input System: check touchscreen first (mobile), fallback to mouse
    if (UnityEngine.InputSystem.Touchscreen.current != null)
    {
        var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
        if (touch.press.wasPressedThisFrame)
        {
            gotTap = true;
            screenPos = touch.position.ReadValue();
        }
    }
    else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
    {
        gotTap = true;
        screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
    }
    #else
    // Legacy input: mouse or touch
    if (Input.touchCount > 0)
    {
        var t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
        {
            gotTap = true;
            screenPos = t.position;
        }
    }
    else if (Input.GetMouseButtonDown(0))
    {
        gotTap = true;
        screenPos = Input.mousePosition;
    }
    #endif

    if (!gotTap) return;

    // 2) Ignore taps over UI
    if (EventSystem.current != null)
    {
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // For new Input System, use pointer id 0 check by raycasting the UI; simplest: use IsPointerOverGameObject() with no param
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        #else
        // Legacy: check touch or mouse pointer separately
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        if (Input.mousePresent && EventSystem.current.IsPointerOverGameObject()) return;
        #endif
    }

    // 3) Build world ray from the (AR) camera — Camera.main should be your AR camera
    var cam = Camera.main;
    if (cam == null)
    {
        Debug.LogError("GridMaster: No Main Camera (tag MainCamera missing).");
        return;
    }

    Ray ray = cam.ScreenPointToRay(screenPos);

    // 4) Physics raycast against your virtual tiles (colliders must exist)
    if (Physics.Raycast(ray, out RaycastHit hit, 200f))
    {
        // Only accept a GridTile attached directly to the collider GameObject (no parent traversal)
        var tileComp = hit.collider.gameObject.GetComponent<GridTile>();
        if (tileComp == null)
        {
            // optionally try GetComponentInParent if you really need it, but you said no — so ignore
            Debug.Log("Hit object has no GridTile on the collider itself. Ignoring.");
            return;
        }

        int index = tileComp.GetIndex();
        if (index < 0 || index >= Length * Length)
        {
            Debug.LogWarning($"GridMaster: tile index out of range: {index}");
            return;
        }

        if (IsInMovableRange(index, currentRobotTile))
        {
            TryMoveTo(index);
            Debug.Log("Moving to: " + index);
        }
        else
        {
            Debug.Log("Tile not movable: " + index);
        }
    }
    else
    {
        Debug.Log("Raycast hit NOTHING");
    }
}



    public bool TryMoveTo(int targetIndex)
    {
        if (!IsInMovableRange(targetIndex, currentRobotTile)) return false;

        MovePlayerTo(targetIndex);
        return true;
    }

    void HighlightCurrentMoveableRegions()
    {
        for (int i = 0; i < Length * Length; i++)
        {
            if (IsInMovableRange(i, currentRobotTile))
            {
                SetMaterial(grid[i], true);
            }
            else
            {
                SetMaterial(grid[i], false);

            }
        }
    }

    bool IsInMovableRange(int locationToMove, int currentlocation)
    {
        if (locationToMove == currentlocation){return false;}
        int total = Length * Length;
        if (locationToMove < 0 || locationToMove >= total) return false;
        if (currentlocation < 0 || currentlocation >= total) return false;

        int rowA = locationToMove / Length;
        int colA = locationToMove % Length;

        int rowB = currentlocation / Length;
        int colB = currentlocation % Length;

        int rowDiff = rowA - rowB;
        int colDiff = colA - colB;

        // adjacent including diagonals and optionally exclude same tile:
        bool isAdjacent = Mathf.Abs(rowDiff) <= 1 && Mathf.Abs(colDiff) <= 1;
        bool isOrthogonal = (Mathf.Abs(rowDiff) + Mathf.Abs(colDiff)) == 1;

        // If you want to exclude the tile the robot is on, uncomment:
        // if (locationToMove == currentlocation) return false;

        return isAdjacent && isOrthogonal;
    }

    
    
    public void SetMaterial(GameObject target, bool useA)
    {
        if (target == null) return;

        var renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;

        renderer.sharedMaterial = useA ? materialA : materialB;
    }

    
    void SpawnGrid()
    {
        if (gridObject == null)
        {
            Debug.LogWarning("GridMaster.SpawnGrid: gridObject prefab is not assigned.");
            return;
        }

        int tileCount = Length * Length;
        if (tileCount <= 0)
        {
            Debug.LogWarning("GridMaster.SpawnGrid: Length must be > 0.");
            return;
        }

        // Destroy any existing grid objects
        if (grid != null)
        {
            foreach (var existing in grid)
            {
                if (existing == null) continue;
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }
        }

        // Recalculate positions into the `locations` array (uses your existing method)
        ReCalculatePostion();

        // Instantiate new tiles and collect them in a temporary list
        var createdTiles = new System.Collections.Generic.List<GameObject>(tileCount);
        for (int i = 0; i < tileCount; i++)
        {
            var instance = Instantiate(gridObject, locations[i], Quaternion.identity, transform);
            instance.transform.localScale = new Vector3(cellSize, cellSize / 10.0f, cellSize);
            robotObject.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
            instance.name = $"GridTile_{i}";
            
            // --- NEW: attach GridTile component and initialize
            var tileComp = instance.AddComponent<GridTile>();
            tileComp.Init(i, this);
            
            createdTiles.Add(instance);
        }

        // Commit list to the grid array
        grid = createdTiles.ToArray();
        UpdatePositions();
    }

    
    public void UpdatePositions()
    {
        ReCalculatePostion();
        for (int i=0; i<locations.Length; i++)
        {
            grid[i].transform.position = locations[i];
            grid[i].transform.localScale = new Vector3(cellSize, cellSize/100.0f, cellSize);
        }
        robotObject.transform.position = locations[currentRobotTile];
        HighlightCurrentMoveableRegions();
    }
    
    void ReCalculatePostion()
    {
        int totalTiles = Length * Length;
        locations = new Vector3[totalTiles];

        // TRUE geometric centre offset so parent (0,0,0) is at grid center
        float centerOffset = (Length - 1) * 0.5f;

        for (int row = 0; row < Length; row++)
        {
            for (int col = 0; col < Length; col++)
            {
                float baseX = (row - centerOffset) * cellSize;
                float baseZ = (col - centerOffset) * cellSize;

                float padX = (row - centerOffset) * padding;
                float padZ = (col - centerOffset) * padding;

                locations[row * Length + col] = new Vector3(baseX + padX, 0f, baseZ + padZ);
            }
        }
    }


    public float moveDuration = 0.4f;

    Coroutine moveRoutine;

    [ContextMenu("MovePlayer")]
    public void MovePlayer()
    {
        int nextTile = currentRobotTile + 1;
        MovePlayerTo(nextTile);
    }

    public void MovePlayerTo(int targetTile)
    {
        if (targetTile < 0 || targetTile >= locations.Length) return;

        currentRobotTile = targetTile;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(SmoothMove(locations[targetTile]));
    }

    IEnumerator SmoothMove(Vector3 target)
    {
        if (robotObject == null) yield break;

        Vector3 start = robotObject.transform.position;
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / moveDuration);
            robotObject.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        HighlightCurrentMoveableRegions();
        robotObject.transform.position = target;
    }

    
}
