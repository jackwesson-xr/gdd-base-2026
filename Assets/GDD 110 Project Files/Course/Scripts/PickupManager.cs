/*
|-----------------------------------------------------------|
| PickupManager Script                                       |
|-----------------------------------------------------------|
| This script manages the pickup and placement of objects   |
| from a pedestal to a specified location (e.g., player's   |
| hand). It handles swapping objects if the hand is already |
| full, ensuring proper tagging and detaching.              |
|-----------------------------------------------------------|
*/

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupManager : MonoBehaviour
{
    [Header("Pickup Object Settings")]
    [Tooltip("The parent of the attachment point to pick up.")]
    public GameObject pickupZoneLocation;

    [Tooltip("The target location for the picked-up object.")]
    public Transform equipLocation;

    [Header("Debug Settings")]
    [Tooltip("Enable to see detailed logs.")]
    public bool debugMode = false;

    private Collider triggerZone;

    private void Awake()
    {
        // Ensure the trigger zone is set up correctly
        triggerZone = GetComponent<Collider>();
        triggerZone.isTrigger = true;

        if (pickupZoneLocation == null)
        {
            Debug.LogError("[PickupManager] Object Parent is not assigned!");
        }

        if (equipLocation == null)
        {
            Debug.LogError("[PickupManager] Target Location is not assigned!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (debugMode) Debug.Log("[PickupManager] Player entered the trigger zone.");

            HandlePickup();
        }
    }

    private void HandlePickup()
    {
        if (pickupZoneLocation == null || equipLocation == null)
        {
            if (debugMode) Debug.LogError("[PickupManager] Missing required references!");
            return;
        }

        // Get the attachment point of the pickupZoneLocation
        if (pickupZoneLocation.transform.childCount == 0) return;
        Transform attachmentPoint = pickupZoneLocation.transform.GetChild(0);
        if (attachmentPoint == null)
        {
            Debug.LogError("[PickupManager] No attachment point found on the object parent!");
            return;
        }

        // Check if the target location already has a "Hand" tagged object
        Transform currentHandObject = null;
        foreach (Transform child in equipLocation)
        {
            if (child.CompareTag("Hand"))
            {
                currentHandObject = child;
                break;
            }
        }

        if (currentHandObject != null)
        {
            if (debugMode) Debug.Log("[PickupManager] Hand is already full. Swapping objects.");

            // Untag the current hand object
            currentHandObject.tag = "Untagged";

            // Move the current hand object back to the pedestal
            currentHandObject.SetParent(pickupZoneLocation.transform);
            currentHandObject.localPosition = Vector3.zero;
        }
        else
        {
            if (debugMode) Debug.Log("[PickupManager] Hand is empty. Picking up new object.");
        }

        // Move the new object to the target location
        attachmentPoint.SetParent(equipLocation);
        attachmentPoint.localPosition = Vector3.zero;
        attachmentPoint.localRotation = Quaternion.identity;

        // Tag the new object as "Hand"
        attachmentPoint.tag = "Hand";

        if (debugMode) Debug.Log("[PickupManager] New object picked up and tagged as 'Hand'.");
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
