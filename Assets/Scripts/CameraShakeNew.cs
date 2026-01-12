using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShakeNew : MonoBehaviour
{
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPosition + new Vector3(x, y, 0); // Adjust Z if needed for 3D shake
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        transform.localPosition = originalPosition; // Reset to original position
    }
    public void AnimationShake()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("HighwayShakeCamera");
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
