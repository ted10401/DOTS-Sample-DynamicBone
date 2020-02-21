using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class EntityCreator : MonoBehaviour
{
    public GameObject prefab;
    public int count = 10;

    private void Awake()
    {
        World destinationWorld = World.DefaultGameObjectInjectionWorld;
        BlobAssetStore blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings gameObjectConversionSettings = GameObjectConversionSettings.FromWorld(destinationWorld, blobAssetStore);
        Entity entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, gameObjectConversionSettings);
        SineMovementData sineMovementData = new SineMovementData()
        {
            moveSpeed = 10
        };

        for (int x = 0; x < count; x++)
        {
            for(int y = 0; y < count; y++)
            {
                Entity instanceEntity = destinationWorld.EntityManager.Instantiate(entity);
                Translation translation = new Translation()
                {
                    Value = new Unity.Mathematics.float3(x - count * 0.5f, 0, y - count * 0.5f)
                };

                sineMovementData.originalPosition = translation.Value;
                destinationWorld.EntityManager.SetComponentData(instanceEntity, translation);
                destinationWorld.EntityManager.SetComponentData(instanceEntity, sineMovementData);
            }
        }

        blobAssetStore.Dispose();
    }
}
