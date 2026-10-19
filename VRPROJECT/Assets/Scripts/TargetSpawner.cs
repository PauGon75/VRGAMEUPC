using UnityEngine;
using System.Collections.Generic;

public class TargetSpawner : MonoBehaviour
{

    [SerializeField] GameObject targetPrefab;
    [SerializeField] Vector3 area = new Vector3(5, 5, 0);
    [SerializeField] List<GameObject> targets = new List<GameObject>();
    [SerializeField] int targetsToSpawn;
    [SerializeField] LayerMask spawnBlockingLayer;
    int targetsRemaining;
    float spawnRadius;

    int spawnedCount = 0;

    void Start()
    {
        spawnRadius = targetPrefab.transform.localScale.x / 2;
        SpawnTargets();
    }

    void Update()
    {
        if (targetsRemaining <= 0) 
        {
            SpawnTargets();
        }
    }

    void SpawnTargets() 
    {
        targetsRemaining = targetsToSpawn;
        while (spawnedCount < targetsRemaining)
        {

            Vector3 spawnPos = new Vector3(Random.Range(0, area.x), Random.Range(0, area.y), Random.Range(0, area.z));

            if (!Physics.CheckSphere(spawnPos, spawnRadius, spawnBlockingLayer))
            {
                targets.Add(GameObject.Instantiate(targetPrefab, spawnPos, Quaternion.identity));
                Debug.Log("Spawning");
                spawnedCount++;
            }
            
        }
    }
}
