using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float maxPickupDistance = 2f;
    public float pickupPlaneDistance = 1f;
    
    [Header("References")]
    public Camera playerCamera;
    public Bag bag;
    
    private Item draggedItem;
    private Vector3 dragPlaneNormal;
    private Vector3 dragPlanePoint;
    private Vector3 dragOffset;
    private bool isDraggingFromBag;
    private Vector3Int? bagGridPosition; // Grid position in bag when hovering
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPickupItem();
        }
        
        if (Input.GetMouseButton(0) && draggedItem != null)
        {
            DragItem();
        }
        
        if (Input.GetMouseButtonUp(0) && draggedItem != null)
        {
            ReleaseItem();
        }
    }
    
    void TryPickupItem()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxPickupDistance))
        {
            Item item = hit.collider.GetComponentInParent<Item>();
            if (item != null)
            {
                // Check if item is on a conveyor belt
                ConveyorBeltGrid conveyorBelt = item.GetComponentInParent<ConveyorBeltGrid>();
                if (conveyorBelt != null)
                {
                    // Check distance from camera
                    float distance = Vector3.Distance(playerCamera.transform.position, item.transform.position);
                    if (distance <= maxPickupDistance)
                    {
                        PickupItemFromConveyor(item, conveyorBelt, hit.point);
                    }
                }
                // Check if item is in the bag
                else if (bag != null && item.transform.parent == bag.transform)
                {
                    TryPickupItemFromBag(item, hit.point);
                }
            }
        }
    }
    
    void PickupItemFromConveyor(Item item, ConveyorBeltGrid conveyorBelt, Vector3 hitPoint)
    {
        // Make item static if it wasn't already
        if (!item.IsStatic)
        {
            item.MakeStatic();
        }
        PickUpItem(item, hitPoint);
                
        Debug.Log($"Picked up item {item.name} from conveyor belt");
    }
    
    void TryPickupItemFromBag(Item item, Vector3 hitPoint)
    {
        if (bag == null)
            return;
        
        // Get the grid position of the item in the bag
        Vector3 localPos = bag.transform.InverseTransformPoint(item.transform.position);
        int gridX = Mathf.RoundToInt(localPos.x / Constants.CellSize + bag.gridSize.x * 0.5f);
        int gridZ = Mathf.RoundToInt(localPos.z / Constants.CellSize + bag.gridSize.z * 0.5f);
        
        // Try to remove the item from the bag
        Item removedItem = bag.TryRemoveItem(gridX, gridZ);
        
        if (removedItem == item)
        {
            PickUpItem(item, hitPoint);
            
            Debug.Log($"Picked up item {item.name} from bag at grid position ({gridX}, {gridZ})");
        }
    }

    void PickUpItem(Item item, Vector3 hitPoint)
    {
        draggedItem = item;
        isDraggingFromBag = true;
            
        // Setup drag plane parallel to screen at pickupPlaneDistance
        dragPlaneNormal = playerCamera.transform.forward;
        dragPlanePoint = playerCamera.transform.position + dragPlaneNormal * pickupPlaneDistance;
            
        // Calculate offset from hit point to item center
        dragOffset = item.transform.position - hitPoint;
            
        // Detach from bag parent
        item.transform.SetParent(null);
        item.transform.rotation = Quaternion.identity;
    }
    
    void DragItem()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        
        // Find intersection with drag plane
        float enter;
        Plane plane = new Plane(dragPlaneNormal, dragPlanePoint);
        
        if (plane.Raycast(ray, out enter))
        {
            Vector3 targetPosition = ray.GetPoint(enter) + dragOffset;
            
            // Check if hovering over bag
            if (bag != null && IsPositionOverBag(targetPosition))
            {
                Debug.Log($"Hovering over bag at grid position {bagGridPosition}");
                UpdateBagHoverPosition(targetPosition);
            }
            else
            {
                // Just follow cursor on the drag plane
                draggedItem.transform.position = targetPosition;
                bagGridPosition = null;
            }
        }
    }
    
    bool IsPositionOverBag(Vector3 worldPosition)
    {
        // Cast ray downward from item position to see if it hits the bag
        Vector3 localPos = bag.transform.InverseTransformPoint(worldPosition);
        
        // Check if position is within bag bounds on XZ plane
        float minX = -bag.gridSize.x * 0.5f * Constants.CellSize;
        float maxX = bag.gridSize.x * 0.5f * Constants.CellSize;
        float minZ = -bag.gridSize.z * 0.5f * Constants.CellSize;
        float maxZ = bag.gridSize.z * 0.5f * Constants.CellSize;
        Debug.Log($"Checking position {localPos} for bag bounds ({minX}, {maxX}, {minZ}, {maxZ})");
        
        return localPos.x >= minX && localPos.x <= maxX && 
               localPos.z >= minZ && localPos.z <= maxZ;
    }
    
    void UpdateBagHoverPosition(Vector3 worldPosition)
    {
        // Convert world position to bag's grid coordinates (XZ)
        Vector3 localPos = bag.transform.InverseTransformPoint(worldPosition);
        int gridX = Mathf.RoundToInt(localPos.x / Constants.CellSize + bag.gridSize.x * 0.5f);
        int gridZ = Mathf.RoundToInt(localPos.z / Constants.CellSize + bag.gridSize.z * 0.5f);
        
        // Try to get a valid preview position
        Vector3 previewPos;
        if (bag.TryGetPreviewPosition(draggedItem, gridX, gridZ, out previewPos))
        {
            // Valid placement - show preview and store grid position
            draggedItem.transform.position = previewPos;
            bagGridPosition = new Vector3Int(gridX, 0, gridZ);
        }
        else
        {
            // Invalid placement - keep item at drag position and clear grid position
            draggedItem.transform.position = worldPosition;
            bagGridPosition = null;
        }
    }
    
    void ReleaseItem()
    {
        // Check if we're hovering over the bag with a valid position
        if (bagGridPosition.HasValue && bag != null)
        {
            // Try to place item in bag
            if (bag.TryAddItem(draggedItem, bagGridPosition.Value.x, bagGridPosition.Value.z))
            {
                Debug.Log($"Placed item {draggedItem.name} in bag at grid position {bagGridPosition.Value}");
            }
            else
            {
                // Failed to place in bag, drop item at current position
                Debug.Log($"Failed to place item {draggedItem.name} in bag");
            }
        }
        else
        {
            // Released outside bag - item just stays where it is
            Debug.Log($"Released item {draggedItem.name} outside bag");
        }
        
        // Clean up
        draggedItem = null;
        bagGridPosition = null;
        isDraggingFromBag = false;
    }
}
