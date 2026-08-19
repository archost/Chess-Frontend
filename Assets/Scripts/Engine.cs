using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

using Random = UnityEngine.Random;

public class Engine : MonoBehaviour
{
    public static Engine Instance { get; private set; }

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

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator MakeARandomMove()
    {
        Move move = new Move();

        List<Move> moveList = MoveGenerator.Instance.legalMoves;

        if (moveList.Count > 0)
        {
            move = moveList[Random.Range(0, moveList.Count)];
        }

        yield return new WaitForSeconds(0.1f);

        if (moveList.Count > 0)
        {
            move.promoteTo |= BoardManager.Instance.colorToMove;
            GameController.Instance.ExecuteMove(move);
        }
    }
}
