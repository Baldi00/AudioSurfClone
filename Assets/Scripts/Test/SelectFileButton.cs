using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SelectFileButton : MonoBehaviour
{
    public enum SelectButtonType
    {
        FILE,
        DIRECTORY
    }

    [SerializeField]
    private TextMeshProUGUI innerText;
    [SerializeField]
    private Button button;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private Sprite fileSprite;
    [SerializeField]
    private Sprite directorySprite;

    public void SetButtonType(SelectButtonType type)
    {
        icon.sprite = type == SelectButtonType.FILE ? fileSprite : directorySprite;
    }

    public void SetInnerText(string text)
    {
        innerText.text = text;
    }

    public void AddListener(UnityAction actionCall)
    {
        button.onClick.AddListener(actionCall);
    }
}
