using Unity.Cinemachine;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float maxPickupDistance = 2f;
    public float pickupPlaneDistance = 1f;
    
    [Header("References")]
    public Camera playerCamera;
    public Bag[] bags;
    public ConveyorBeltGrid[] conveyorBelts;

    public CinemachineCamera bagCamera;
    public CinemachineCamera cashierCamera;
    
    public PackButton packButton;

    public bool CanControl = true;
    private Item draggedItem;
    private Vector3 dragPlaneNormal;
    private Vector3 dragPlanePoint;
    private Vector3 dragOffset;
    private bool isDraggingFromBag;
    private Vector3Int? bagGridPosition; // Grid position in bag when hovering
    private Vector2Int? conveyorGridPosition; // Grid position on conveyor belt when hovering
    private Item hoveredItem; // Currently hovered item (not being dragged)
    private Bag activeBag;
    private ConveyorBeltGrid activeConveyorBelt;

    private float totalTime = 0;
    public float TotalTime => totalTime;
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }
    
    void Update()
    {
        if (!CanControl)
        {
            return;
        }
        // Handle hover detection when not dragging
        if (draggedItem == null)
        {
            UpdateHover();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (draggedItem == null)
            {
                TryPickupItem();
            }
            else
            {
                ReleaseItem();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            SwitchCamera();
        }

        if (draggedItem != null)
        {
            // Handle rotation input while dragging
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RotateDraggedItem();
            }

            DragItem();
        }
        
        totalTime += Time.deltaTime;
    }

    private void SwitchCamera()
    {
        if (bagCamera.Priority > cashierCamera.Priority)
        {
            bagCamera.Priority = 0;
            cashierCamera.Priority = 1;
        }
        else
        {
            bagCamera.Priority = 1;
            cashierCamera.Priority = 0;
        }
    }

    void UpdateHover()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Item newHoveredItem = null;

        if (Physics.Raycast(ray, out hit, maxPickupDistance))
        {
            Item item = hit.collider.GetComponentInParent<Item>();
            if (item != null)
            {
                // Check if item is on a conveyor belt or in bag
                ConveyorBeltGrid conveyorBelt = item.GetComponentInParent<ConveyorBeltGrid>();
                Bag bag = item.GetComponentInParent<Bag>();

                if (conveyorBelt != null || bag != null)
                {
                    float distance = Vector3.Distance(playerCamera.transform.position, item.transform.position);
                    if (distance <= maxPickupDistance)
                    {
                        newHoveredItem = item;
                    }
                }
            }
        }

        // Update hover state
        if (newHoveredItem != hoveredItem)
        {
            // Reset previous hovered item
            if (hoveredItem != null)
            {
                hoveredItem.ResetHover();
            }

            // Set new hovered item
            hoveredItem = newHoveredItem;
            if (hoveredItem != null)
            {
                hoveredItem.SetHover();
            }
        }
    }
    
    void TryPickupItem()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        var layerMask = LayerMask.GetMask("Item");
        
        if (Physics.Raycast(ray, out hit, maxPickupDistance, layerMask))
        {
            // Debug.Log("Raycast hit " + hit.collider.name);
            Item item = hit.collider.GetComponentInParent<Item>();
            if (item != null)
            {
                Debug.Log($"Item found {item.name}");
                // Check if item is on a conveyor belt
                ConveyorBeltGrid conveyorBelt = item.GetComponentInParent<ConveyorBeltGrid>();
                Bag bag = item.GetComponentInParent<Bag>();
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
                else if (bag != null)
                {
                    Debug.Log($"Try picking up item from bag {item.name}");
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

        // Free the grid cells occupied by this item
        conveyorBelt.RemoveItem(item);

        PickUpItem(item, hitPoint);

        // Debug.Log($"Picked up item {item.name} from conveyor belt");
    }
    
    void TryPickupItemFromBag(Item item, Vector3 hitPoint)
    {
        foreach (var bag in bags)
        {
            // Get the grid position of the item in the bag
            Vector3 localPos = bag.transform.InverseTransformPoint(hitPoint);
            int gridX = Mathf.FloorToInt(localPos.x / Constants.CellSize + bag.gridSize.x * 0.5f);
            int gridZ = Mathf.FloorToInt(localPos.z / Constants.CellSize + bag.gridSize.z * 0.5f);

            // Try to remove the item from the bag
            Item removedItem = bag.TryRemoveItem(gridX, gridZ);

            if (removedItem == item)
            {
                PickUpItem(item, hitPoint);

                Debug.Log($"Picked up item {item.name} from bag at grid position ({gridX}, {gridZ})");
                break;
            }
        }
    }

    void PickUpItem(Item item, Vector3 hitPoint)
    {
        draggedItem = item;
        isDraggingFromBag = true;

        // Reset hover state for the item being picked up
        if (hoveredItem == item)
        {
            hoveredItem = null;
        }
        item.ResetHover();

        // Setup drag plane parallel to screen at pickupPlaneDistance
        dragPlaneNormal = playerCamera.transform.forward;
        dragPlanePoint = playerCamera.transform.position + dragPlaneNormal * pickupPlaneDistance;

        // Calculate offset from hit point to item center
        dragOffset = -GetHalfItemGridSize(item);

        // Detach from bag parent
        item.transform.SetParent(null);
        item.transform.rotation = Quaternion.identity;
        
        draggedItem.OnDragStart();
    }

    private static Vector3 GetHalfItemGridSize(Item item)
    {
        return new Vector3(item.GetGridSize().x, 0, item.GetGridSize().y) * Constants.CellSize / 2f;
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
            if (IsPositionOverBag(targetPosition, out Bag bag))
            {
                // Debug.Log($"Hovering over bag {bag.name} at grid position {bagGridPosition}");
                UpdateBagHoverPosition(targetPosition, bag, draggedItem);
                conveyorGridPosition = null;
                activeConveyorBelt = null;
            }
            // Check if hovering over conveyor belt
            else if (IsPositionOverConveyorBelt(targetPosition, out ConveyorBeltGrid conveyorBelt))
            {
                UpdateConveyorHoverPosition(targetPosition, conveyorBelt, draggedItem);
                bagGridPosition = null;
                activeBag = null;
            }
            else
            {
                // Just follow cursor on the drag plane
                draggedItem.transform.position = targetPosition;
                bagGridPosition = null;
                activeBag = null;
                conveyorGridPosition = null;
                activeConveyorBelt = null;
                draggedItem.ShowGridPreview(false);
                draggedItem.ResetTint();
            }
        }
    }
    
    bool IsPositionOverBag(Vector3 worldPosition, out Bag res)
    {
        // Cast ray from camera through mouse cursor position to check if bag is under cursor
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // First, check if we're hovering over a Grid collider (placed items in bag)
        int gridLayerMask = LayerMask.GetMask("Grid");
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayerMask))
        {
            // Found a grid collider - find which bag this item belongs to
            Item hitItem = hit.collider.GetComponentInParent<Item>();
            if (hitItem != null)
            {
                Bag itemBag = hitItem.GetComponentInParent<Bag>();
                if (itemBag != null)
                {
                    res = itemBag;
                    return true;
                }
            }
        }

        // If no Grid collider hit, check for bag collider
        foreach (var bag in bags)
        {
            // Check if ray intersects with bag's collider
            Collider bagCollider = bag.GetComponentInChildren<Collider>();
            if (bagCollider != null)
            {
                if (bagCollider.Raycast(ray, out hit, Mathf.Infinity))
                {
                    res = bag;
                    return true;
                }
            }
        }

        res = null;
        return false;
    }
    
    void UpdateBagHoverPosition(Vector3 worldPosition, Bag bag, Item item)
    {
        // Raycast from camera through mouse cursor to find hit point
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool hitFound = false;

        // First, check if we're hovering over a Grid collider (placed items in bag)
        int gridLayerMask = LayerMask.GetMask("Grid");
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayerMask))
        {
            // Found a grid collider - find the item component in the parent
            Item hitItem = hit.collider.GetComponentInParent<Item>();
            if (hitItem != null)
            {
                hitFound = true;
            }
        }

        // If no Grid collider hit, check for bag collider
        if (!hitFound)
        {
            Collider bagCollider = bag.GetComponentInChildren<Collider>();
            if (bagCollider != null && bagCollider.Raycast(ray, out hit, Mathf.Infinity))
            {
                hitFound = true;
            }
        }

        if (hitFound)
        {
            // Convert hit point on bag to grid coordinates
            Vector3 localPos = bag.transform.InverseTransformPoint(hit.point - GetHalfItemGridSize(item));
            int gridX = Mathf.RoundToInt(localPos.x / Constants.CellSize + bag.gridSize.x * 0.5f);
            int gridZ = Mathf.RoundToInt(localPos.z / Constants.CellSize + bag.gridSize.z * 0.5f);

            // Try to get a valid preview position
            Vector3 previewPos;
            if (bag.TryGetPreviewPosition(draggedItem, gridX, gridZ, out previewPos))
            {
                // Valid placement - show preview and store grid position
                draggedItem.transform.position = previewPos;
                bagGridPosition = new Vector3Int(gridX, 0, gridZ);
                activeBag = bag;
                draggedItem.ShowGridPreview(true);
            }
            else
            {
                // Invalid placement - keep item at drag position and clear grid position
                draggedItem.transform.position = worldPosition;
                bagGridPosition = null;
                activeBag = null;
                draggedItem.ShowGridPreview(false);
                draggedItem.ResetTint();
            }
        }
        else
        {
            // Couldn't raycast to bag - keep item at drag position
            draggedItem.transform.position = worldPosition;
            bagGridPosition = null;
            activeBag = null;
            draggedItem.ShowGridPreview(false);
            draggedItem.ResetTint();
        }
    }

    bool IsPositionOverConveyorBelt(Vector3 worldPosition, out ConveyorBeltGrid res)
    {
        // Cast ray from camera through mouse cursor position to check if conveyor belt is under cursor
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        foreach (var conveyorBelt in conveyorBelts)
        {
            // Check if ray intersects with conveyor belt's collider
            Collider conveyorCollider = conveyorBelt.GetComponentInChildren<Collider>();
            if (conveyorCollider != null)
            {
                RaycastHit hit;
                if (conveyorCollider.Raycast(ray, out hit, Mathf.Infinity))
                {
                    res = conveyorBelt;
                    return true;
                }
            }
        }

        res = null;
        return false;
    }

    void UpdateConveyorHoverPosition(Vector3 worldPosition, ConveyorBeltGrid conveyorBelt, Item item)
    {
        // Raycast from camera through mouse cursor to find hit point on conveyor belt
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        Collider conveyorCollider = conveyorBelt.GetComponentInChildren<Collider>();
        RaycastHit hit;

        if (conveyorCollider != null && conveyorCollider.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Convert hit point on conveyor belt to grid coordinates
            Vector3 localPos = conveyorBelt.transform.InverseTransformPoint(hit.point - GetHalfItemGridSize(item));
            int gridX = Mathf.RoundToInt(localPos.x / Constants.CellSize + conveyorBelt.gridSize.x * 0.5f);
            int gridY = Mathf.RoundToInt(localPos.z / Constants.CellSize);

            // Try to get a valid preview position
            Vector3 previewPos;
            if (conveyorBelt.TryGetPreviewPosition(draggedItem, gridX, gridY, out previewPos))
            {
                // Valid placement - show preview and store grid position
                draggedItem.transform.position = previewPos;
                conveyorGridPosition = new Vector2Int(gridX, gridY);
                activeConveyorBelt = conveyorBelt;
                draggedItem.ShowGridPreview(true);
            }
            else
            {
                // Invalid placement - keep item at drag position and clear grid position
                draggedItem.transform.position = worldPosition;
                conveyorGridPosition = null;
                activeConveyorBelt = null;
                draggedItem.ShowGridPreview(false);
                draggedItem.ResetTint();
            }
        }
        else
        {
            // Couldn't raycast to conveyor belt - keep item at drag position
            draggedItem.transform.position = worldPosition;
            conveyorGridPosition = null;
            activeConveyorBelt = null;
            draggedItem.ShowGridPreview(false);
            draggedItem.ResetTint();
        }
    }

    void RotateDraggedItem()
    {
        if (draggedItem != null)
        {
            draggedItem.RotateClockwise90();

            // If we're hovering over a bag, revalidate the position with the new rotation
            if (activeBag != null)
            {
                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                float enter;
                Plane plane = new Plane(dragPlaneNormal, dragPlanePoint);

                if (plane.Raycast(ray, out enter))
                {
                    Vector3 targetPosition = ray.GetPoint(enter) + dragOffset;
                    UpdateBagHoverPosition(targetPosition, activeBag, draggedItem);
                }
            }
            // If we're hovering over a conveyor belt, revalidate the position with the new rotation
            else if (activeConveyorBelt != null)
            {
                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                float enter;
                Plane plane = new Plane(dragPlaneNormal, dragPlanePoint);

                if (plane.Raycast(ray, out enter))
                {
                    Vector3 targetPosition = ray.GetPoint(enter) + dragOffset;
                    UpdateConveyorHoverPosition(targetPosition, activeConveyorBelt, draggedItem);
                }
            }

            // Update drag offset so item is still centered on the cursor with the new rotation
            dragOffset = -GetHalfItemGridSize(draggedItem);
            // Debug.Log($"Rotated item {draggedItem.name}");
        }
    }

    void ReleaseItem()
    {
        // Check if we're hovering over the bag with a valid position
        if (bagGridPosition.HasValue && activeBag != null)
        {
            // Try to place item in bag
            if (activeBag.TryAddItem(draggedItem, bagGridPosition.Value.x, bagGridPosition.Value.z))
            {
                // Debug.Log($"Placed item {draggedItem.name} in bag at grid position {bagGridPosition.Value}");
                // Hide grid preview
                draggedItem.ShowGridPreview(false);
                draggedItem.OnDragEnd();
                draggedItem = null;
                bagGridPosition = null;
                activeConveyorBelt = null;
                conveyorGridPosition = null;
                isDraggingFromBag = false;

                if (IsBagFull(activeBag))
                {
                    packButton.Pack();
                }
            }
            else
            {
                // Failed to place in bag, keep dragging
                // Debug.Log($"Failed to place item {draggedItem.name} in bag");
            }
        }
        // Check if we're hovering over the conveyor belt with a valid position
        else if (conveyorGridPosition.HasValue && activeConveyorBelt != null)
        {
            // Try to place item on conveyor belt
            if (activeConveyorBelt.TryAddItemFromPlayer(draggedItem, conveyorGridPosition.Value.x, conveyorGridPosition.Value.y))
            {
                // Debug.Log($"Placed item {draggedItem.name} on conveyor belt at grid position {conveyorGridPosition.Value}");
                // Hide grid preview
                draggedItem.ShowGridPreview(false);
                draggedItem.OnDragEnd();
                draggedItem = null;
                conveyorGridPosition = null;
                bagGridPosition = null;
                activeBag = null;
                isDraggingFromBag = false;
            }
            else
            {
                // Failed to place on conveyor belt, keep dragging
                // Debug.Log($"Failed to place item {draggedItem.name} on conveyor belt");
            }
        }
        else
        {
            // Attempt to release item outside bag/conveyor belt, keep dragging
            // Debug.Log($"Invalid placement attempt of item {draggedItem.name}, keep dragging");
        }


    }

    private bool IsBagFull(Bag bag)
    {
        // check if top layer of bag is fully occupied
        return bag.IsTopLayerFull();
    }
}
