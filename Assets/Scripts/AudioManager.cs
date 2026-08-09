using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip _moveSound;
    [SerializeField] private AudioClip _captureSound;
    [SerializeField] private AudioClip _castleSound;

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

    public void PlayMoveSound(MoveType type)
    {
        if (type == MoveType.Take)
            audioSource.clip = _captureSound;
        else if (type == MoveType.Castle)
            audioSource.clip = _castleSound;
        else
            audioSource.clip = _moveSound;
        audioSource.Play();
    }

    void Update()
    {
        
    }
}
