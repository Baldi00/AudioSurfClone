using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private Splines splineVisualizer;
    [SerializeField]
    private Splines splineVisualizer2;

    private int entireSongTimeSamples;
    float[] intensities;
    double[][] frequencies;

    void Start()
    {
        entireSongTimeSamples = audioSource.clip.samples * audioSource.clip.channels;
        float[] audioData = new float[entireSongTimeSamples];
        audioSource.clip.GetData(audioData, 0);

        // Intensity computation
        intensities = new float[entireSongTimeSamples / 4096];

        for (int i = 0; i < intensities.Length; i++)
        {
            intensities[i] = 0;
            for (int j = 0; j < 4096; j++)
                intensities[i] += Mathf.Abs(audioData[i * 4096 + j]);
            intensities[i] /= 4096;
        }

        // Frequencies computation
        double[] audioDataChunk = new double[128];
        System.Numerics.Complex[] audioDataChunkComplex = new System.Numerics.Complex[128];
        System.Numerics.Complex[] spectrumComplex = new System.Numerics.Complex[128];
        double[] spectrum = new double[128];
        frequencies = new double[entireSongTimeSamples / 128][];

        for (int i = 0; i < entireSongTimeSamples; i += 128)
        {
            for (int j = 0; j < 128; j++)
                audioDataChunk[j] = audioData[i + j];

            audioDataChunkComplex = FastFourierTransform.doubleToComplex(audioDataChunk);
            spectrumComplex = FastFourierTransform.FFT(audioDataChunkComplex, false);

            for (int j = 0; j < 128; j++)
                spectrum[j] = spectrumComplex[j].Magnitude;

            frequencies[i / 128] = new double[128];
            System.Array.Copy(spectrum, frequencies[i / 128], 128);
        }

        // Intensity visualization
        Vector3[] intensityPoints = new Vector3[intensities.Length];
        for (int i = 0; i < intensities.Length; i++)
            intensityPoints[i] = new Vector3(300f * i / intensities.Length, intensities[i]*10 - 10);
        splineVisualizer.SetPoints(intensityPoints);

        // Frequency visualization
        int frequency = 20; // 20->20000
        int frequencyIndex = (int)(128f / (20000 - 20) * frequency);
        Vector3[] bassPoints = new Vector3[frequencies.Length];
        for (int i = 0; i < frequencies.Length; i++)
            bassPoints[i] = new Vector3(300f * i / frequencies.Length, (float)frequencies[i][frequencyIndex]);
        splineVisualizer2.SetPoints(bassPoints);
    }

    void Update()
    {
        splineVisualizer.SetCurrentTime(audioSource.time / audioSource.clip.length);
        splineVisualizer2.SetCurrentTime(audioSource.time / audioSource.clip.length);
    }
}
