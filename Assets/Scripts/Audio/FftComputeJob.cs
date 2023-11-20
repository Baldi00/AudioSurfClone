using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FftComputeJob : IJob
{
    public NativeArray<float> audioDataChunk;
    public NativeArray<float> spectrumAmplitudeChunk;
    [ReadOnly] public NativeArray<int> bitReversalPermutation;
    [ReadOnly] public NativeArray<float> blackmanHarrisWindow;
    [ReadOnly] public float2 wn;

    /// <summary>
    /// Computes fft on the given audio data chunk and puts the spectrum amplitudes in the corresponding variable
    /// </summary>
    public void Execute()
    {
        int length = audioDataChunk.Length;
        NativeArray<float2> spectrumComplex = new NativeArray<float2>(length, Allocator.Temp);

        // Apply Blackman-Harris window
        for (int i = 0; i < length; i++)
            audioDataChunk[i] *= blackmanHarrisWindow[i];

        // Prepare positions for the Cooley-Tukey iteration
        for (int i = 0; i < length; i++)
            spectrumComplex[i] = audioDataChunk[bitReversalPermutation[i]];

        // Cooley-Tukey iteration
        for (int size = 2; size <= length; size *= 2)
        {
            for (int j = 0; j < length; j += size)
            {
                float2 w = new float2(1, 0);
                for (int k = 0; k < size / 2; k++)
                {
                    float2 t = w * spectrumComplex[j + k + size / 2];
                    float2 u = spectrumComplex[j + k];
                    spectrumComplex[j + k] = u + t;
                    spectrumComplex[j + k + size / 2] = u - t;
                    w *= wn;
                }
            }
        }

        // Compute amplitudes from complex spectrum data
        for (int i = 0; i < length; i++)
            spectrumAmplitudeChunk[i] =
                math.sqrt(spectrumComplex[i].x * spectrumComplex[i].x + spectrumComplex[i].y * spectrumComplex[i].y);

        spectrumComplex.Dispose();
    }
}