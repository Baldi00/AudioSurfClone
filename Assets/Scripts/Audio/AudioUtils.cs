using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;

public class AudioUtils : MonoBehaviour
{
    /// <summary>
    /// Returns the audio data of the given audioClip (i.e. all the samples of the audio file).
    /// The length of the audio data array is audioClip.samples * audioClip.channels
    /// </summary>
    /// <param name="audioClip">The audioClip you want the audio data of</param>
    /// <returns>The audio data of the given audioClip</returns>
    public static float[] GetAudioData(AudioClip audioClip)
    {
        int sampleCount = audioClip.samples * audioClip.channels;
        float[] audioData = new float[sampleCount];
        audioClip.GetData(audioData, 0);
        return audioData;
    }

    /// <summary>
    /// Returns the average intensity (i.e. volume) for each chunk of the audio file inside the given audioClip
    /// </summary>
    /// <param name="audioClip">The audioClip you want the intensities of</param>
    /// <param name="windowsSize">The size of each chunk</param>
    /// <returns>The average intensity for each chunk of the audio file</returns>
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

    /// <summary>
    /// Returns the spectrum amplitudes for each chunk of the audio file inside the given audioClip.<br>
    /// I.e. The average amplitudes of the sinusoids at each frequency from 20Hz to 20000Hz of each chunk.<br>
    /// E.g. Window size: 128, Audio data: 1024 samples, the first element of the result will contain an array of 128
    /// elements, each with the average amplitudes for each frequency. The first element of this array is the average
    /// amplitudes for frequency from 20Hz to 176Hz (20 + (20000-20)/128).
    /// The final result will be a matrix of 8x128 elements (Samples/WindowSize)x(WindowSize)
    /// </summary>
    /// <param name="audioClip">The audioClip you want the spectrum amplitudes of</param>
    /// <param name="windowSize">The size of audio chunks and the number of amplitude averages for each chunk</param>
    /// <returns>The spectrum amplitudes for each chunk of the audio file</returns>
    public static float[][] GetAudioSpectrumAmplitudes(AudioClip audioClip, int windowSize)
    {
        var audioData = GetAudioData(audioClip);

        // Create audio data chunks
        var audioDataChunks = new List<NativeArray<float>>();
        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            var audioDataChunk = new NativeArray<float>(windowSize, Allocator.TempJob);
            for (int j = 0; j < windowSize; j++)
                audioDataChunk[j] = audioData[i * windowSize + j];
            audioDataChunks.Add(audioDataChunk);
        }

        // Create spectrum amplitude chunks
        var spectrumAmplitudeChunks = new List<NativeArray<float>>();
        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            var spectrumAmplitudeChunk = new NativeArray<float>(windowSize, Allocator.TempJob);
            spectrumAmplitudeChunks.Add(spectrumAmplitudeChunk);
        }

        // Prepare jobs parameters
        float factorEXP = -2.0f * Mathf.PI / windowSize;
        var wn = new float2(math.cos(factorEXP), math.sin(factorEXP));

        var bitReversalIndexes = new NativeArray<int>(GetBitReversalPermutation(windowSize), Allocator.TempJob);
        var blackmanHarrisWindow = new NativeArray<float>(GetBlackmanHarrisWindow(windowSize), Allocator.TempJob);

        // Schedule and run all jobs
        var fftComputeJobs = new NativeList<JobHandle>(Allocator.TempJob);

        for (int i = 0; i < audioData.Length / windowSize; i++)
        {
            fftComputeJobs.Add(new FftComputeJob()
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
        var spectrumAmplitudes = new float[audioData.Length / windowSize][];

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

    /// <summary>
    /// Loads an audio file located at a given path inside the given audioSource
    /// </summary>
    /// <param name="audioPath">The path of the audioFile to load</param>
    /// <param name="audioSource">The audioSource in which the audio will be loaded</param>
    public static IEnumerator LoadAudio(string audioPath, AudioSource audioSource)
    {
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.MPEG);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.Log("Failed to load MP3: " + request.error);
        else
            audioSource.clip = DownloadHandlerAudioClip.GetContent(request);
    }

    /// <summary>
    /// Returns a list of indexes at which a beat is detected
    /// </summary>
    /// <param name="spectrumAmplitudes">The spectrum amplitudes of the audio file</param>
    /// <param name="sampleFrequency">The sample frequency of the original audio file</param>
    /// <param name="audioChannels">The number of audio channels of the original audio file</param>
    /// <param name="frequency">The frequency at which you want to detect beats on</param>
    /// <param name="beatThreshold">The minimum amplitude difference from previous to next chunck to detect a beat</param>
    /// <param name="skipSecondsIfBeatFound">The amount of seconds to skip analysing if a beat has been found (use this to avoid too many near beats)</param>
    /// <returns>A list of indexes at which a beat is detected</returns>
    public static List<int> GetBeatIndexes(float[][] spectrumAmplitudes, int sampleFrequency, int audioChannels,
                                           int frequency, float beatThreshold, float skipSecondsIfBeatFound)
    {
        int windowSize = spectrumAmplitudes[0].Length;
        int frequencyIndex = (int)((float)windowSize / (20000 - 20) * frequency);
        var indexes = new List<int>();
        int skipSamplesIfBeatFound = (int)(sampleFrequency * audioChannels * skipSecondsIfBeatFound / windowSize);

        float max = float.MinValue;
        for (int i = 0; i < spectrumAmplitudes.Length - 1; i++)
            if (spectrumAmplitudes[i][frequencyIndex] > max)
                max = spectrumAmplitudes[i][frequencyIndex];

        for (int i = 0; i < spectrumAmplitudes.Length - 1; i++)
        {
            float curr = spectrumAmplitudes[i][frequencyIndex] / max;
            float next = spectrumAmplitudes[i + 1][frequencyIndex] / max;
            if (next - curr >= beatThreshold)
            {
                indexes.Add(i);
                i += Mathf.Max(skipSamplesIfBeatFound, 1) - 1;
            }
        }

        return indexes;
    }

    /// <summary>
    /// Returns the bit reversal permutation needed by the Cooley-Tukey algorithm for a chunk of the given length
    /// </summary>
    /// <param name="length">The length of the bit reversal permutation</param>
    /// <returns>Bit reversal permutation needed by the Cooley-Tukey algorithm</returns>
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

    /// <summary>
    /// Returns a Blackman-Harris window for a chunk of the given length
    /// </summary>
    /// <param name="length">The length of the Blackman-Harris window</param>
    /// <returns>Blackman-Harris window for a chunk of the given length</returns>
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
