using UnityEngine;
using Unity.Entities;

public class ParticleDataComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public DynamicBoneDataComponent dynamicBoneDataComponent;
    public Entity dynamicBoneEntity;
    public int parentIndex;
    [Range(0, 1)] public float inertia = 0.5f;
    [Range(0, 1)] public float damping = 0.2f;
    [Range(0, 1)] public float elasticity = 0.05f;
    [Range(0, 1)] public float stiffness = 0.7f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ParticleData(dynamicBoneEntity, parentIndex, inertia, damping, elasticity, stiffness, transform));
    }
}
