using Unity.Entities;
using Unity.Mathematics;

public struct DynamicBoneData : IComponentData
{
    public float3 objectInertia;
    public float3 objectPrevPosition;
    public float inertia;
    public float damping;
    public float elasticity;
    public float stiffness;
}