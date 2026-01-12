using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VenueAnimator : MonoBehaviour
{
    public Animation venueAnimation;
    // Start is called before the first frame update
    void Start()
    {
        venueAnimation.Play("VenueEntry");
        venueAnimation.PlayQueued("VenueIdle");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
