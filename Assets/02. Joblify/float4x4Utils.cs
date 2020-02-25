using Unity.Mathematics;

public static class float4x4Utils
{
    public static float magnitude(this float3 value)
    {
        return math.sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
    }

    public static float3 MultiplyPoint3x4(this float4x4 matrix, float3 point)
    {
        float3 res;
        res.x = matrix.c0.x * point.x + matrix.c0.y * point.y + matrix.c0.z * point.z + matrix.c0.w;
        res.y = matrix.c1.x * point.x + matrix.c1.y * point.y + matrix.c1.z * point.z + matrix.c1.w;
        res.z = matrix.c2.x * point.x + matrix.c2.y * point.y + matrix.c2.z * point.z + matrix.c2.w;

        return res;
    }
}
