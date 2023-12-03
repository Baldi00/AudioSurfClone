using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class BlocksManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;

    [SerializeField] private float maxDistanceFromCenter = 2.2f;
    [SerializeField] private int lowBeatFrequency = 20;
    [SerializeField] private float lowBeatThreshold = 0.1f;
    [SerializeField] private float lowBeatSkip = 0.5f;
    [SerializeField] private int highBeatFrequency = 7500;
    [SerializeField] private float highBeatThreshold = 0.025f;
    [SerializeField] private float highBeatSkip = 0.5f;

    private GameObject blocksContainer;

    private List<Block> blocks;
    private List<BlockData> blocksData;
    private List<Transform> blocksTransforms;

    private TrackData trackData;

    private NativeArray<Vector3> trackSplinePointsNativeArray;
    private NativeArray<BlockData> blocksDataNativeArray;
    private TransformAccessArray blocksTransformsAccessArray;
    private JobHandle updateBlocksPositionJobHandle;
    private UpdateBlockPositionsJob updateBlocksPositionJob;

    void Awake()
    {
        blocks = new List<Block>();
        blocksData = new List<BlockData>();
        blocksTransforms = new List<Transform>();
    }

    void OnDestroy()
    {
        if (trackSplinePointsNativeArray.IsCreated)
            trackSplinePointsNativeArray.Dispose();

        if (blocksDataNativeArray.IsCreated)
            blocksDataNativeArray.Dispose();

        if (blocksTransformsAccessArray.isCreated)
            blocksTransformsAccessArray.Dispose();
    }

    /// <summary>
    /// Spawns the blocks on the track in the beat positions
    /// </summary>
    /// <param name="spectrum">The spectrum of the audio clip</param>
    /// <param name="audioClip">The audioClip containing the audio to reproduce</param>
    /// <param name="trackData">The data generated for the given audioClip</param>
    public void SpawnBlocksOnTrack(float[][] spectrum, AudioClip audioClip, TrackData trackData)
    {
        this.trackData = trackData;
        blocksContainer = new GameObject("Blocks container");

        // Get low and high beat indexes
        List<int> lowBeatIndexes = AudioUtils.GetBeatIndexes(
            spectrum, audioClip.frequency, audioClip.channels,
            lowBeatFrequency, lowBeatThreshold, lowBeatSkip);

        List<int> highBeatIndexes = AudioUtils.GetBeatIndexes(
            spectrum, audioClip.frequency, audioClip.channels,
            highBeatFrequency, highBeatThreshold, highBeatSkip);

        RemoveNearBeats(lowBeatIndexes, highBeatIndexes, 5);

        SpawnBlocks(lowBeatIndexes);
        SpawnBlocks(highBeatIndexes);

        // Prepare the blocks movement job arrays
        trackSplinePointsNativeArray = new NativeArray<Vector3>(trackData.splinePoints, Allocator.Persistent);
        blocksDataNativeArray = new NativeArray<BlockData>(blocksData.ToArray(), Allocator.Persistent);
        blocksTransformsAccessArray = new TransformAccessArray(blocksTransforms.ToArray());
        updateBlocksPositionJob = new UpdateBlockPositionsJob()
        {
            trackSplinePoints = trackSplinePointsNativeArray,
            blocksData = blocksDataNativeArray
        };
    }

    /// <summary>
    /// Resets all blocks initial status
    /// </summary>
    public void ResetAllBlocks()
    {
        foreach (Block block in blocks)
            block.ResetBlock();
    }

    /// <summary>
    /// Removes all blocks from the scene
    /// </summary>
    public void RemoveAllBlocks()
    {
        Destroy(blocksContainer);
        blocks.Clear();
        blocksData.Clear();
        blocksTransforms.Clear();

        if (trackSplinePointsNativeArray.IsCreated)
            trackSplinePointsNativeArray.Dispose();

        if (blocksDataNativeArray.IsCreated)
            blocksDataNativeArray.Dispose();

        if (blocksTransformsAccessArray.isCreated)
            blocksTransformsAccessArray.Dispose();
    }

    /// <summary>
    /// Updates the positions of all blocks in the right position according to the given audio percentage
    /// </summary>
    /// <param name="currentPercentage">The current audio percentage</param>
    public void UpdateBlocksPositions(float currentPercentage)
    {
        updateBlocksPositionJob.currentPercentage = currentPercentage;
        updateBlocksPositionJobHandle = updateBlocksPositionJob.Schedule(blocksTransformsAccessArray);
        updateBlocksPositionJobHandle.Complete();
    }

    /// <summary>
    /// Returns the total number of spawned blocks
    /// </summary>
    /// <returns>The total number of spawned blocks</returns>
    public int GetTotalBlocksCount()
    {
        return blocksData.Count;
    }

    /// <summary>
    /// Checks and removes a beat from the additive list if a beat is inside the range of another one in the base list.
    /// E.g. Base beat 7, additive beat 5, range 3 -> 5 is inside the [4 (7-3), 10 (7+3)] range so it will be removed
    /// </summary>
    /// <param name="baseBeats">Base beats list, will be untouched</param>
    /// <param name="additiveBeats">Additive beats list, some beats may be removed</param>
    /// <param name="range">The range of beats in which to check</param>
    private void RemoveNearBeats(List<int> baseBeats, List<int> additiveBeats, int range)
    {
        var toRemove = new List<int>();
        foreach (int additiveBeat in additiveBeats)
        {
            if (ListContainsInRange(baseBeats, additiveBeat, range))
                toRemove.Add(additiveBeat);
        }
        additiveBeats.RemoveAll(beat => toRemove.Contains(beat));
    }

    /// <summary>
    /// Checks if the list contains the item to check in the given range
    /// E.g. List contains 7, toCkeck 5, range 3 -> 5 is inside the [4 (7-3), 10 (7+3)] range so it return true
    /// </summary>
    /// <param name="list">The list of integer to check if it contains the item</param>
    /// <param name="toCheck">Item to check if it is inside the list (in range)</param>
    /// <param name="range">The range of integers in which to check</param>
    /// <returns>True if the list contains the item between the given range, false otherwise</returns>
    private bool ListContainsInRange(List<int> list, int toCheck, int range)
    {
        for (int i = toCheck - range; i < toCheck + range; i++)
            if (list.Contains(i))
                return true;
        return false;
    }

    /// <summary>
    /// Spawns blocks at the given beat indexes. Uses some deterministic noise to spawn them on left, center or right
    /// </summary>
    /// <param name="beatIndexes">The indexes of beat in which to spawn the blocks</param>
    private void SpawnBlocks(List<int> beatIndexes)
    {
        int previousIndex = beatIndexes[0];
        int noise;
        foreach (int beatIndex in beatIndexes)
        {
            if (beatIndex % 3 - 1 == previousIndex % 3 - 1)
                noise = beatIndex % 2 + 1;
            else
                noise = 0;

            previousIndex = beatIndex + noise;

            // 4 is due to spline correction
            float percentage = (float)(beatIndex + 4) / (trackData.splinePoints.Length - 4);
            float blockSpawnZPosition = ((beatIndex + noise) % 3 - 1) * maxDistanceFromCenter;

            GameObject block = Instantiate(blockPrefab,
                trackData.spline.GetPointAt(percentage) + Vector3.forward * blockSpawnZPosition,
                Quaternion.LookRotation(trackData.spline.GetTangentAt(percentage), Vector3.up),
                blocksContainer.transform);

            BlockPosition blockPosition = BlockPosition.CENTER;
            if (blockSpawnZPosition > Mathf.Epsilon)
                blockPosition = BlockPosition.LEFT;
            else if (blockSpawnZPosition < -Mathf.Epsilon)
                blockPosition = BlockPosition.RIGHT;

            Block blockManager = block.GetComponent<Block>();
            blockManager.Initialize(blockPosition, percentage);

            blocks.Add(blockManager);

            blocksData.Add(blockManager.GetBlockData(maxDistanceFromCenter));

            blocksTransforms.Add(block.transform);
        }
    }
}
