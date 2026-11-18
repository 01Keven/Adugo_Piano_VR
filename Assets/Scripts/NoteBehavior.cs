using UnityEngine;

public class NoteBehavior : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer[] noteRenderers;
    private MaterialPropertyBlock colorProperties;

    private void Awake()
    {
        noteRenderers = GetComponentsInChildren<Renderer>(true);
        colorProperties = new MaterialPropertyBlock();
    }

    [Tooltip("velocidade com que a nota se move para o jogador")]
    public float moveSpeed = 5f;

    public Vector3 moveDirection = new Vector3(0, -1, 0); // direção para baixo
    // Update is called once per frame
    void Update()
    {
        // move a nota para baixo em direção ao jogador
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);
    }
    public void ChangeColorToRed()
    {
        ChangeColor(Color.red);
    }

    public void ChangeColorToGreen()
    {
        ChangeColor(Color.green);
    }

    private void ChangeColor(Color color)
    {
        foreach (Renderer noteRenderer in noteRenderers)
        {
            noteRenderer.GetPropertyBlock(colorProperties);
            colorProperties.SetColor(BaseColorId, color);
            colorProperties.SetColor(ColorId, color);
            noteRenderer.SetPropertyBlock(colorProperties);
        }
    }
}
