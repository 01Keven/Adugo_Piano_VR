using UnityEngine;

public class NoteBehavior : MonoBehaviour
{
    [Tooltip("velocidade com que a nota se move para o jogador")]
    public float moveSpeed = 5f;

    public Vector3 moveDirection = new Vector3(0, -1, 0); // direção para baixo
    // Update is called once per frame
    void Update()
    {
        // move a nota para baixo em direção ao jogador
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);
    }
}
