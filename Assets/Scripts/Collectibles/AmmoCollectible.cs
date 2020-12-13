using UnityEngine;

public class AmmoCollectible : MonoBehaviour
{
    // Pri konci existencie objektu
    private void OnDestroy()
    {
        var generator = GetComponentInParent<AmmoGenerator>();
        if (generator != null)
            generator.freePoints.Add(transform.position);
    }
}