using Unity.VisualScripting;
using UnityEngine;

public class riseY : MonoBehaviour
{
    public float riseTo = 0f;
    public float riseFrom = 0f;
    public float speed = 5f;
    public GameObject objectToRise;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (objectToRise == null)
        {
            objectToRise = this.gameObject;
        }
        // Set initial Y position to riseFrom
        Vector3 pos = objectToRise.transform.position;
        pos.y = riseFrom;
        objectToRise.transform.position = pos;
        objectToRise.transform.position = new Vector3(objectToRise.transform.position.x, riseFrom, objectToRise.transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (objectToRise.transform.position.y < riseTo)
        {
            float newY = objectToRise.transform.position.y + speed * Time.deltaTime;
            newY = Mathf.Min(newY, riseTo); // Clamp so it doesn't overshoot
            Vector3 pos = objectToRise.transform.position;
            pos.y = newY;
            objectToRise.transform.position = pos;
        }
    }
}
