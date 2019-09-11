using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAudioController : MonoBehaviour
{
    private ParticleSystem[] pSystems;

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        pSystems = GetComponentsInChildren<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var pSystem in pSystems)
        {
            // Ak priebieha animacia a nehra audio
            if (!audioSource.isPlaying && pSystem.isEmitting)
            {
                audioSource.Play();
            }
            else if (audioSource.isPlaying && !pSystem.isEmitting)
            {
                audioSource.Pause();
            }
        }
    }
}
