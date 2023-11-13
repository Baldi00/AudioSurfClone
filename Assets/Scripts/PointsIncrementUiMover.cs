using TMPro;
using UnityEngine;

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
        rectTransform.anchoredPosition = new Vector2(initialX + direction * speed * timer, rectTransform.anchoredPosition.y);
        color.a = 1 - timer / duration;
        pointsIncrementText.color = color;

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
