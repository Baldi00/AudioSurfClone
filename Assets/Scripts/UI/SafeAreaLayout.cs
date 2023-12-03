using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]

public class SafeAreaLayout : UIBehaviour, ILayoutSelfController
{
    private RectTransform rectTransform = null;
    private Rect safeArea;
    private int screenWidth;
    private int screenHeight;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = transform as RectTransform;
        safeArea = Screen.safeArea;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetLayoutHorizontal();
        SetLayoutVertical();
    }

    public void SetLayoutHorizontal()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(safeArea.x / screenWidth, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2((safeArea.x + safeArea.width) / screenWidth, rectTransform.anchorMax.y);
    }

    public void SetLayoutVertical()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, safeArea.y / screenHeight);
        rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, (safeArea.y + safeArea.height) / screenHeight);
    }
}
