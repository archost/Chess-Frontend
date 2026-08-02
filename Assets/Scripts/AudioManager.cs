using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip _moveSound;
    [SerializeField] private AudioClip _captureSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMoveSound()
    {
        audioSource.clip = _moveSound;
        audioSource.Play();
    }

    public void PlayCaptureSound()
    {
        audioSource.clip = _captureSound;
        audioSource.Play();
    }

    void Update()
    {
        
    }
}
