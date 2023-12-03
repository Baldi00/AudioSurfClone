using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]

public class SafeAreaBackgroundUp : UIBehaviour, ILayoutSelfController
{
    private RectTransform rectTransform = null;
    private Rect safeArea;
    private int screenHeight;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = transform as RectTransform;
        safeArea = Screen.safeArea;
        screenHeight = Screen.height;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetLayoutVertical();
    }

    public void SetLayoutVertical()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, (safeArea.y + safeArea.height) / screenHeight);
        rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, 1);
    }

    public void SetLayoutHorizontal()
    {
        if (rectTransform == null)
            return;
    }
}
