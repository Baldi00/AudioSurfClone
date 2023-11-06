using System.Collections.Generic;
using UnityEngine;

public class PlayerColorSyncher : MonoBehaviour, IColorSynchable
{
    [SerializeField]
    private MeshRenderer spaceshipRenderer;
    [SerializeField]
    private List<int> materialsToUpdateIndexes;

    // Cache
    private List<Material> spaceshipMaterials;

    void Awake()
    {
        spaceshipMaterials = new List<Material>();
        spaceshipRenderer.GetMaterials(spaceshipMaterials);
    }

    public void SyncColor(Color color)
    {
        for (int i = 0; i < spaceshipMaterials.Count; i++)
            if (materialsToUpdateIndexes.Contains(i))
                spaceshipMaterials[i].SetColor("_EmissionColor", color);
    }
}
