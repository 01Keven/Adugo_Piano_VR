using UnityEngine;

public class NoteBehavior : MonoBehaviour
{
    [Tooltip("velocidade com que a nota se move para o jogador")]
    public float moveSpeed = 5f;
    // Update is called once per frame
    void Update()
    {
        // move a nota para baixo em direção ao jogador
        transform.position -= new Vector3(0, moveSpeed * Time.deltaTime, 0);
    }
}
