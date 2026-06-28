using UnityEngine;

public class moveZed : MonoBehaviour
{
    // unused, use GlobalMoveY instead
    public float speed = 5f;
    public bool tiled = true;
    public bool isPlaying = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }
    void Awake()
    {
        float userSpeedSetting = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        speed = userSpeedSetting;
    }

    // Update is called once per frame
    void Update()
    {
        float userSpeedSetting = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        speed = userSpeedSetting;

        
        if (isPlaying)
        {
            transform.Translate(0, -speed * Time.deltaTime, 0);
            if (tiled && transform.position.y < 0f) // highway texture
            {
                transform.position = new Vector3(transform.position.x, 20f, transform.position.z);
            }
        }
        else
        {
            // Do nothing when not playing
        }
    }
}
