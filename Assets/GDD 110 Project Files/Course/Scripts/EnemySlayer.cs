/*
|-----------------------------------------------------------|
| EnemySlayer                                               |
|-----------------------------------------------------------|
| A script to detect collisions with objects tagged as      |
| "Enemy" and trigger a Unity Event upon contact. Includes  |
| an option to automatically call the "Slay" method on      |
| the enemy GameObject if the "slayEnemy" variable is true. |
|-----------------------------------------------------------|
*/

using UnityEngine;
using UnityEngine.Events;

public class EnemySlayer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, slays the enemy on contact by triggering its Slay() method.")]
    public bool slayEnemy = false;

    [Header("Events")]
    [Tooltip("Event triggered when this object hits an enemy.")]
    public UnityEvent onEnemyHit;

    private void OnCollisionEnter(Collision collision)
    {
        HandleEnemyCollision(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleEnemyCollision(other);
    }

    private void HandleEnemyCollision(Collider collider)
    {
        // Check if the object collided with is tagged as "Enemy"
        if (collider.CompareTag("Enemy"))
        {
            // Trigger the Unity Event
            onEnemyHit?.Invoke();

            // If slayEnemy is true, attempt to call the Slay() method
            if (slayEnemy)
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.Slay();
                }
                else
                {
                    Debug.LogWarning("SlayEnemy is enabled, but the object tagged 'Enemy' has no Enemy script attached.", collider);
                }
            }
        }
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
