using UnityEngine;

public class AudioAnalyzer : MonoBehaviour
{
    public static float[] GetAudioData(AudioClip audioClip)
    {
        int sampleCount = audioClip.samples * audioClip.channels;
        float[] audioData = new float[sampleCount];
        audioClip.GetData(audioData, 0);
        return audioData;
    }

    public static float[] GetAudioIntensities(AudioClip audioClip, int windowsSize)
    {
        float[] audioData = GetAudioData(audioClip);
        float[] intensities = new float[audioData.Length / windowsSize];

        for (int i = 0; i < intensities.Length; i++)
        {
            intensities[i] = 0;
            for (int j = 0; j < windowsSize; j++)
                intensities[i] += Mathf.Abs(audioData[i * windowsSize + j]);
            intensities[i] /= windowsSize;
        }

        return intensities;
    }

    public static double[][] GetAudioSpectrum(AudioClip audioClip, int windowSize)
    {
        float[] audioData = GetAudioData(audioClip);
        double[][] spectrum = new double[audioData.Length / windowSize][];

        double[] audioDataChunk = new double[windowSize];
        double[] spectrumChunk = new double[windowSize];
        System.Numerics.Complex[] spectrumChunkComplex;

        for (int i = 0; i < spectrum.Length; i++)
        {
            for (int j = 0; j < windowSize; j++)
                audioDataChunk[j] = audioData[i * windowSize + j];

            spectrumChunkComplex = FastFourierTransform.FFT(audioDataChunk, false);

            for (int j = 0; j < windowSize; j++)
                spectrumChunk[j] = spectrumChunkComplex[j].Magnitude;

            spectrum[i] = new double[windowSize];
            System.Array.Copy(spectrumChunk, spectrum[i], windowSize);
        }

        return spectrum;
    }
}
