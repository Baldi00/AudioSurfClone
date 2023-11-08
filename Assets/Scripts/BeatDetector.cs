using System.Collections.Generic;
using UnityEngine;

public class BeatDetector : MonoBehaviour
{
    public static List<int> GetBeatIndexes(double[][] spectrum, int windowSize, AudioClip audioClip, int frequency, float beatThreshold, float skipSecondsIfBeatFound)
    {
        int frequencyIndex = (int)((float)windowSize / (20000 - 20) * frequency);
        var indexes = new List<int>();
        int skipSamplesIfBeatFound = (int)(audioClip.frequency * audioClip.channels * skipSecondsIfBeatFound / windowSize);

        for (int i = 0; i < spectrum.Length - 1; i++)
        {
            double curr = spectrum[i][frequencyIndex];
            double next = spectrum[i + 1][frequencyIndex];
            if (next - curr >= beatThreshold)
            {
                indexes.Add(i);
                i += skipSamplesIfBeatFound - 1;
            }
        }
        return indexes;
    }
}
