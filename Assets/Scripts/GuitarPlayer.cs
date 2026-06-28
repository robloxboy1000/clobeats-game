using UnityEngine;


public class GuitarPlayer : MonoBehaviour
{
    public void NoteHit()
    {
        SFXPlayer player = FindAnyObjectByType<SFXPlayer>();
        if (player != null)
        {
            //Shake(gameObject, 0.2f, 100);
            //player.PlayClip("Tick");
        }
    }
    public void SetOpacity(float amount)
    {
        float amounClamp = Mathf.Clamp01(amount);
        SpriteRenderer[] sprites = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprt in sprites)
        {
            sprt.color = new Color(sprt.color.r, sprt.color.g, sprt.color.b, amounClamp);
        }
    }

    public System.Collections.IEnumerator Shake(GameObject objectToShake, float duration, float magnitude)
    {
        Vector3 originalPosition = objectToShake.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float z = Mathf.Lerp(0f, -0.2f, elapsed);

            objectToShake.transform.localPosition = originalPosition + new Vector3(0, 0, z); // Adjust Z if needed for 3D shake
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        objectToShake.transform.localPosition = originalPosition; // Reset to original position
    }
    
    

    
    
}
