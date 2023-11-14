using System.Collections.Generic;
using UnityEngine;

public class ColorSyncher : MonoBehaviour
{
    private List<IColorSynchable> colorSynchables;

    // Cache
    private GameManager gameManager;
    private Color currentColor;

    void Awake()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        currentColor = gameManager.GetCurrentColor();
        for (int i = 0; i < colorSynchables.Count; i++)
            colorSynchables[i].SyncColor(currentColor);
    }

    public void AddColorSynchable(IColorSynchable colorSynchable)
    {
        colorSynchables ??= new List<IColorSynchable>();
        colorSynchables.Add(colorSynchable);
    }

    public void RemoveColorSynchables()
    {
        colorSynchables.Clear();
    }
}
