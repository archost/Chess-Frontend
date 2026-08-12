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
    [SerializeField] private AudioClip _promotionSound;

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
        switch (type)
        {
            case MoveType.Take:
                audioSource.clip = _captureSound;
                break;
            case MoveType.Castle:
                audioSource.clip = _castleSound;
                break;
            case MoveType.Promote:
                audioSource.clip = _promotionSound;
                break;
            case MoveType.EnPassant:
                audioSource.clip = _captureSound;
                break;
            default:
                audioSource.clip = _moveSound;
                break;
        }
        audioSource.Play();
    }

    void Update()
    {
        
    }
}
