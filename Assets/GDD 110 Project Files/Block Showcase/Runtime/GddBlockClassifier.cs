using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Works out which toolkit template a GameObject is from the functions on it, so juice also lands on blocks
    /// students assembled themselves with Add Function. Returns the template name, or null.
    /// </summary>
    public static class GddBlockClassifier
    {
        public static string Classify(GameObject go)
        {
            if (go == null || go.GetComponent<GddActor>() != null) return null;

            if (go.GetComponent<GddDialog>() != null) return "Dialog NPC";
            if (go.GetComponent<GddShooter>() != null) return "Shooter";
            if (go.GetComponent<GddWearableBody>() != null) return "Wearable Body";
            if (go.GetComponent<GddPopupMessage>() != null) return null;   // it is its own card; no guide card on top
            var collectible = go.GetComponent<GddCollectible>();
            if (collectible != null)
                return collectible.kind == GddCollectible.Kind.SnapToPlayer ? "Snap Collectible" : collectible.kind == GddCollectible.Kind.PickupCounter ? "Counter Collectible" : "Key Item";
            if (go.GetComponent<GddKeyLock>() != null) return "Key Lock";
            if (go.GetComponent<GddCameraSwitch>() != null) return "Camera Switch";
            if (go.GetComponent<GddPlayTimeline>() != null) return "Play Timeline";
            if (go.GetComponent<GddMover>() != null) return "Mover";
            if (go.GetComponent<GddToggleObjects>() != null) return "Toggle Objects";

            var contact = go.GetComponent<GddContactEffect>();
            if (contact != null)
            {
                switch (contact.effect)
                {
                    case GddContactEffect.Effect.Damage: return "Hazard";
                    case GddContactEffect.Effect.Launch: return "Bounce Pad";
                    case GddContactEffect.Effect.Teleport: return "Teleporter";
                    case GddContactEffect.Effect.NextScene: return "Next Scene Portal";
                    case GddContactEffect.Effect.Heal: return "Health Collectible";
                    default: return "Event Trigger";
                }
            }

            var pickup = go.GetComponent<GddPickup>();
            if (pickup != null)
            {
                var power = go.GetComponentInChildren<GddPower>(true);
                var kind = power != null && power.definition != null ? power.definition.kind : GddPowerKind.EventsOnly;
                if (pickup.mode == GddPickup.PickupMode.Collect)
                    return kind == GddPowerKind.Heal ? "Health Collectible" : "Score Collectible";
                if (kind == GddPowerKind.Projectile) return "Blaster Pickup";
                if (kind == GddPowerKind.SpeedBoost) return "Speed Boots";
                if (kind == GddPowerKind.JumpBoost) return "Jump Boots";
                if (pickup.attachment == GddSocket.Head) return "Hat Pickup";
                if (pickup.attachment == GddSocket.Chest) return "Chest Pickup";
                return "Hand Pickup";
            }

            var follow = go.GetComponent<GddFollow>();
            if (follow != null) return follow.contactDamage > 0 ? "Enemy" : "Companion";
            if (go.GetComponent<GddSpawn>() != null) return "Spawner";
            if (go.GetComponent<GddTimer>() != null) return "Timer";
            if (go.GetComponent<GddInteractable>() != null) return "Interact Button";
            if (go.GetComponent<GddTrigger>() != null) return "Event Trigger";
            if (go.GetComponent<GddMotion>() != null) return "Rotating Object";
            return null;
        }
    }
}
