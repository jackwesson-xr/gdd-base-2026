/*
|-----------------------------------------------------------|
| Bullet.cs                                                 |
|-----------------------------------------------------------|
| This script spawns a bullet prefab at a specified start   |
| transform (gun) and launches it in a set direction and    |
| speed. The bullet can travel in either world or local     |
| space relative to the gun's transform.                    |
|-----------------------------------------------------------|
*/

using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("The bullet prefab to instantiate.")]
    public GameObject bulletPrefab;

    [Tooltip("The gun transform to use as the starting position. Defaults to this GameObject if empty.")]
    public Transform gun;

    [Tooltip("The speed at which the bullet travels.")]
    [Range(0.1f, 100f)]
    public float bulletSpeed = 10f;

    [Tooltip("Seconds before a fired bullet is removed, preventing unlimited accumulation.")]
    [Min(0.1f)] public float bulletLifetime = 8f;

    [Tooltip("The direction the bullet will travel in.")]
    public Vector3 launchDirection = Vector3.forward;

    [Tooltip("Choose whether the bullet moves in local or world space.")]
    public Space travelSpace = Space.World;

    private void Awake()
    {
        // If no gun is assigned, default to the current GameObject's transform
        if (gun == null)
        {
            gun = transform;
        }
    }

    /// <summary>
    /// Launches the bullet prefab from the gun's position and direction.
    /// </summary>
    public void Fire()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[Bullet] No bullet prefab assigned.");
            return;
        }

        // Instantiate the bullet at the gun's position and rotation
        GameObject bullet = Instantiate(bulletPrefab, gun.position, gun.rotation);

        // Ensure the bullet has a Rigidbody for movement
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bullet.AddComponent<Rigidbody>();
            rb.useGravity = false; // Disable gravity for the bullet by default
        }

        // Determine the velocity based on travel space
        Vector3 velocity = (travelSpace == Space.Self)
            ? gun.TransformDirection(launchDirection.normalized * bulletSpeed)
            : launchDirection.normalized * bulletSpeed;

        // Apply velocity to the bullet
        rb.linearVelocity = velocity;
        Destroy(bullet, Mathf.Max(0.1f, bulletLifetime));
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
