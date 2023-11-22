using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// At every update syncs the colors on the registered items
/// </summary>
public class ColorSyncher : MonoBehaviour
{
    private List<IColorSynchable> colorSynchables;

    // Cache
    private GameManager gameManager;
    private Color currentColor;

    void Awake()
    {
        gameManager = GameManager.GetGameManager();
    }

    void Update()
    {
        SyncColorSynchables();
    }

    /// <summary>
    /// Adds a color syncable to the list of items to sync
    /// </summary>
    /// <param name="colorSynchable">The item to sync color on</param>
    public void AddColorSynchable(IColorSynchable colorSynchable)
    {
        colorSynchables ??= new List<IColorSynchable>();
        colorSynchables.Add(colorSynchable);
    }

    /// <summary>
    /// Retrive current color from game manager and sync all the registered color synchables
    /// </summary>
    private void SyncColorSynchables()
    {
        currentColor = gameManager.GetCurrentColor();
        foreach (IColorSynchable colorSynchable in colorSynchables)
            colorSynchable.SyncColor(currentColor);
    }
}
