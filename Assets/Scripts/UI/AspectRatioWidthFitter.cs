using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]

public class AspectRatioWidthFitter : UIBehaviour, ILayoutSelfController
{
    private RectTransform rectTransform = null;
    [SerializeField] private float minWidth;
    [SerializeField] private float maxWidth;
    [SerializeField] private float minScreenRatio;
    [SerializeField] private float maxScreenRatio;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = transform as RectTransform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetLayoutHorizontal();
    }

    public void SetLayoutVertical()
    {
        if (rectTransform == null)
            return;
    }

    public void SetLayoutHorizontal()
    {
        if (rectTransform == null)
            return;

        float aspectRatio = (float)Screen.width / Screen.height;
        float width = Mathf.Lerp(minWidth, maxWidth, Mathf.InverseLerp(maxScreenRatio, minScreenRatio, aspectRatio));
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
}
