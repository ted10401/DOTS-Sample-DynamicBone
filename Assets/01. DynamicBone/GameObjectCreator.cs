using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectCreator : MonoBehaviour
{
    public GameObject prefab;
    public int count = 10;

    private void Awake()
    {
        for(int x = 0; x < count; x++)
        {
            for(int y = 0; y < count; y++)
            {
                GameObject instance = Instantiate(prefab, new Vector3(x - count * 0.5f, 0, y - count * 0.5f), Quaternion.identity);
            }
        }
    }
}
