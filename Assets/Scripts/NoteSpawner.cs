using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public Transform[] spawnPoints; // Array de pontos de spawn para as notas
    
    public GameObject notePrefab;

    public float spawnInterval = 1.5f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnNote();
            timer = 0f;
        }
    }

    private void SpawnNote()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("Nenhum ponto de spawn definido!");
            return;
        }
        int randomIndex = Random.Range(0, spawnPoints.Length);
        // Instancia a nota na posição do spawner
        Transform spawnPoint = spawnPoints[randomIndex];
        Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
    }
}