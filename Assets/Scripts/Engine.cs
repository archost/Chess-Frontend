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

        List<Move> moveList = MoveGenerator.Instance.FilterLegalMoves();

        if (moveList.Count > 0)
        {
            move = moveList[Random.Range(0, moveList.Count)];
        }

        yield return new WaitForSeconds(0.2f);

        if (moveList.Count > 0)
        {
            GameController.Instance.ExecuteMove(move, Piece.None);
        }
    }
}
