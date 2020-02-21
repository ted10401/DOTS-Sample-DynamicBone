using UnityEngine;

[AddComponentMenu("Dynamic Bone/Dynamic Bone Collider")]
public class DynamicBoneCollider : MonoBehaviour
{
    private Transform m_transform = null;

#if ORI_DYNAMIC_BONE
#else
    private float m_scaledRadius;
    private Vector3 m_transformedCenter;
#endif

    public Vector3 m_Center = Vector3.zero;
    public float m_Radius = 0.5f;
    public float m_Height = 0;

    public enum Direction
    {
        X, Y, Z
    }
    public Direction m_Direction = Direction.X;

    public enum Bound
    {
        Outside,
        Inside
    }
    public Bound m_Bound = Bound.Outside;

    private void Awake()
    {
        m_transform = transform;
    }

    void OnValidate()
    {
        m_Radius = Mathf.Max(m_Radius, 0);
        m_Height = Mathf.Max(m_Height, 0);
    }

#if ORI_DYNAMIC_BONE
#else
    public void PreUpdate()
    {
        m_scaledRadius = m_Radius * Mathf.Abs(m_transform.lossyScale.x);
        m_transformedCenter = m_transform.TransformPoint(m_Center);
    }
#endif

    public void Collide(ref Vector3 particlePosition, float particleRadius)
    {
#if ORI_DYNAMIC_BONE
        float radius = m_Radius * Mathf.Abs(m_transform.lossyScale.x);
#else
#endif
        float h = m_Height * 0.5f - m_Radius;
        if (h <= 0)
        {
            if (m_Bound == Bound.Outside)
#if ORI_DYNAMIC_BONE
                OutsideSphere(ref particlePosition, particleRadius, m_transform.TransformPoint(m_Center), radius);
#else
                OutsideSphere(ref particlePosition, particleRadius, m_transformedCenter, m_scaledRadius);
#endif
            else
#if ORI_DYNAMIC_BONE
                InsideSphere(ref particlePosition, particleRadius, m_transform.TransformPoint(m_Center), radius);
#else
                InsideSphere(ref particlePosition, particleRadius, m_transformedCenter, m_scaledRadius);
#endif
        }
        else
        {
            Vector3 c0 = m_Center;
            Vector3 c1 = m_Center;

            switch (m_Direction)
            {
                case Direction.X:
                    c0.x -= h;
                    c1.x += h;
                    break;
                case Direction.Y:
                    c0.y -= h;
                    c1.y += h;
                    break;
                case Direction.Z:
                    c0.z -= h;
                    c1.z += h;
                    break;
            }
            if (m_Bound == Bound.Outside)
#if ORI_DYNAMIC_BONE
                OutsideCapsule(ref particlePosition, particleRadius, m_transform.TransformPoint(c0), m_transform.TransformPoint(c1), radius);
#else
                OutsideCapsule(ref particlePosition, particleRadius, m_transform.TransformPoint(c0), m_transform.TransformPoint(c1), m_scaledRadius);
#endif
            else
#if ORI_DYNAMIC_BONE
                InsideCapsule(ref particlePosition, particleRadius, m_transform.TransformPoint(c0), m_transform.TransformPoint(c1), radius);
#else
                InsideCapsule(ref particlePosition, particleRadius, m_transform.TransformPoint(c0), m_transform.TransformPoint(c1), m_scaledRadius);
#endif
        }
    }

    static void OutsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
    {
        float r = sphereRadius + particleRadius;
        float r2 = r * r;
        
        //Vector3 d = particlePosition - sphereCenter;
        Vector3 d = particlePosition;
        d.x -= sphereCenter.x;
        d.y -= sphereCenter.y;
        d.z -= sphereCenter.z;

        float len2 = d.sqrMagnitude;

        // if is inside sphere, project onto sphere surface
        if (len2 > 0 && len2 < r2)
        {
            float len = Mathf.Sqrt(len2);
            //particlePosition = sphereCenter + d * (r / len);
            float f = (r / len);
            particlePosition.x = sphereCenter.x + d.x * f;
            particlePosition.y = sphereCenter.y + d.y * f;
            particlePosition.z = sphereCenter.z + d.z * f;
        }
    }

    static void InsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
    {
        float r = sphereRadius + particleRadius;
        float r2 = r * r;
        
        //Vector3 d = particlePosition - sphereCenter;
        Vector3 d = particlePosition;
        d.x -= sphereCenter.x;
        d.y -= sphereCenter.y;
        d.z -= sphereCenter.z;

        float len2 = d.sqrMagnitude;

        // if is outside sphere, project onto sphere surface
        if (len2 > r2)
        {
            float len = Mathf.Sqrt(len2);
            
            //particlePosition = sphereCenter + d * (r / len);
            float f = (r / len);
            particlePosition.x = sphereCenter.x + d.x * f;
            particlePosition.y = sphereCenter.y + d.y * f;
            particlePosition.z = sphereCenter.z + d.z * f;
        }
    }

