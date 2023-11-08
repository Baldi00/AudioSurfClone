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
        colorSynchables = new List<IColorSynchable>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        currentColor = gameManager.GetCurrentColor();
        colorSynchables.ForEach(cs => cs.SyncColor(currentColor));
    }

    public void AddColorSynchable(IColorSynchable colorSynchable)
    {
        colorSynchables.Add(colorSynchable);
    }

    public void RemoveColorSynchables()
    {
        colorSynchables.Clear();
    }
}
