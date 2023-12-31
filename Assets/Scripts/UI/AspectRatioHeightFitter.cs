using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]

public class AspectRatioHeightFitter : UIBehaviour, ILayoutSelfController
{
    private RectTransform rectTransform = null;
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
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
        SetLayoutVertical();
    }

    void Update()
    {
        SetLayoutVertical();
    }

    public void SetLayoutVertical()
    {
        if (rectTransform == null)
            return;

        float aspectRatio = (float)Screen.width / Screen.height;
        float height = Mathf.Lerp(minHeight, maxHeight, Mathf.InverseLerp(maxScreenRatio, minScreenRatio, aspectRatio));
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public void SetLayoutHorizontal()
    {
        if (rectTransform == null)
            return;
    }
}
