using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ОБЯЗАТЕЛЬНО добавили для работы со Slider (полосками здоровья)

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

    [Header("Анимация огня")]
    public float pulseSpeed = 5f;      // Скорость пульсации
    public float pulseAmount = 0.1f;   // Насколько сильно огонь меняет размер
    private Vector3 originalFireScale; // Запоминаем изначальный размер огня

    [Header("Героиня (Анимации)")]
    public HeroineAnimator heroineAnimator;

    [Header("Враги")]
    [Header("Визуал врагов")]
    public Image enemyImage; // Картинка врага (Enemy_UI)
    public Sprite[] enemySprites; // Массив твоих 11 картинок

    [Header("Прогресс и Экономика")]
    public int currentLevel = 1;
    public float baseEnemyHealth = 100f;
    public int crystals = 0;
    public int playerDamage = 10;
    public TextMeshProUGUI crystalsText;

    [Header("Здоровье")]
    public float maxPlayerHealth = 100f;
    public float currentPlayerHealth = 100f;
    public float maxEnemyHealth = 100f;
    public float currentEnemyHealth = 100f;

    [Header("Мана")]
    public int maxMana = 25;
    public Image manaFlaskLiquid;

    [Header("UI")]
    public Image healthFlaskLiquid; // Фласка здоровья игрока
    public Slider enemyHealthBar;  // Полоска здоровья врага
    public TextMeshProUGUI movesText;
    public GameObject gameOverText;
    public GameObject victoryText;
    public GameObject fireAura;    // Ссылка на наш огонь
    public Image caveOverlay;      // Ссылка на затемнение (если еще не добавил)
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText;
    public TextMeshProUGUI enemyLevelText;

    private bool isGameOver = false;
    private int moves = 20;

    private GameObject[,] allPieces;
    private Piece firstSelectedPiece;

    void Start()
    {
        // Настраиваем сложность
        moves = 25 - (currentLevel * 2);

        // Настраиваем здоровье
        maxEnemyHealth = baseEnemyHealth * currentLevel;
        currentEnemyHealth = maxEnemyHealth;

        allPieces = new GameObject[width, height];
        CreateGrid();
        UpdateUI();
        if (fireAura != null)
        {
            originalFireScale = fireAura.transform.localScale;
        }
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
        if (fireAura != null && fireAura.activeSelf)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            fireAura.transform.localScale = originalFireScale * pulse;
        }
        if (isGameOver) return;

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
        allPieces[piece1.x, piece1.y] = piece2.gameObject;
        allPieces[piece2.x, piece2.y] = piece1.gameObject;

        int tempX = piece1.x;
        int tempY = piece1.y;
        piece1.x = piece2.x;
        piece1.y = piece2.y;
        piece2.x = tempX;
        piece2.y = tempY;

        Vector3 tempPos = piece1.transform.position;
        piece1.transform.position = piece2.transform.position;
        piece2.transform.position = tempPos;

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
                moves--;
                UpdateUI();

                // Враг бьет в ответ за каждый потраченный ход
                currentPlayerHealth -= 5f;
                if (currentPlayerHealth < 0) currentPlayerHealth = 0;
                UpdateUI();
            }
        }
    }

    System.Collections.IEnumerator RevertSwap(Piece p1, Piece p2)
    {
        yield return new WaitForSeconds(0.2f);
        SwapPieces(p1, p2, true);
    }

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

        if (piecesToRemove.Count > 0)
        {
            int greenCount = 0;
            int otherCount = 0;
            int blueCount = 0; // Новая переменная для синих фишек

            // Считаем фишки по цветам
            foreach (GameObject pieceObj in piecesToRemove)
            {
                Piece p = pieceObj.GetComponent<Piece>();

                if (p.originalColor == Color.green) greenCount++;
                else if (p.originalColor == Color.blue) blueCount++; // Если синяя — плюс мана
                else otherCount++; // Остальные бьют врага
                               
                allPieces[p.x, p.y] = null;

                if (explosionPrefab != null)
                {
                    GameObject explosion = Instantiate(explosionPrefab, pieceObj.transform.position, Quaternion.identity);
                    Destroy(explosion, 1.5f);
                }

                Destroy(pieceObj);
            }

            // ЛЕЧЕНИЕ (зеленые фишки)
            if (greenCount > 0)
            {
                currentPlayerHealth += greenCount * 5f;
                if (currentPlayerHealth > maxPlayerHealth) currentPlayerHealth = maxPlayerHealth;
            }

            // ВОССТАНОВЛЕНИЕ МАНЫ (синие фишки)
            if (blueCount > 0)
            {
                moves += blueCount; // Добавляем ману (1 фишка = 1 мана)

                // Не даем мане превысить максимум
                if (moves > maxMana) moves = maxMana;

                Debug.Log("Собрано синих фишек: " + blueCount + ". Мана восстановлена!");
            }

            // УРОН (остальные фишки)
            if (otherCount > 0)
            {
                currentEnemyHealth -= otherCount * playerDamage;
                StartCoroutine(FlashEnemyDamage());
                if (currentEnemyHealth <= 0)
                {
                    currentEnemyHealth = 0;
                    EnemyDefeated();
                }
            }

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

        Invoke("FindMatches", 0.5f);
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
        if (movesText != null) movesText.text = "Ходы: " + moves + " / " + maxMana; // Пишем макс. ману

        // Обновляем полоски
        if (healthFlaskLiquid != null)
            healthFlaskLiquid.fillAmount = currentPlayerHealth / maxPlayerHealth;
        if (manaFlaskLiquid != null)
            manaFlaskLiquid.fillAmount = (float)moves / maxMana;
        if (enemyHealthBar != null) enemyHealthBar.value = currentEnemyHealth;

        // Обновляем цифры
        if (playerHealthText != null)
            playerHealthText.text = Mathf.RoundToInt(currentPlayerHealth) + " / " + Mathf.RoundToInt(maxPlayerHealth);

        if (enemyHealthText != null)
            enemyHealthText.text = Mathf.RoundToInt(currentEnemyHealth) + " / " + Mathf.RoundToInt(maxEnemyHealth);

        if (enemyLevelText != null)
            enemyLevelText.text = "Уровень " + currentLevel;

        // Проверяем поражение
        if ((moves <= 0 || currentPlayerHealth <= 0) && !isGameOver)
        {
            GameOver();
        }
    }

    void EnemyDefeated()
    {
        Debug.Log("Враг повержен!");

        // Даем кристаллы
        int reward = 10 + (currentLevel * 5);
        crystals += reward;
        if (crystalsText != null) crystalsText.text = "Кристаллы: " + crystals;

        // Повышаем уровень
        currentLevel++;

        // Смена картинки врага
        if (enemyImage != null && enemySprites.Length > 0)
        {
            // Берем картинку по кругу (если уровень больше, чем картинок, начинаем сначала)
            int spriteIndex = (currentLevel - 1) % enemySprites.Length;
            enemyImage.sprite = enemySprites[spriteIndex];
        }

        // Делаем следующего врага сильнее
        maxEnemyHealth = baseEnemyHealth * currentLevel;
        currentEnemyHealth = maxEnemyHealth;

        Debug.Log($"Уровень {currentLevel}! Новый враг с HP: {maxEnemyHealth}");
        UpdateUI();
    }

    // Метод для кнопки улучшения
    public void BuyDamageUpgrade()
    {
        if (crystals >= 50)
        {
            crystals -= 50;
            playerDamage += 5;
            if (crystalsText != null) crystalsText.text = "Кристаллы: " + crystals;

            // ВКЛЮЧАЕМ И УВЕЛИЧИВАЕМ ОГОНЬ
            if (fireAura != null)
            {
                fireAura.SetActive(true);

                // Берем текущий размер и умножаем на 1.15 (плюс 15%)
                float newScaleX = fireAura.transform.localScale.x * 1.15f;
                float newScaleY = fireAura.transform.localScale.y * 1.15f;

                // Ограничиваем максимальный размер (например, не больше 2.0, чтобы не закрыть доску)
                if (newScaleX > 2.0f) newScaleX = 2.0f;
                if (newScaleY > 2.0f) newScaleY = 2.0f;

                fireAura.transform.localScale = new Vector3(newScaleX, newScaleY, 1);
            }

            Debug.Log("Урон улучшен! Теперь урон: " + playerDamage);
        }
        else
        {
            Debug.Log("Не хватает кристаллов!");
        }
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Игра окончена!");
        if (gameOverText != null) gameOverText.SetActive(true);
        if (heroineAnimator != null) heroineAnimator.PlayDefeat();
    }

    void Victory()
    {
        isGameOver = true;
        if (victoryText != null) victoryText.SetActive(true);
        if (heroineAnimator != null) heroineAnimator.PlayVictory();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    System.Collections.IEnumerator FlashEnemyDamage()
    {
        if (enemyImage == null) yield break;

        Color originalColor = enemyImage.color;
        enemyImage.color = new Color(1f, 0.3f, 0.3f); // Красный оттенок
        yield return new WaitForSeconds(0.15f);
        enemyImage.color = originalColor; // Возвращаем белый
    }
}