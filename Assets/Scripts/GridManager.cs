using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Настройки поля")]
    public int width = 8;
    public int height = 8;

    [Header("Префаб фишки")]
    public GameObject piecePrefab;
    public Color[] pieceColors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };

    [Header("Эффекты")]
    public GameObject explosionPrefab; // Сюда перетащим префаб взрыва

    private GameObject[,] allPieces;
    private Piece firstSelectedPiece;

    void Start()
    {
        allPieces = new GameObject[width, height];
        CreateGrid();
    }

    void CreateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newPiece = Instantiate(piecePrefab);
                newPiece.name = $"Piece_{x}_{y}";

                // Выбираем случайный цвет, но проверяем, не создаст ли он тройку
                int randomIndex = Random.Range(0, pieceColors.Length);
                Color randomColor = pieceColors[randomIndex];

                // Проверяем: если две фишки слева или снизу того же цвета — меняем цвет
                bool colorIsBad = false;

                // Проверка по горизонтали (две слева)
                if (x >= 2)
                {
                    Color colorLeft1 = allPieces[x - 1, y].GetComponent<SpriteRenderer>().color;
                    Color colorLeft2 = allPieces[x - 2, y].GetComponent<SpriteRenderer>().color;
                    if (colorLeft1 == randomColor && colorLeft2 == randomColor)
                        colorIsBad = true;
                }

                // Проверка по вертикали (две снизу)
                if (y >= 2)
                {
                    Color colorDown1 = allPieces[x, y - 1].GetComponent<SpriteRenderer>().color;
                    Color colorDown2 = allPieces[x, y - 2].GetComponent<SpriteRenderer>().color;
                    if (colorDown1 == randomColor && colorDown2 == randomColor)
                        colorIsBad = true;
                }

                // Если цвет создаёт тройку — перебираем заново
                while (colorIsBad)
                {
                    randomIndex = Random.Range(0, pieceColors.Length);
                    randomColor = pieceColors[randomIndex];
                    colorIsBad = false;

                    if (x >= 2)
                    {
                        Color colorLeft1 = allPieces[x - 1, y].GetComponent<SpriteRenderer>().color;
                        Color colorLeft2 = allPieces[x - 2, y].GetComponent<SpriteRenderer>().color;
                        if (colorLeft1 == randomColor && colorLeft2 == randomColor)
                            colorIsBad = true;
                    }
                    if (y >= 2)
                    {
                        Color colorDown1 = allPieces[x, y - 1].GetComponent<SpriteRenderer>().color;
                        Color colorDown2 = allPieces[x, y - 2].GetComponent<SpriteRenderer>().color;
                        if (colorDown1 == randomColor && colorDown2 == randomColor)
                            colorIsBad = true;
                    }
                }

                newPiece.GetComponent<SpriteRenderer>().color = randomColor;

                Piece pieceScript = newPiece.GetComponent<Piece>();
                pieceScript.x = x;
                pieceScript.y = y;
                pieceScript.gridManager = this;
                pieceScript.originalColor = randomColor;

                newPiece.transform.position = new Vector3(x - (width - 1) / 2.0f, y - (height - 1) / 2.0f, 0);
                allPieces[x, y] = newPiece;
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null)
            {
                Piece piece = hit.GetComponent<Piece>();
                if (piece != null)
                {
                    Debug.Log("Клик по фишке: " + piece.name);
                    OnPieceClicked(piece);
                }
            }
        }
    }

    // Этот метод вызывается, когда игрок кликает по фишке
    public void OnPieceClicked(Piece piece)
    {
        // Если ничего не выбрано — запоминаем первую фишку и подсвечиваем её
        if (firstSelectedPiece == null)
        {
            firstSelectedPiece = piece;
            piece.GetComponent<SpriteRenderer>().color = Color.white; // Подсветка
        }
        else
        {
            // Если кликнули по той же самой фишке — сбрасываем выбор
            if (firstSelectedPiece == piece)
            {
                firstSelectedPiece.GetComponent<SpriteRenderer>().color = firstSelectedPiece.originalColor;
                firstSelectedPiece = null;
                return;
            }

            // Проверяем, являются ли фишки соседними (разница по X + разница по Y = 1)
            if (Mathf.Abs(firstSelectedPiece.x - piece.x) + Mathf.Abs(firstSelectedPiece.y - piece.y) == 1)
            {
                // Меняем местами
                SwapPieces(firstSelectedPiece, piece);
            }

            // В любом случае сбрасываем выбор и возвращаем цвет
            firstSelectedPiece.GetComponent<SpriteRenderer>().color = firstSelectedPiece.originalColor;
            firstSelectedPiece = null;
        }
    }

    // Логика обмена двух фишек
    void SwapPieces(Piece piece1, Piece piece2)
    {
        // 1. Меняем местами в массиве
        allPieces[piece1.x, piece1.y] = piece2.gameObject;
        allPieces[piece2.x, piece2.y] = piece1.gameObject;

        // 2. Меняем их координаты в скриптах
        int tempX = piece1.x;
        int tempY = piece1.y;
        piece1.x = piece2.x;
        piece1.y = piece2.y;
        piece2.x = tempX;
        piece2.y = tempY;

        // 3. Меняем их позиции в мире (визуально)
        Vector3 tempPos = piece1.transform.position;
        piece1.transform.position = piece2.transform.position;
        piece2.transform.position = tempPos;

        // 4. НОВОЕ: Проверяем, есть ли совпадения после обмена
        FindMatches();
    }

    
    void FindMatches()
    {
        System.Collections.Generic.List<GameObject> piecesToRemove = new System.Collections.Generic.List<GameObject>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null) continue;

                // Проверка горизонтали
                if (x < width - 2 && allPieces[x + 1, y] != null && allPieces[x + 2, y] != null)
                {
                    Piece p1 = allPieces[x, y].GetComponent<Piece>();
                    Piece p2 = allPieces[x + 1, y].GetComponent<Piece>();
                    Piece p3 = allPieces[x + 2, y].GetComponent<Piece>();

                    if (p1.originalColor == p2.originalColor && p2.originalColor == p3.originalColor)
                    {
                        if (!piecesToRemove.Contains(allPieces[x, y])) piecesToRemove.Add(allPieces[x, y]);
                        if (!piecesToRemove.Contains(allPieces[x + 1, y])) piecesToRemove.Add(allPieces[x + 1, y]);
                        if (!piecesToRemove.Contains(allPieces[x + 2, y])) piecesToRemove.Add(allPieces[x + 2, y]);
                    }
                }

                // Проверка вертикали
                if (y < height - 2 && allPieces[x, y + 1] != null && allPieces[x, y + 2] != null)
                {
                    Piece p1 = allPieces[x, y].GetComponent<Piece>();
                    Piece p2 = allPieces[x, y + 1].GetComponent<Piece>();
                    Piece p3 = allPieces[x, y + 2].GetComponent<Piece>();

                    if (p1.originalColor == p2.originalColor && p2.originalColor == p3.originalColor)
                    {
                        if (!piecesToRemove.Contains(allPieces[x, y])) piecesToRemove.Add(allPieces[x, y]);
                        if (!piecesToRemove.Contains(allPieces[x, y + 1])) piecesToRemove.Add(allPieces[x, y + 1]);
                        if (!piecesToRemove.Contains(allPieces[x, y + 2])) piecesToRemove.Add(allPieces[x, y + 2]);
                    }
                }
            }
        }

        // Удаляем фишки + создаём взрывы
        foreach (GameObject piece in piecesToRemove)
        {
            Piece p = piece.GetComponent<Piece>();
            allPieces[p.x, p.y] = null;

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, piece.transform.position, Quaternion.identity);
                Destroy(explosion, 1.5f);
            }

            Destroy(piece);
        }

        // ЗАПУСКАЕМ ЗАПОЛНЕНИЕ
        if (piecesToRemove.Count > 0)
        {
            Debug.Log($"Удалено фишек: {piecesToRemove.Count}");
            RefillBoard();
        }
    }

    void RefillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            int holes = 0;

            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null)
                {
                    holes++;
                }
                else if (holes > 0)
                {
                    Piece p = allPieces[x, y].GetComponent<Piece>();
                    allPieces[x, y - holes] = allPieces[x, y];
                    allPieces[x, y] = null;
                    p.y = y - holes;
                    // Вместо мгновенного перемещения — плавное
                    StartCoroutine(MovePiece(p, new Vector3(x - (width - 1) / 2.0f, p.y - (height - 1) / 2.0f, 0)));
                }
            }

            for (int i = 0; i < holes; i++)
            {
                int y = height - 1 - i;
                GameObject newPiece = Instantiate(piecePrefab);
                newPiece.name = $"Piece_{x}_{y}";

                int randomIndex = Random.Range(0, pieceColors.Length);
                Color randomColor = pieceColors[randomIndex];
                newPiece.GetComponent<SpriteRenderer>().color = randomColor;

                Piece pieceScript = newPiece.GetComponent<Piece>();
                pieceScript.x = x;
                pieceScript.y = y;
                pieceScript.gridManager = this;
                pieceScript.originalColor = randomColor;

                // Ставим новую фишку ВЫШЕ поля, чтобы она «упала»
                Vector3 startPos = new Vector3(x - (width - 1) / 2.0f, height - (height - 1) / 2.0f + 1, 0);
                Vector3 endPos = new Vector3(x - (width - 1) / 2.0f, y - (height - 1) / 2.0f, 0);
                newPiece.transform.position = startPos;
                allPieces[x, y] = newPiece;

                StartCoroutine(MovePiece(pieceScript, endPos));
            }
        }

        Invoke("FindMatches", 0.35f); // Чуть дольше, чтобы анимация успела
    }

    // НОВОЕ: Плавное движение фишки
    System.Collections.IEnumerator MovePiece(Piece piece, Vector3 targetPos)
    {
        float duration = 0.2f; // Время падения в секундах
        float elapsed = 0f;
        Vector3 startPos = piece.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            piece.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        piece.transform.position = targetPos;
    }
}