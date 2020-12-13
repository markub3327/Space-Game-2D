using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    // Pri konci existencie objektu
    private void OnDestroy()
    {
        var generator = GetComponentInParent<HealthsGenerator>();
        if (generator != null)
            generator.freePoints.Add(transform.position);
    }
}
