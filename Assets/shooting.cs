using UnityEngine;

public class FixedTargetShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    public float fireRate = 2f;
    public float despawnTime = 5f;   // default despawn timer

    // ✅ The fixed world target position
    private Vector3 targetPosition = new Vector3(21.6000004f, 92.7337189f, -104.599998f);

    private void update()
    {
        // Repeatedly shoot at the target
        InvokeRepeating(nameof(ShootAtTarget), fireRate, fireRate);
    }

    void ShootAtTarget()
    {
        if (projectilePrefab == null) return;

        // ✅ Spawn projectile
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // ✅ Auto-despawn
        Destroy(proj, despawnTime);

        // ✅ Get direction toward the fixed position
        Vector3 direction = (targetPosition - transform.position).normalized;

        // ✅ Launch projectile
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * projectileSpeed;
        }
    }
}