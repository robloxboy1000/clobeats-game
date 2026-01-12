using UnityEngine;

public class moveZed : MonoBehaviour
{
    // unused, use GlobalMoveY instead
    public float speed = 5f;
    public bool tiled = false;
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
        if (isPlaying)
        {
            transform.Translate(0, 0, -speed * Time.deltaTime);
            if (tiled && transform.position.z < 0f) // highway texture
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, 5f);
            }
        }
        else
        {
            // Do nothing when not playing
        }
    }
}
