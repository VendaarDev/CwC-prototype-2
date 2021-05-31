using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
  public GameObject[] animalPrefabs;
  private float spawnRangeX = 14;
  private float spawnPosZ = 20;

    // Update is called once per frame

    void Start()
    {
      InvokeRepeating("SpawnRandomAnimal", 2, 1.5f);
    }

    void Update()
    {

    }

    void SpawnRandomAnimal()
    {
      int animalIndex = Random.Range(0, animalPrefabs.Length);
      Vector3 spawnPos = new Vector3(Random.Range(-spawnRangeX, spawnRangeX), 0, spawnPosZ);

      Instantiate(animalPrefabs[animalIndex], spawnPos, animalPrefabs[animalIndex].transform.rotation);
    }
}
