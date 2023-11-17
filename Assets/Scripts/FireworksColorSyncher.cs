using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FireworksColorSyncher : MonoBehaviour, IColorSynchable
{
    [SerializeField] private ColorSyncher colorSyncher;
    [SerializeField] private float colorDelta;

    private new ParticleSystem particleSystem;

    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        colorSyncher.AddColorSynchable(this);
    }

    public void SyncColor(Color color)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        Color.RGBToHSV(color, out float h, out float s, out float v);
        Color min = Color.HSVToRGB(Mathf.Max(h - colorDelta, 0), s - colorDelta, v - colorDelta);
        Color max = Color.HSVToRGB(Mathf.Min(h + colorDelta, 0.83f), s + colorDelta, v + colorDelta);
        main.startColor = new ParticleSystem.MinMaxGradient(min, max);
    }
}
