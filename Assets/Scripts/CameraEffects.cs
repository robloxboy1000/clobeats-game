using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraEffects : MonoBehaviour
{
    public GameObject cameraToTweak;
    public int fps = 20;
    float elapsed;
    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraToTweak == null)
        {
            cameraToTweak = gameObject;
        }
        elapsed += Time.deltaTime;
        if (elapsed > 1 / fps) {
            elapsed = 0;
            cam.Render();
        }
    }
}
