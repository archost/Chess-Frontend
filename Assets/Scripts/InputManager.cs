using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    [SerializeField] private Camera _camera;

    private SquareView _selectedSquare;
    public bool isPromotion = false;

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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ProcessClick(0);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            ProcessClick(1);
        }
    }

    private void ProcessClick(int mouseButton)
    {
        RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (rayHit.transform != null && rayHit.transform.TryGetComponent<SquareView>(out _selectedSquare))
        {
            if (!isPromotion)
            {
                if (mouseButton == 0)
                    GameController.Instance.ProcessClick(_selectedSquare.squareIndex);
                if (mouseButton == 1)
                    GameController.Instance.ProcessRightClick(_selectedSquare.squareIndex);
            }
            else if (_selectedSquare.gameObject.GetComponent<SpriteRenderer>().sortingOrder == 2)
            {
                // Нам нужно, чтобы он обрабатывал только клики на promotionMenu. Пока определяю с помощью sortingOrder
                GameController.Instance.OnPromotionPieceSelected(_selectedSquare.squareIndex);
                isPromotion = false;
            }
        }
    }
}
