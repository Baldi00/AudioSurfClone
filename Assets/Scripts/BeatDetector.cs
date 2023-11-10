using System.Collections.Generic;
using UnityEngine;

public class BeatDetector : MonoBehaviour
{
    public static List<int> GetBeatIndexes(float[][] spectrum, int windowSize, AudioClip audioClip, int frequency, float beatThreshold, float skipSecondsIfBeatFound)
    {
        int frequencyIndex = (int)((float)windowSize / (20000 - 20) * frequency);
        var indexes = new List<int>();
        int skipSamplesIfBeatFound = (int)(audioClip.frequency * audioClip.channels * skipSecondsIfBeatFound / windowSize);

        float max = float.MinValue;
        for (int i = 0; i < spectrum.Length - 1; i++)
            if (spectrum[i][frequencyIndex] > max)
                max = spectrum[i][frequencyIndex];

        for (int i = 0; i < spectrum.Length - 1; i++)
        {
            float curr = spectrum[i][frequencyIndex] / max;
            float next = spectrum[i + 1][frequencyIndex] / max;
            if (next - curr >= beatThreshold)
            {
                indexes.Add(i);
                i += Mathf.Max(skipSamplesIfBeatFound, 1) - 1;
            }
        }
        return indexes;
    }
}
