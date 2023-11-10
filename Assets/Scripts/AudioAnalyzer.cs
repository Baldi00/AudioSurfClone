using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class AudioAnalyzer : MonoBehaviour
{
    [BurstCompile]
    private struct FFTCompute : IJob
    {
        public NativeArray<float> audioDataChunk;
        public NativeArray<float> spectrumAmplitudeChunk;
        [ReadOnly] public NativeArray<int> bitReversalPermutation;
        [ReadOnly] public NativeArray<float> blackmanHarrisWindow;
        [ReadOnly] public float2 wn;

        public void Execute()
        {
            int length = audioDataChunk.Length;
            NativeArray<float2> spectrumComplex = new NativeArray<float2>(length, Allocator.Temp);

            for (int i = 0; i < length; i++)
                audioDataChunk[i] *= blackmanHarrisWindow[i];

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

            for (int i = 0; i < length; i++)
                spectrumAmplitudeChunk[i] =
                    math.sqrt(spectrumComplex[i].x * spectrumComplex[i].x + spectrumComplex[i].y * spectrumComplex[i].y);

            spectrumComplex.Dispose();
        }
    }

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

    public static float[][] GetAudioSpectrum(AudioClip audioClip, int windowSize)
    {
        float[] audioData = GetAudioData(audioClip);
        
        // Create audio data chunks
        List<NativeArray<float>> audioDataChunks = new List<NativeArray<float>>();
        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            NativeArray<float> audioDataChunk = new NativeArray<float>(windowSize, Allocator.TempJob);
            for (int j = 0; j < windowSize; j++)
                audioDataChunk[j] = audioData[i * windowSize + j];
            audioDataChunks.Add(audioDataChunk);
        }

        // Create spectrum amplitude chunks
        List<NativeArray<float>> spectrumAmplitudeChunks = new List<NativeArray<float>>();
        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            NativeArray<float> spectrumAmplitudeChunk = new NativeArray<float>(windowSize, Allocator.TempJob);
            spectrumAmplitudeChunks.Add(spectrumAmplitudeChunk);
        }

        // Prepare and run jobs
        float factorEXP = -2.0f * Mathf.PI / windowSize;
        float2 wn = new float2((float)math.cos(factorEXP), (float)math.sin(factorEXP));

        NativeArray<int> bitReversalIndexes = new NativeArray<int>(GetBitReversalPermutation(windowSize), Allocator.TempJob);
        NativeArray<float> blackmanHarrisWindow = new NativeArray<float>(GetBlackmanHarrisWindow(windowSize), Allocator.TempJob);

        NativeList<JobHandle> fftComputeJobs = new NativeList<JobHandle>(Allocator.TempJob);

        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            fftComputeJobs.Add(new FFTCompute()
            {
                audioDataChunk = audioDataChunks[i],
                spectrumAmplitudeChunk = spectrumAmplitudeChunks[i],
                bitReversalPermutation = bitReversalIndexes,
                blackmanHarrisWindow = blackmanHarrisWindow,
                wn = wn
            }.Schedule());
        }

        JobHandle.CompleteAll(fftComputeJobs);

        // Convert results
        float[][] spectrumAmplitudes = new float[audioData.Length / windowSize][];

        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            spectrumAmplitudes[i] = new float[windowSize];
            for (int j = 0; j < windowSize; j++)
                spectrumAmplitudes[i][j] = spectrumAmplitudeChunks[i][j];
        }

        // Dispose native arrays and lists
        audioDataChunks.ForEach(adc => adc.Dispose());
        spectrumAmplitudeChunks.ForEach(sac => sac.Dispose());
        bitReversalIndexes.Dispose();
        blackmanHarrisWindow.Dispose();
        fftComputeJobs.Dispose();

        // Return result
        return spectrumAmplitudes;
    }

    private static int[] GetBitReversalPermutation(int length)
    {
        int[] bitReversedIndices = new int[length];
        int bits = (int)Math.Log(length, 2);

        for (int i = 0; i < length; i++)
        {
            int reversed = 0;
            for (int j = 0; j < bits; j++)
            {
                reversed = (reversed << 1) | ((i >> j) & 1);
            }
            bitReversedIndices[i] = reversed;
        }

        return bitReversedIndices;
    }

    private static float[] GetBlackmanHarrisWindow(int length)
    {
        float[] blackmanHarrisWindow = new float[length];
        for (int n = 0; n < length; n++)
        {
            float w =
                0.35875f - 0.48829f * Mathf.Cos(2.0f * Mathf.PI * n / (length - 1)) +
                0.14128f * Mathf.Cos(4.0f * Mathf.PI * n / (length - 1));
            blackmanHarrisWindow[n] = w;
        }

        return blackmanHarrisWindow;
    }
}
