using UnityEngine;

// Hides renderers on the GameObject (and children) until the object reaches a given world Z coordinate.
// Only toggles Renderer components — leaves colliders and other components active so gameplay still works.
public class VisibilityGate : MonoBehaviour
{
    // When true, colliders (3D & 2D) are disabled while the object is hidden and re-enabled when revealed.
    public bool disableColliders = false;

    Renderer[] renderers;
    Collider[] colliders3D;
    Collider2D[] colliders2D;
    float revealY = 0f;
    bool revealed = false;
    const float EPS = 1.111f;

    // Initialize can be called while the GameObject is inactive; it will cache renderers and hide them.
    // If colliders are configured to be disabled, those will be cached and disabled as well.
    public void Initialize(float revealYWorld)
    {
        revealY = revealYWorld;
        revealed = false;
        // get all renderers including inactive children
        renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        if (disableColliders)
        {
            colliders3D = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders3D.Length; i++)
            {
                if (colliders3D[i] != null)
                {
                    colliders3D[i].enabled = false;
                }
            }

            colliders2D = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders2D.Length; i++)
            {
                if (colliders2D[i] != null)
                {
                    colliders2D[i].enabled = false;
                }
            }
        }
        
        // Register with the centralized manager so it will reveal this gate when appropriate.
        VisibilityGateManager.Instance.Register(this);
    }
    // Called by the centralized manager each frame to check if this gate should reveal visuals.
    public void CheckAndReveal()
    {
        if (revealed) return;
        if (transform.position.y <= revealY + EPS || transform.position.y >= revealY + EPS)
        {
            revealed = true;
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = true;
            }

            if (disableColliders)
            {
                if (colliders3D == null) colliders3D = GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders3D.Length; i++)
                {
                    if (colliders3D[i] != null)
                        colliders3D[i].enabled = true;
                }

                if (colliders2D == null) colliders2D = GetComponentsInChildren<Collider2D>(true);
                for (int i = 0; i < colliders2D.Length; i++)
                {
                    if (colliders2D[i] != null)
                        colliders2D[i].enabled = true;
                }
            }

            // Let the manager stop calling us
            VisibilityGateManager.Instance.Unregister(this);
        }
    }

    // Expose revealed state for manager bookkeeping
    public bool IsRevealed { get { return revealed; } }

    void OnDestroy()
    {
        // Ensure we don't leave stale references in the manager
        if (VisibilityGateManager.Instance != null)
        {
            VisibilityGateManager.Instance.Unregister(this);
        }
    }
}
