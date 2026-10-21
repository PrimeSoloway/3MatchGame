using TMPro; // Обязательно для работы с текстом UI
using UnityEngine;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    [Header("Настройки поля")]
    public int width = 8;
    public int height = 8;

    [Header("Префаб фишки")]
    public GameObject piecePrefab;
    public Color[] pieceColors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };

    [Header("Эффекты")]
    public GameObject explosionPrefab;
    
    [Header("Героиня (Анимации)")]
    public HeroineAnimator heroineAnimator;
    
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI movesText;
    public GameObject gameOverText; // Сюда перетащим наш текст Game Over
    private bool isGameOver = false; // Флаг, чтобы игра остановилась

    private int currentLevel = 1;
    private int score = 0;
    private int moves = 20; // Даем 20 ходов
    public int targetScore = 1000; // Цель по очкам
    public GameObject victoryText; // Ссылка на текст победы

    private GameObject[,] allPieces;
    private Piece firstSelectedPiece;

    void Start()
    {
        // Настраиваем сложность в зависимости от уровня
        targetScore = 1000 * currentLevel; // На 1 уровне цель 1000 очков
        moves = 25 - (currentLevel * 2);   // На 1 уровне будет 23 хода

        allPieces = new GameObject[width, height];
        CreateGrid();
        UpdateUI();
    }

    void CreateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newPiece = Instantiate(piecePrefab);
                newPiece.name = $"Piece_{x}_{y}";

                int randomIndex = Random.Range(0, pieceColors.Length);
                Color randomColor = pieceColors[randomIndex];

                bool colorIsBad = false;

                if (x >= 2)
                {
                    Color colorLeft1 = allPieces[x - 1, y].GetComponent<SpriteRenderer>().color;
                    Color colorLeft2 = allPieces[x - 2, y].GetComponent<SpriteRenderer>().color;
                    if (colorLeft1 == randomColor && colorLeft2 == randomColor) colorIsBad = true;
                }

                if (y >= 2)
                {
                    Color colorDown1 = allPieces[x, y - 1].GetComponent<SpriteRenderer>().color;
                    Color colorDown2 = allPieces[x, y - 2].GetComponent<SpriteRenderer>().color;
                    if (colorDown1 == randomColor && colorDown2 == randomColor) colorIsBad = true;
                }

                while (colorIsBad)
                {
                    randomIndex = Random.Range(0, pieceColors.Length);
                    randomColor = pieceColors[randomIndex];
                    colorIsBad = false;

                    if (x >= 2)
                    {
                        Color colorLeft1 = allPieces[x - 1, y].GetComponent<SpriteRenderer>().color;
                        Color colorLeft2 = allPieces[x - 2, y].GetComponent<SpriteRenderer>().color;
                        if (colorLeft1 == randomColor && colorLeft2 == randomColor) colorIsBad = true;
                    }
                    if (y >= 2)
                    {
                        Color colorDown1 = allPieces[x, y - 1].GetComponent<SpriteRenderer>().color;
                        Color colorDown2 = allPieces[x, y - 2].GetComponent<SpriteRenderer>().color;
                        if (colorDown1 == randomColor && colorDown2 == randomColor) colorIsBad = true;
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
        if (isGameOver) return; // Если игра окончена — не реагируем на клики

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null)
            {
                Piece piece = hit.GetComponent<Piece>();
                if (piece != null)
                {
                    OnPieceClicked(piece);
                }
            }
        }
    }

    public void OnPieceClicked(Piece piece)
    {
        if (firstSelectedPiece == null)
        {
            firstSelectedPiece = piece;
            piece.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else
        {
            if (firstSelectedPiece == piece)
            {
                firstSelectedPiece.GetComponent<SpriteRenderer>().color = firstSelectedPiece.originalColor;
                firstSelectedPiece = null;
                return;
            }

            if (Mathf.Abs(firstSelectedPiece.x - piece.x) + Mathf.Abs(firstSelectedPiece.y - piece.y) == 1)
            {
                SwapPieces(firstSelectedPiece, piece);
            }

            firstSelectedPiece.GetComponent<SpriteRenderer>().color = firstSelectedPiece.originalColor;
            firstSelectedPiece = null;
        }
    }

    void SwapPieces(Piece piece1, Piece piece2, bool isReverting = false)
    {
        // Меняем местами в массиве
        allPieces[piece1.x, piece1.y] = piece2.gameObject;
        allPieces[piece2.x, piece2.y] = piece1.gameObject;

        // Меняем координаты
        int tempX = piece1.x;
        int tempY = piece1.y;
        piece1.x = piece2.x;
        piece1.y = piece2.y;
        piece2.x = tempX;
        piece2.y = tempY;

        // Меняем позиции
        Vector3 tempPos = piece1.transform.position;
        piece1.transform.position = piece2.transform.position;
        piece2.transform.position = tempPos;

        // Если это не откат — проверяем совпадения и тратим ход
        if (!isReverting)
        {
            int matches = FindMatches();

            if (matches == 0)
            {
                Debug.Log("Нет совпадений! Возвращаем фишки.");
                StartCoroutine(RevertSwap(piece1, piece2));
            }
            else
            {
                // Тратим ход только если совпадение удалось!
                moves--;
                UpdateUI();
            }
        }
    }

    System.Collections.IEnumerator RevertSwap(Piece p1, Piece p2)
    {
        yield return new WaitForSeconds(0.2f);
        SwapPieces(p1, p2, true);
    }

    // ВАЖНО: Метод возвращает int (количество удаленных фишек)
    int FindMatches()
    {
        System.Collections.Generic.List<GameObject> piecesToRemove = new System.Collections.Generic.List<GameObject>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null) continue;

                // Горизонталь
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

                // Вертикаль
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

        // Если что-то нашли
        if (piecesToRemove.Count > 0)
        {
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

            score += piecesToRemove.Count * 10;
            UpdateUI();
            RefillBoard();
        }

        return piecesToRemove.Count;
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

                Vector3 startPos = new Vector3(x - (width - 1) / 2.0f, height - (height - 1) / 2.0f + 1, 0);
                Vector3 endPos = new Vector3(x - (width - 1) / 2.0f, y - (height - 1) / 2.0f, 0);
                newPiece.transform.position = startPos;
                allPieces[x, y] = newPiece;

                StartCoroutine(MovePiece(pieceScript, endPos));
            }
        }

        Invoke("FindMatches", 0.5f); // Ждем падения и проверяем каскады
    }

    System.Collections.IEnumerator MovePiece(Piece piece, Vector3 targetPos)
    {
        float duration = 0.2f;
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

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score + " / " + targetScore;
        if (movesText != null) movesText.text = "Moves: " + moves;

        // 1. Проверяем победу
        if (score >= targetScore && !isGameOver)
        {
            Victory();
            return; // Выходим, чтобы не сработал Game Over
        }

        // 2. Проверяем поражение (если ходы кончились)
        if (moves <= 0 && !isGameOver)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Игра окончена! Твой счет: " + score);

        if (gameOverText != null)
        {
            gameOverText.SetActive(true); // Показываем текст
        }
        if (heroineAnimator != null) heroineAnimator.PlayDefeat();
    }
    void Victory()
    {
        isGameOver = true;
        Debug.Log("Победа! Твой счет: " + score);

        if (victoryText != null)
        {
            victoryText.SetActive(true); // Показываем текст победы
        }
        if (heroineAnimator != null) heroineAnimator.PlayVictory();
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}