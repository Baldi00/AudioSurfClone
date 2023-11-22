using TMPro;
using UnityEngine;

/// <summary>
/// Moves and updates the color of the ui points
/// </summary>
public class PointsIncrementUiMover : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI pointsIncrementText;
    [SerializeField] float duration = 0.8f;
    [SerializeField] float speed = 5f;

    private float initialX;
    private float direction;
    private Color color;

    private float timer = 0;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Update position
        rectTransform.anchoredPosition =
            new Vector2(initialX + direction * speed * timer, rectTransform.anchoredPosition.y);

        // Update color
        color.a = 1 - timer / duration;
        pointsIncrementText.color = color;

        // After the timer destroy the object
        if (timer >= duration)
            Destroy(gameObject);
    }

    public void Setup(string text, float initialX, int direction, Color color)
    {
        this.initialX = initialX;
        this.direction = direction;
        this.color = color;

        pointsIncrementText.text = text;
        pointsIncrementText.color = color;
    }
}
