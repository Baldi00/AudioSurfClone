using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public enum BlockPosition
    {
        LEFT,
        CENTER,
        RIGHT
    }

    [SerializeField] private new Renderer renderer;
    [SerializeField] private Collider myCollider;
    [SerializeField] private float goingUpAnimationSpeed;
    [SerializeField] private float goingUpAnimationDuration;

    private Transform playerTransform;

    private float timer;
    private float xOffset;
    private float yOffset;

    public BlockPosition Position { get; private set; }

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        enabled = false;
    }

    void Update()
    {
        timer += Time.deltaTime;

        renderer.transform.position = new Vector3(
            playerTransform.position.x + xOffset,
            playerTransform.position.y + yOffset + goingUpAnimationSpeed * timer,
            renderer.transform.position.z);

        renderer.material.SetFloat("_Alpha", Mathf.InverseLerp(goingUpAnimationDuration, 0, timer));

        if (timer >= goingUpAnimationDuration)
            gameObject.SetActive(false);
    }

    public void Initialize(BlockPosition blockPosition)
    {
        Position = blockPosition;
    }

    public void Pick()
    {
        enabled = true;
        myCollider.enabled = false;
        xOffset = renderer.transform.position.x - playerTransform.position.x;
        yOffset = renderer.transform.position.y - playerTransform.position.y;
    }
}