    static void OutsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
    {
        float r = capsuleRadius + particleRadius;
        float r2 = r * r;

        //Vector3 dir = capsuleP1 - capsuleP0;
        Vector3 dir = capsuleP1;
        dir.x -= capsuleP0.x;
        dir.y -= capsuleP0.y;
        dir.z -= capsuleP0.z;

        //Vector3 d = particlePosition - capsuleP0;
        Vector3 d = particlePosition;
        d.x = capsuleP0.x;
        d.y = capsuleP0.y;
        d.z = capsuleP0.z;

        float t = Vector3.Dot(d, dir);

        if (t <= 0)
        {
            // check sphere1
            float len2 = d.sqrMagnitude;
            if (len2 > 0 && len2 < r2)
            {
                float len = Mathf.Sqrt(len2);
                //particlePosition = capsuleP0 + d * (r / len);
                float f = (r / len);
                particlePosition.x = capsuleP0.x + d.x * f;
                particlePosition.y = capsuleP0.y + d.y * f;
                particlePosition.z = capsuleP0.z + d.z * f;
            }
        }
        else
        {
            float dl = dir.sqrMagnitude;
            if (t >= dl)
            {
                // check sphere2
                //d = particlePosition - capsuleP1;
                d = particlePosition;
                d.x -= capsuleP1.x;
                d.y -= capsuleP1.y;
                d.z -= capsuleP1.z;

                float len2 = d.sqrMagnitude;
                if (len2 > 0 && len2 < r2)
                {
                    float len = Mathf.Sqrt(len2);
                    //particlePosition = capsuleP1 + d * (r / len);
                    float f = (r / len);
                    particlePosition.x = capsuleP1.x + d.x * f;
                    particlePosition.y = capsuleP1.y + d.y * f;
                    particlePosition.z = capsuleP1.z + d.z * f;
                }
            }
            else if (dl > 0)
            {
                // check cylinder
                t /= dl;

                //d -= dir * t;
                d.x -= dir.x * t;
                d.y -= dir.y * t;
                d.z -= dir.z * t;

                float len2 = d.sqrMagnitude;
                if (len2 > 0 && len2 < r2)
                {
                    float len = Mathf.Sqrt(len2);
                    //particlePosition += d * ((r - len) / len);
                    float f = ((r - len) / len);
                    particlePosition.x += d.x * f;
                    particlePosition.y += d.y * f;
                    particlePosition.z += d.z * f;
                }
            }
        }
    }

    static void InsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
    {
        float r = capsuleRadius + particleRadius;
        float r2 = r * r;
        
        //Vector3 dir = capsuleP1 - capsuleP0;
        Vector3 dir = capsuleP1;
        dir.x -= capsuleP0.x;
        dir.y -= capsuleP0.y;
        dir.z -= capsuleP0.z;

        //Vector3 d = particlePosition - capsuleP0;
        Vector3 d = particlePosition;
        d.x = capsuleP0.x;
        d.y = capsuleP0.y;
        d.z = capsuleP0.z;

        float t = Vector3.Dot(d, dir);

        if (t <= 0)
        {
            // check sphere1
            float len2 = d.sqrMagnitude;
            if (len2 > r2)
            {
                float len = Mathf.Sqrt(len2);
                //particlePosition = capsuleP0 + d * (r / len);

                float f = (r / len);
                particlePosition.x = capsuleP0.x + d.x * f;
                particlePosition.y = capsuleP0.y + d.y * f;
                particlePosition.z = capsuleP0.z + d.z * f;
            }
        }
        else
        {
            float dl = dir.sqrMagnitude;
            if (t >= dl)
            {
                // check sphere2
                //d = particlePosition - capsuleP1;
                d = particlePosition;
                d.x -= capsuleP1.x;
                d.y -= capsuleP1.y;
                d.z -= capsuleP1.z;

                float len2 = d.sqrMagnitude;
                if (len2 > r2)
                {
                    float len = Mathf.Sqrt(len2);
                    //particlePosition = capsuleP1 + d * (r / len);

                    float f = (r / len);
                    particlePosition.x = capsuleP1.x + d.x * f;
                    particlePosition.y = capsuleP1.y + d.y * f;
                    particlePosition.z = capsuleP1.z + d.z * f;
                }
            }
            else if (dl > 0)
            {
                // check cylinder
                t /= dl;

                //d -= dir * t;
                d.x -= dir.x * t;
                d.y -= dir.y * t;
                d.z -= dir.z * t;

                float len2 = d.sqrMagnitude;
                if (len2 > r2)
                {
                    float len = Mathf.Sqrt(len2);
                    //particlePosition += d * ((r - len) / len);

                    float f = ((r - len) / len);
                    particlePosition.x += d.x * f;
                    particlePosition.y += d.y * f;
                    particlePosition.z += d.z * f;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;

        if (m_Bound == Bound.Outside)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.magenta;
        float radius = m_Radius * Mathf.Abs(transform.lossyScale.x);
        float h = m_Height * 0.5f - m_Radius;
        if (h <= 0)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(m_Center), radius);
        }
        else
        {
            Vector3 c0 = m_Center;
            Vector3 c1 = m_Center;

            switch (m_Direction)
            {
                case Direction.X:
                    c0.x -= h;
                    c1.x += h;
                    break;
                case Direction.Y:
                    c0.y -= h;
                    c1.y += h;
                    break;
                case Direction.Z:
                    c0.z -= h;
                    c1.z += h;
                    break;
            }
            Gizmos.DrawWireSphere(transform.TransformPoint(c0), radius);
            Gizmos.DrawWireSphere(transform.TransformPoint(c1), radius);
        }
    }
}
