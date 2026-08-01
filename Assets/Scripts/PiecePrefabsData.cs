using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PiecePrefabs", menuName = "Chess/Piece Prefabs")]
public class PiecePrefabsData : ScriptableObject
{
    [SerializeField] private List<PiecePrefabEntry> entries;

    private Dictionary<int, GameObject> _prefabDictionary;

    // Метод вызывается при загрузке ассета
    private void OnEnable()
    {
        _prefabDictionary = new Dictionary<int, GameObject>();
        if (entries != null)
        {
            foreach (var entry in entries)
            {
                _prefabDictionary[entry.pieceType] = entry.prefab;
            }
        }
    }

    public GameObject GetPrefab(int pieceType)
    {
        return _prefabDictionary.TryGetValue(pieceType, out var prefab) ? prefab : null;
    }
}