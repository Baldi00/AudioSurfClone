using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SelectFileButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI innerText;
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite fileSprite;
    [SerializeField] private Sprite directorySprite;

    /// <summary>
    /// Initializes a select file button
    /// </summary>
    /// <param name="type">Is the this button a directory or a audio file?</param>
    /// <param name="innerText">The text of the button</param>
    /// <param name="onClickCallback">The callback to call when the button gets clicked</param>
    public void InitializeButton(SelectButtonType type, string innerText, UnityAction onClickCallback)
    {
        icon.sprite = type == SelectButtonType.FILE ? fileSprite : directorySprite;
        this.innerText.text = innerText;
        button.onClick.AddListener(onClickCallback);
    }
}
