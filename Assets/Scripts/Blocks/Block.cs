using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private new Renderer renderer;
    [SerializeField] private Collider myCollider;
    [SerializeField] private float goingUpAnimationSpeed;
    [SerializeField] private float goingUpAnimationDuration;

    [SerializeField] private Material defaultMaterial;

    private Transform playerTransform;

    private float endPercentage;

    private float timer;
    private float xOffset;
    private float yOffset;

    private Vector3 startRendererPosition;

    private bool isPicked;

    public BlockPosition Position { get; private set; }

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        startRendererPosition = renderer.transform.localPosition;
        ResetBlock();
    }

    void Update()
    {
        if (!isPicked) return;

        DoPickAnimation();
    }

    /// <summary>
    /// Initializes block data
    /// </summary>
    /// <param name="blockPosition">The block position on the track</param>
    /// <param name="endPercentage">The final percentage of the track in which the block is located at</param>
    public void Initialize(BlockPosition blockPosition, float endPercentage)
    {
        Position = blockPosition;
        this.endPercentage = endPercentage;
    }

    /// <summary>
    /// Starts the pick animation on the block and disables the collider
    /// </summary>
    public void Pick()
    {
        enabled = true;
        isPicked = true;
        xOffset = renderer.transform.position.x - playerTransform.position.x;
        yOffset = renderer.transform.position.y - playerTransform.position.y;
        DisableCollider();
    }

    /// <summary>
    /// Disables the block collider
    /// </summary>
    public void DisableCollider()
    {
        myCollider.enabled = false;
    }

    /// <summary>
    /// Resets the initial status of the block
    /// </summary>
    public void ResetBlock()
    {
        renderer.sharedMaterial = defaultMaterial;
        renderer.transform.localPosition = startRendererPosition;
        myCollider.enabled = true;
        gameObject.SetActive(true);
        timer = 0;
        enabled = false;
        isPicked = false;
    }

    /// <summary>
    /// Returns the block data of the current block
    /// </summary>
    /// <param name="maxDistanceFromCenter">The maximum distance from the center on the track</param>
    /// <returns>The block data of the current block</returns>
    public BlockData GetBlockData(float maxDistanceFromCenter)
    {
        int position = 0;
        if (Position == BlockPosition.RIGHT)
            position = -1;
        else if (Position == BlockPosition.LEFT)
            position = 1;

        return new BlockData
        {
            zPosition = position * maxDistanceFromCenter,
            endPercentage = endPercentage
        };
    }

    /// <summary>
    /// Animates the block renderer when block is picked
    /// </summary>
    private void DoPickAnimation()
    {
        timer += Time.deltaTime;

        // Position transition
        renderer.transform.position = new Vector3(
            playerTransform.position.x + xOffset,
            playerTransform.position.y + yOffset + goingUpAnimationSpeed * timer,
            renderer.transform.position.z);

        // Transparency animation
        renderer.material.SetFloat("_Alpha", Mathf.InverseLerp(goingUpAnimationDuration, 0, timer));

        if (timer >= goingUpAnimationDuration)
            gameObject.SetActive(false);
    }
}
