using System.Collections.Generic;
using UnityEngine;

public class FireworksManager : MonoBehaviour
{
    [SerializeField] private List<ParticleSystem> leftFireworks;
    [SerializeField] private List<ParticleSystem> centerFireworks;
    [SerializeField] private List<ParticleSystem> rightFireworks;

    [SerializeField] private float fireworksDurationPortrait;
    [SerializeField] private Vector2 startSizePortrait;
    [SerializeField] private float fireworksDurationLandscape;
    [SerializeField] private Vector2 startSizeLandscape;

    void Awake()
    {
        var particleSystems = new List<ParticleSystem>();
        particleSystems.AddRange(leftFireworks);
        particleSystems.AddRange(centerFireworks);
        particleSystems.AddRange(rightFireworks);

        var fireworksDuration = Screen.width > Screen.height ? fireworksDurationLandscape : fireworksDurationPortrait;
        var startSize = Screen.width > Screen.height ? startSizeLandscape : startSizePortrait;

        foreach (var particleSystem in particleSystems)
        {
            var mainModule = particleSystem.main;
            mainModule.duration = fireworksDuration;
            mainModule.startLifetime = fireworksDuration;
            var size = mainModule.startSize;
            size.constantMin = startSize.x;
            size.constantMax = startSize.y;
            mainModule.startSize = size;
        }
    }

    /// <summary>
    /// Emits fireworks at the given block position
    /// </summary>
    /// <param name="blockPosition">The block position corresponding to the position of the fireworks</param>
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
