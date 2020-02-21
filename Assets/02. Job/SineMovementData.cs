using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct SineMovementData : IComponentData
{
    public float3 originalPosition;
    public float moveSpeed;
}
