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

    public void SpawnBlocksOnTrack(float[][] spectrum, AudioClip audioClip, TrackData trackData)
    {
        this.trackData = trackData;

        blocksContainer = new GameObject("Blocks container");

        List<int> lowBeatIndexes = AudioUtils.GetBeatIndexes(spectrum, audioClip.frequency, audioClip.channels, lowBeatFrequency, lowBeatThreshold, lowBeatSkip);
        List<int> highBeatIndexes = AudioUtils.GetBeatIndexes(spectrum, audioClip.frequency, audioClip.channels, highBeatFrequency, highBeatThreshold, highBeatSkip);

        RemoveNearBeats(lowBeatIndexes, highBeatIndexes, 5);

        int spawnLocationNoise = 0;
        SpawnBlocks(lowBeatIndexes, ref spawnLocationNoise);
        SpawnBlocks(highBeatIndexes, ref spawnLocationNoise);

        trackSplinePointsNativeArray = new NativeArray<Vector3>(trackData.splinePoints, Allocator.Persistent);
        blocksDataNativeArray = new NativeArray<BlockData>(blocksData.ToArray(), Allocator.Persistent);
        blocksTransformsAccessArray = new TransformAccessArray(blocksTransforms.ToArray());
        updateBlocksPositionJob = new UpdateBlockPositionsJob()
        {
            trackSplinePoints = trackSplinePointsNativeArray,
            blocksData = blocksDataNativeArray
        };
    }

    public void ResetAllBlocks()
    {
        foreach (Block block in blocks)
            block.ResetBlock();
    }

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

    public void UpdateBlocksPositions(float currentPercentage)
    {
        updateBlocksPositionJob.currentPercentage = currentPercentage;
        updateBlocksPositionJobHandle = updateBlocksPositionJob.Schedule(blocksTransformsAccessArray);
        updateBlocksPositionJobHandle.Complete();
    }

    public int GetTotalBlocksCount()
    {
        return blocksData.Count;
    }

    private void RemoveNearBeats(List<int> baseBeats, List<int> additiveBeats, int range)
    {
        List<int> toRemove = new List<int>();
        foreach (int additiveBeat in additiveBeats)
        {
            if (ListContainsInRange(baseBeats, additiveBeat, range))
                toRemove.Add(additiveBeat);
        }
        additiveBeats.RemoveAll(beat => toRemove.Contains(beat));
    }

    private bool ListContainsInRange(List<int> list, int toCheck, int range)
    {
        for (int i = toCheck - range; i < toCheck + range; i++)
            if (list.Contains(i))
                return true;
        return false;
    }

    private void SpawnBlocks(List<int> beatIndexes, ref int spawnLocationNoise)
    {
        foreach (int beatIndex in beatIndexes)
        {
            // 4 is due to spline correction
            float percentage = (float)(beatIndex + 4) / (trackData.splinePoints.Length - 4);
            float blockSpawnZPosition = ((beatIndex + spawnLocationNoise) % 3 - 1) * maxDistanceFromCenter;

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

            spawnLocationNoise++;
        }
    }
}
