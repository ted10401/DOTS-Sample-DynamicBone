using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{
    public Transform m_Root = null;
    public float m_UpdateRate = 60.0f;
    [Range(0, 1)]
    public float m_Damping = 0.1f;
    public AnimationCurve m_DampingDistrib = null;
    [Range(0, 1)]
    public float m_Elasticity = 0.1f;
    public AnimationCurve m_ElasticityDistrib = null;
    [Range(0, 1)]
    public float m_Stiffness = 0.1f;
    public AnimationCurve m_StiffnessDistrib = null;
    [Range(0, 1)]
    public float m_Inert = 0;
    public AnimationCurve m_InertDistrib = null;
    public float m_Radius = 0;
    public AnimationCurve m_RadiusDistrib = null;

    public float m_EndLength = 0;
    public Vector3 m_EndOffset = Vector3.zero;
    public Vector3 m_Gravity = Vector3.zero;
    public Vector3 m_Force = Vector3.zero;
    public List<DynamicBoneCollider> m_Colliders = null;
    public List<Transform> m_Exclusions = null;
    public enum FreezeAxis
    {
        None, X, Y, Z
    }
    public FreezeAxis m_FreezeAxis = FreezeAxis.None;
    public bool m_DistantDisable = false;
    public Transform m_ReferenceObject = null;
    public float m_DistanceToObject = 20;
    
    // 是否可視範圍內
    public bool Visible = true;

    Vector3 m_LocalGravity = Vector3.zero;
    Vector3 m_ObjectMove = Vector3.zero;
    Vector3 m_ObjectPrevPosition = Vector3.zero;
    float m_BoneTotalLength = 0;
    float m_ObjectScale = 1.0f;
    float m_Time = 0;
    float m_Weight = 1.0f;
    bool m_DistantDisabled = false;

    class Particle
    {
        public Transform m_Transform = null;
        public int m_ParentIndex = -1;
        public float m_Damping = 0;
        public float m_Elasticity = 0;
        public float m_Stiffness = 0;
        public float m_Inert = 0;
        public float m_Radius = 0;
        public float m_BoneLength = 0;

        public Vector3 m_Position = Vector3.zero;
        public Vector3 m_PrevPosition = Vector3.zero;
        public Vector3 m_EndOffset = Vector3.zero;
        public Vector3 m_InitLocalPosition = Vector3.zero;
        public Quaternion m_InitLocalRotation = Quaternion.identity;
    }

    List<Particle> m_Particles = new List<Particle>();

    void Start()
    {
        SetupParticles();
    }

    void Update()
    {
        if (!Visible)
            return;

        if (m_Weight > 0 && !(m_DistantDisable && m_DistantDisabled))
            InitTransforms();
    }

    void LateUpdate()
    {
        if (!Visible)
            return;

        if (m_DistantDisable)
            CheckDistance();

        if (m_Weight > 0 && !(m_DistantDisable && m_DistantDisabled))
            UpdateDynamicBones(Time.deltaTime);
    }

    void CheckDistance()
    {
        Transform rt = m_ReferenceObject;
        if (rt == null && Camera.main != null)
            rt = Camera.main.transform;
        if (rt != null)
        {
            float d = (rt.position - transform.position).sqrMagnitude;
            bool disable = d > m_DistanceToObject * m_DistanceToObject;
            if (disable != m_DistantDisabled)
            {
                if (!disable)
                    ResetParticlesPosition();
                m_DistantDisabled = disable;
            }
        }
    }

    void OnEnable()
    {
        ResetParticlesPosition();
    }

    void OnDisable()
    {
        InitTransforms();
    }

    void OnValidate()
    {
        m_UpdateRate = Mathf.Max(m_UpdateRate, 0);
        m_Damping = Mathf.Clamp01(m_Damping);
        m_Elasticity = Mathf.Clamp01(m_Elasticity);
        m_Stiffness = Mathf.Clamp01(m_Stiffness);
        m_Inert = Mathf.Clamp01(m_Inert);
        m_Radius = Mathf.Max(m_Radius, 0);

        if (Application.isEditor && Application.isPlaying)
        {
            InitTransforms();
            SetupParticles();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled || m_Root == null)
            return;

        if (Application.isEditor && !Application.isPlaying && transform.hasChanged)
        {
            InitTransforms();
            SetupParticles();
        }

        Gizmos.color = Color.white;
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                Particle p0 = m_Particles[p.m_ParentIndex];
                Gizmos.DrawLine(p.m_Position, p0.m_Position);
            }
            if (p.m_Radius > 0)
                Gizmos.DrawWireSphere(p.m_Position, p.m_Radius * m_ObjectScale);
        }
    }

    public void SetWeight(float w)
    {
        if (m_Weight != w)
        {
            if (w == 0)
                InitTransforms();
            else if (m_Weight == 0)
                ResetParticlesPosition();
            m_Weight = w;
        }
    }

    public float GetWeight()
    {
        return m_Weight;
    }

    void UpdateDynamicBones(float t)
    {
        if (m_Root == null)
            return;

        m_ObjectScale = Mathf.Abs(transform.lossyScale.x);
        m_ObjectMove = transform.position - m_ObjectPrevPosition;
        m_ObjectPrevPosition = transform.position;

        int loop = 1;
        if (m_UpdateRate > 0)
        {
            float dt = 1.0f / m_UpdateRate;
            m_Time += t;
            loop = 0;

            while (m_Time >= dt)
            {
                m_Time -= dt;
                if (++loop >= 3)
                {
                    m_Time = 0;
                    break;
                }
            }
        }

        if (loop > 0)
        {
            for (int i = 0; i < loop; ++i)
            {
                UpdateParticles1();
                UpdateParticles2();
                m_ObjectMove = Vector3.zero;
            }
        }
        else
        {
            SkipUpdateParticles();
        }

        ApplyParticlesToTransforms();
    }

    void SetupParticles()
    {
        m_Particles.Clear();
        if (m_Root == null)
            return;

        m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);
        m_ObjectScale = transform.lossyScale.x;
        m_ObjectPrevPosition = transform.position;
        m_ObjectMove = Vector3.zero;
        m_BoneTotalLength = 0;
        AppendParticles(m_Root, -1, 0);

        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            p.m_Damping = m_Damping;
            p.m_Elasticity = m_Elasticity;
            p.m_Stiffness = m_Stiffness;
            p.m_Inert = m_Inert;
            p.m_Radius = m_Radius;

            if (m_BoneTotalLength > 0)
            {
                float a = p.m_BoneLength / m_BoneTotalLength;
                if (m_DampingDistrib != null && m_DampingDistrib.keys.Length > 0)
                    p.m_Damping *= m_DampingDistrib.Evaluate(a);
                if (m_ElasticityDistrib != null && m_ElasticityDistrib.keys.Length > 0)
                    p.m_Elasticity *= m_ElasticityDistrib.Evaluate(a);
                if (m_StiffnessDistrib != null && m_StiffnessDistrib.keys.Length > 0)
                    p.m_Stiffness *= m_StiffnessDistrib.Evaluate(a);
                if (m_InertDistrib != null && m_InertDistrib.keys.Length > 0)
                    p.m_Inert *= m_InertDistrib.Evaluate(a);
                if (m_RadiusDistrib != null && m_RadiusDistrib.keys.Length > 0)
                    p.m_Radius *= m_RadiusDistrib.Evaluate(a);
            }

            p.m_Damping = Mathf.Clamp01(p.m_Damping);
            p.m_Elasticity = Mathf.Clamp01(p.m_Elasticity);
            p.m_Stiffness = Mathf.Clamp01(p.m_Stiffness);
            p.m_Inert = Mathf.Clamp01(p.m_Inert);
            p.m_Radius = Mathf.Max(p.m_Radius, 0);
        }
    }

    void AppendParticles(Transform b, int parentIndex, float boneLength)
    {
        Particle p = new Particle();
        p.m_Transform = b;
        p.m_ParentIndex = parentIndex;
        if (b != null)
        {
            p.m_Position = p.m_PrevPosition = b.position;
            p.m_InitLocalPosition = b.localPosition;
            p.m_InitLocalRotation = b.localRotation;
        }
        else 	// end bone
        {
            Transform pb = m_Particles[parentIndex].m_Transform;
            if (m_EndLength > 0)
            {
                Transform ppb = pb.parent;
                if (ppb != null)
                    p.m_EndOffset = pb.InverseTransformPoint((pb.position * 2 - ppb.position)) * m_EndLength;
                else
                    p.m_EndOffset = new Vector3(m_EndLength, 0, 0);
            }
            else
            {
                p.m_EndOffset = pb.InverseTransformPoint(transform.TransformDirection(m_EndOffset) + pb.position);
            }
            p.m_Position = p.m_PrevPosition = pb.TransformPoint(p.m_EndOffset);
        }

        if (parentIndex >= 0)
        {
            boneLength += (m_Particles[parentIndex].m_Transform.position - p.m_Position).magnitude;
            p.m_BoneLength = boneLength;
            m_BoneTotalLength = Mathf.Max(m_BoneTotalLength, boneLength);
        }

        int index = m_Particles.Count;
        m_Particles.Add(p);

        if (b != null)
        {
            for (int i = 0; i < b.childCount; ++i)
            {
                bool exclude = false;
                if (m_Exclusions != null)
                {
                    for (int j = 0; j < m_Exclusions.Count; ++j)
                    {
                        Transform e = m_Exclusions[j];
                        if (e == b.GetChild(i))
                        {
                            exclude = true;
                            break;
                        }
                    }
                }
                if (!exclude)
                    AppendParticles(b.GetChild(i), index, boneLength);
            }

            if (b.childCount == 0 && (m_EndLength > 0 || m_EndOffset != Vector3.zero))
                AppendParticles(null, index, boneLength);
        }
    }

    void InitTransforms()
    {
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_Transform != null)
            {
                p.m_Transform.localPosition = p.m_InitLocalPosition;
                p.m_Transform.localRotation = p.m_InitLocalRotation;
            }
        }
    }

    void ResetParticlesPosition()
    {
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_Transform != null)
            {
                p.m_Position = p.m_PrevPosition = p.m_Transform.position;
            }
            else	// end bone
            {
                Transform pb = m_Particles[p.m_ParentIndex].m_Transform;
                p.m_Position = p.m_PrevPosition = pb.TransformPoint(p.m_EndOffset);
            }
        }
        m_ObjectPrevPosition = transform.position;
    }

    void UpdateParticles1()
    {
        Vector3 force = m_Gravity;
        Vector3 fdir = m_Gravity.normalized;
        Vector3 rf = m_Root.TransformDirection(m_LocalGravity);
        Vector3 pf = fdir * Mathf.Max(Vector3.Dot(rf, fdir), 0);    // project current gravity to rest gravity

        // remove projected gravity
        //force -= pf;
        force.x -= pf.x;
        force.y -= pf.y;
        force.z -= pf.z;

        //force = (force + m_Force) * m_ObjectScale;
        force.x = (force.x + m_Force.x) * m_ObjectScale;
        force.y = (force.y + m_Force.y) * m_ObjectScale;
        force.z = (force.z + m_Force.z) * m_ObjectScale;

        Vector3 v;
        Vector3 rmove;
        for (int i = 0, count = m_Particles.Count; i < count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                // verlet integration
                //Vector3 v = p.m_Position - p.m_PrevPosition;
                v = new Vector3(p.m_Position.x - p.m_PrevPosition.x,
                                p.m_Position.y - p.m_PrevPosition.y,
                                p.m_Position.z - p.m_PrevPosition.z);

                //Vector3 rmove = m_ObjectMove * p.m_Inert;
                rmove = new Vector3(m_ObjectMove.x * p.m_Inert,
                                    m_ObjectMove.y * p.m_Inert,
                                    m_ObjectMove.z * p.m_Inert);

                //p.m_PrevPosition = p.m_Position + rmove;
                p.m_PrevPosition.x = p.m_Position.x + rmove.x;
                p.m_PrevPosition.y = p.m_Position.y + rmove.y;
                p.m_PrevPosition.z = p.m_Position.z + rmove.z;
                
                //p.m_Position += v * (1 - p.m_Damping) + force + rmove;
                float fact = (1 - p.m_Damping);
                p.m_Position.x = p.m_Position.x + fact * v.x + force.x + rmove.x;
                p.m_Position.y = p.m_Position.y + fact * v.y + force.y + rmove.y;
                p.m_Position.z = p.m_Position.z + fact * v.z + force.x + rmove.z;
            }
            else
            {
                p.m_PrevPosition = p.m_Position;
                p.m_Position = p.m_Transform.position;
            }
        }
    }

    void UpdateParticles2()
    {
#if ORI_DYNAMIC_BONE
#else
        int colliderCount = 0;
        if (m_Colliders != null)
        {
            colliderCount = m_Colliders.Count;
            for (int i = 0; i < colliderCount; i++)
            {
                DynamicBoneCollider c = m_Colliders[i];
                if (c != null && c.enabled)
                {
                    c.PreUpdate();
                }
            }
        }
#endif

        Plane movePlane = new Plane();
        bool p_transformNotNull = false;
        for (int i = 1, count_i = m_Particles.Count; i < count_i; ++i)
        {
            Particle p = m_Particles[i];
            Particle p0 = m_Particles[p.m_ParentIndex];
            p_transformNotNull = (p.m_Transform != null);
            float restLen;
            if (p_transformNotNull)
            {
                //restLen = (p0.m_Transform.position - p.m_Transform.position).magnitude;
                Vector3 pos_p0 = p0.m_Transform.position;
                Vector3 pos_p = p.m_Transform.position;
                pos_p0.x -= pos_p.x;
                pos_p0.y -= pos_p.y;
                pos_p0.z -= pos_p.z;

                restLen = pos_p0.magnitude;
            }
            else
            {
                restLen = p0.m_Transform.localToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude;
            }

            // keep shape
            float stiffness = Mathf.Lerp(1.0f, p.m_Stiffness, m_Weight);
            if (stiffness > 0 || p.m_Elasticity > 0)
            {
                Matrix4x4 m0 = p0.m_Transform.localToWorldMatrix;
                m0.SetColumn(3, p0.m_Position);
                Vector3 restPos;
                if (p_transformNotNull)
                    restPos = m0.MultiplyPoint3x4(p.m_Transform.localPosition);
                else
                    restPos = m0.MultiplyPoint3x4(p.m_EndOffset);

                //Vector3 d = restPos - p.m_Position;
                Vector3 d = new Vector3(restPos.x - p.m_Position.x,
                                        restPos.y - p.m_Position.y,
                                        restPos.z - p.m_Position.z);

                //p.m_Position += d * p.m_Elasticity;
                p.m_Position.x += d.x * p.m_Elasticity;
                p.m_Position.y += d.y * p.m_Elasticity;
                p.m_Position.z += d.z * p.m_Elasticity;

                if (stiffness > 0)
                {
                    //d = restPos - p.m_Position;
                    d.x = restPos.x - p.m_Position.x;
                    d.y = restPos.y - p.m_Position.y;
                    d.z = restPos.z - p.m_Position.z;

                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                    {
                        // p.m_Position += d * ((len - maxlen) / len);
                        float f = ((len - maxlen) / len);
                        p.m_Position.x += d.x * f;
                        p.m_Position.y += d.y * f;
                        p.m_Position.z += d.z * f;
                    }
                }
            }

            // collide
#if ORI_DYNAMIC_BONE
            if (m_Colliders != null)
            {
                float particleRadius = p.m_Radius * m_ObjectScale;
                for (int j = 0; j < m_Colliders.Count; ++j)
                {
                    DynamicBoneCollider c = m_Colliders[j];
                    if (c != null && c.enabled)
                        c.Collide(ref p.m_Position, particleRadius);
                }
            }
#else
            if (colliderCount > 0)
            {
                float particleRadius = p.m_Radius * m_ObjectScale;
                for (int j = 0; j < colliderCount; ++j)
                {
                    DynamicBoneCollider c = m_Colliders[j];
                    if (c != null && c.enabled)
                        c.Collide(ref p.m_Position, particleRadius);
                }
            }
#endif

            // freeze axis, project to plane 
            if (m_FreezeAxis != FreezeAxis.None)
            {
                switch (m_FreezeAxis)
                {
                    case FreezeAxis.X:
                        movePlane.SetNormalAndPosition(p0.m_Transform.right, p0.m_Position);
                        break;
                    case FreezeAxis.Y:
                        movePlane.SetNormalAndPosition(p0.m_Transform.up, p0.m_Position);
                        break;
                    case FreezeAxis.Z:
                        movePlane.SetNormalAndPosition(p0.m_Transform.forward, p0.m_Position);
                        break;
                }
                //p.m_Position -= movePlane.normal * movePlane.GetDistanceToPoint(p.m_Position);
                float dis = movePlane.GetDistanceToPoint(p.m_Position);
                Vector3 movePlaneNormal = movePlane.normal;
                p.m_Position.x -= movePlaneNormal.x * dis;
                p.m_Position.y -= movePlaneNormal.y * dis;
                p.m_Position.z -= movePlaneNormal.z * dis;
            }

            // keep length
            // Vector3 dd = p0.m_Position - p.m_Position;
            Vector3 dd = new Vector3(p0.m_Position.x - p.m_Position.x,
                                     p0.m_Position.y - p.m_Position.y,
                                     p0.m_Position.z - p.m_Position.z);

            float leng = dd.magnitude;
            if (leng > 0)
            {
                // p.m_Position += dd * ((leng - restLen) / leng);
                float f = ((leng - restLen) / leng);
                p.m_Position.x += dd.x * f;
                p.m_Position.y += dd.y * f;
                p.m_Position.z += dd.z * f;
            }
        }
    }

    // only update stiffness and keep bone length
    void SkipUpdateParticles()
    {
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                p.m_PrevPosition += m_ObjectMove;
                p.m_Position += m_ObjectMove;

                Particle p0 = m_Particles[p.m_ParentIndex];

                float restLen;
                if (p.m_Transform != null)
                    restLen = (p0.m_Transform.position - p.m_Transform.position).magnitude;
                else
                    restLen = p0.m_Transform.localToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude;

                // keep shape
                float stiffness = Mathf.Lerp(1.0f, p.m_Stiffness, m_Weight);
                if (stiffness > 0)
                {
                    Matrix4x4 m0 = p0.m_Transform.localToWorldMatrix;
                    m0.SetColumn(3, p0.m_Position);
                    Vector3 restPos;
                    if (p.m_Transform != null)
                        restPos = m0.MultiplyPoint3x4(p.m_Transform.localPosition);
                    else
                        restPos = m0.MultiplyPoint3x4(p.m_EndOffset);

                    Vector3 d = restPos - p.m_Position;
                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                        p.m_Position += d * ((len - maxlen) / len);
                }

                // keep length
                Vector3 dd = p0.m_Position - p.m_Position;
                float leng = dd.magnitude;
                if (leng > 0)
                    p.m_Position += dd * ((leng - restLen) / leng);
            }
            else
            {
                p.m_PrevPosition = p.m_Position;
                p.m_Position = p.m_Transform.position;
            }
        }
    }

    void ApplyParticlesToTransforms()
    {
        bool p_transformNotNull = false;
        for (int i = 1, count = m_Particles.Count; i < count; ++i)
        {
            Particle p = m_Particles[i];
            Particle p0 = m_Particles[p.m_ParentIndex];
            p_transformNotNull = (p.m_Transform != null);
            if (p0.m_Transform.childCount <= 1)		// do not modify bone orientation if has more then one child
            {
                Vector3 v;
                if (p_transformNotNull)
                    v = p.m_Transform.localPosition;
                else
                    v = p.m_EndOffset;
                Quaternion rot = Quaternion.FromToRotation(p0.m_Transform.TransformDirection(v), p.m_Position - p0.m_Position);
                p0.m_Transform.rotation = rot * p0.m_Transform.rotation;
            }

            if (p_transformNotNull)
                p.m_Transform.position = p.m_Position;
        }
    }
}
