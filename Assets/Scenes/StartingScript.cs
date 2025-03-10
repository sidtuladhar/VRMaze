using UnityEngine;

public class StartingScript : MonoBehaviour
{

    public GameObject ball;

    public float spawnInterval = 2f;

    float timer = 0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            Instantiate(ball, new Vector3(Random.Range(-8f, 8f), 6, Random.Range(-8f, 8f)), Quaternion.identity);
            timer = 0;
        }
    }
}
