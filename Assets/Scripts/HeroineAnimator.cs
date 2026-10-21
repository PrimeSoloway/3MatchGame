using UnityEngine;
using System.Collections;

public class HeroineAnimator : MonoBehaviour
{
    [Header("Настройки покачивания (Idle)")]
    public float swaySpeed = 1.5f; // Скорость покачивания
    public float swayAngle = 2f;   // Угол наклона (в градусах)

    [Header("Настройки реакций")]
    public float reactionSpeed = 0.5f; // Длительность анимации

    private RectTransform rectTransform;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool isReacting = false; // Флаг, чтобы покачивание не мешало реакции

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalRotation = rectTransform.localRotation;
        originalScale = rectTransform.localScale;
    }

    void Update()
    {
        // Если сейчас играет анимация победы/поражения, не качаемся
        if (!isReacting)
        {
            // Формула синуса создает плавное покачивание влево-вправо
            float angle = Mathf.Sin(Time.time * swaySpeed) * swayAngle;
            rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
        }
    }

    // Вызывается при победе
    public void PlayVictory()
    {
        if (!isReacting) StartCoroutine(VictoryRoutine());
    }

    // Вызывается при поражении
    public void PlayDefeat()
    {
        if (!isReacting) StartCoroutine(DefeatRoutine());
    }

    IEnumerator VictoryRoutine()
    {
        isReacting = true;
        // Прыжок (увеличение и уменьшение)
        for (float t = 0; t < 1; t += Time.deltaTime / reactionSpeed)
        {
            float scale = 1 + Mathf.Sin(t * Mathf.PI) * 0.15f; // Увеличиваем до 15%
            rectTransform.localScale = originalScale * scale;
            yield return null;
        }
        rectTransform.localScale = originalScale;
        isReacting = false;
    }

    IEnumerator DefeatRoutine()
    {
        isReacting = true;
        float targetAngle = 15f; // Насколько сильно поникнуть
        Quaternion targetRotation = originalRotation * Quaternion.Euler(0, 0, -targetAngle);

        // Плавно опускаем голову
        for (float t = 0; t < 1; t += Time.deltaTime / reactionSpeed)
        {
            rectTransform.localRotation = Quaternion.Slerp(originalRotation, targetRotation, t);
            yield return null;
        }

        // Ждем секунду, чтобы игрок увидел грусть, и возвращаем обратно (опционально)
        yield return new WaitForSeconds(1f);
        for (float t = 0; t < 1; t += Time.deltaTime / reactionSpeed)
        {
            rectTransform.localRotation = Quaternion.Slerp(targetRotation, originalRotation, t);
            yield return null;
        }
        isReacting = false;
    }
}