using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]

public class AspectRatioHeightFitter : UIBehaviour, ILayoutSelfController
{
    private RectTransform rectTransform = null;
    [SerializeField] private float defaultHeight;
    [SerializeField] private float defaultScreenRatio;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = transform as RectTransform;
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

        float aspectRatio = (float)Screen.width / Screen.height;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, defaultScreenRatio / aspectRatio * defaultHeight);
    }

    public void SetLayoutHorizontal()
    {
        if (rectTransform == null)
            return;
    }
}
