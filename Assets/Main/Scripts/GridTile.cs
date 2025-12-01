using UnityEngine;

public class GridTile : MonoBehaviour
{
    int tileIndex;
    GridMaster owner;
    public int GetIndex(){return tileIndex;}

    // Called immediately after Instantiate to wire index + owner
    public void Init(int index, GridMaster ownerMaster)
    {
        tileIndex = index;
        owner = ownerMaster;
    }

    // Simple mouse click handler (works in both Scene and Game view when clicking)
    void OnMouseDown()
    {
        Debug.Log($"CLICK DETECTED on tile {tileIndex} / object: {gameObject.name}", gameObject);

        if (owner == null) return;
        owner.TryMoveTo(tileIndex);
    }
}
