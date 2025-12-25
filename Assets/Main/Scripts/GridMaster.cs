using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridMaster : MonoBehaviour
{
    public Material materialA;
    public Material materialB;

    public GameObject gridObject;
    public GameObject robotObject;
    public float cellSize = 1f;
    public float padding = 0.2f;
    public int Length = 2;
    public int currentRobotTile = 0;

    private int _centerTileIndex;
    private Vector3[] locations;
    private GameObject[] grid;

    public static GridMaster Instance { get; private set; }

    [Header("Animation")]
    [SerializeField] private Animator robotAnimator;          // drag child with Animator here
    [SerializeField] private string walkingBoolName = "isWalking";
    private int walkingBoolHash;


    void Awake()
    {
        Instance = this;

    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    void Start()
    {
        SpawnGrid();
        _centerTileIndex = Mathf.FloorToInt(Length / 2f);

        // --- animation init (minimal) ---
        if (robotAnimator == null && robotObject != null)
            robotAnimator = robotObject.GetComponentInChildren<Animator>();

        if (!string.IsNullOrEmpty(walkingBoolName))
            walkingBoolHash = Animator.StringToHash(walkingBoolName);

    }

    void Update()
    {
        bool gotTap = false;
        Vector2 screenPos = Vector2.zero;

        ReCalculatePostion();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                gotTap = true;
                screenPos = touch.position.ReadValue();
            }
        }
        else if (UnityEngine.InputSystem.Mouse.current != null &&
                 UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            gotTap = true;
            screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
#else
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

        if (EventSystem.current != null)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (EventSystem.current.IsPointerOverGameObject())
                return;
#else
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            if (Input.mousePresent && EventSystem.current.IsPointerOverGameObject()) return;
#endif
        }

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("GridMaster: No Main Camera.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var tileComp = hit.collider.gameObject.GetComponent<GridTile>();
            if (tileComp == null) return;

            int index = tileComp.GetIndex();
            if (index < 0 || index >= Length * Length) return;

            if (IsInMovableRange(index, currentRobotTile))
                TryMoveTo(index);
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
            SetMaterial(grid[i], IsInMovableRange(i, currentRobotTile));
        }
    }

    bool IsInMovableRange(int locationToMove, int currentlocation)
    {
        if (locationToMove == currentlocation) return false;

        int total = Length * Length;
        if (locationToMove < 0 || locationToMove >= total) return false;
        if (currentlocation < 0 || currentlocation >= total) return false;

        int rowA = locationToMove / Length;
        int colA = locationToMove % Length;

        int rowB = currentlocation / Length;
        int colB = currentlocation % Length;

        int rowDiff = rowA - rowB;
        int colDiff = colA - colB;

        bool isAdjacent = Mathf.Abs(rowDiff) <= 1 && Mathf.Abs(colDiff) <= 1;
        bool isOrthogonal = (Mathf.Abs(rowDiff) + Mathf.Abs(colDiff)) == 1;

        return isAdjacent;
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
            Debug.LogWarning("GridMaster: gridObject not assigned.");
            return;
        }

        int tileCount = Length * Length;
        if (tileCount <= 0)
        {
            Debug.LogWarning("GridMaster: Length must be > 0.");
            return;
        }

        if (grid != null)
        {
            foreach (var g in grid)
            {
                if (g == null) continue;
                if (Application.isPlaying) Destroy(g);
                else DestroyImmediate(g);
            }
        }

        ReCalculatePostion();

        var created = new System.Collections.Generic.List<GameObject>(tileCount);

        for (int i = 0; i < tileCount; i++)
        {
            var instance = Instantiate(gridObject, transform);
            instance.transform.localPosition = locations[i];
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = new Vector3(cellSize, cellSize / 10f, cellSize);
            robotObject.transform.localScale = new Vector3(cellSize, cellSize, cellSize);

            instance.name = $"GridTile_{i}";

            var tileComp = instance.AddComponent<GridTile>();
            tileComp.Init(i, this);

            created.Add(instance);
        }

        grid = created.ToArray();
        UpdatePositions();
    }

    public void UpdatePositions()
    {
        ReCalculatePostion();

        for (int i = 0; i < locations.Length; i++)
        {
            grid[i].transform.localPosition = locations[i];
            grid[i].transform.localScale = new Vector3(cellSize, cellSize / 100f, cellSize);
        }

        robotObject.transform.localPosition = locations[currentRobotTile];
        HighlightCurrentMoveableRegions();
    }

    void ReCalculatePostion()
    {
        int totalTiles = Length * Length;
        locations = new Vector3[totalTiles];

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

    public void MovePlayerTo(int targetTile)
    {
        if (targetTile < 0 || targetTile >= locations.Length) return;

        currentRobotTile = targetTile;

        if (moveRoutine != null) StopCoroutine(moveRoutine);

        SetWalking(true); // start walking anim
        moveRoutine = StartCoroutine(SmoothMove(locations[targetTile]));
    }


    [SerializeField] float spinsPerMove = 1f;   // number of full 360° turns per move

    IEnumerator SmoothMove(Vector3 targetLocalPos)
    {
        if (robotObject == null)
        {
            SetWalking(false);
            yield break;
        }

        Vector3 startPos = robotObject.transform.localPosition;
        Quaternion startRot = robotObject.transform.localRotation;

        // face direction of movement
        Vector3 moveDir = targetLocalPos - startPos;
        moveDir.y = 0f;
        Quaternion targetRot = moveDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(moveDir.normalized, Vector3.up)
            : startRot;

        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / moveDuration);

            // movement
            robotObject.transform.localPosition = Vector3.Lerp(startPos, targetLocalPos, t);
            // rotation towards movement direction
            robotObject.transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        robotObject.transform.localPosition = targetLocalPos;
        robotObject.transform.localRotation = targetRot;

        HighlightCurrentMoveableRegions();

        SetWalking(false); // stop walking anim
    }

    private void SetWalking(bool walking)
    {
        if (robotAnimator == null || walkingBoolHash == 0) return;
        robotAnimator.SetBool(walkingBoolHash, walking);
    }


}
