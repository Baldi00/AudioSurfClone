using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FireworksColorSyncher : MonoBehaviour, IColorSynchable
{
    [SerializeField] private ColorSyncher colorSyncher;

    private new ParticleSystem particleSystem;

    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        colorSyncher.AddColorSynchable(this);
    }

    public void SyncColor(Color color)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.startColor = color;
    }
}
