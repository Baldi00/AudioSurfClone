using System.Collections.Generic;
using UnityEngine;

public class FireworksManager : MonoBehaviour
{
    [SerializeField] private List<ParticleSystem> leftFireworks;
    [SerializeField] private List<ParticleSystem> centerFireworks;
    [SerializeField] private List<ParticleSystem> rightFireworks;

    public void EmitFireworks(BlockPosition blockPosition)
    {
        List<ParticleSystem> currentParticleSystem = blockPosition switch
        {
            BlockPosition.LEFT => leftFireworks,
            BlockPosition.CENTER => centerFireworks,
            BlockPosition.RIGHT => rightFireworks,
            _ => null,
        };

        for (int i = 0; i < currentParticleSystem.Count; i++)
            currentParticleSystem[i].Play();
    }
}
