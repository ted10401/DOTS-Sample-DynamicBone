using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{
    public Transform m_Root = null;
    public float m_UpdateRate = 60.0f;
    [Range(0, 1)]
    public float m_Damping = 0.1f;
    [Range(0, 1)]
    public float m_Elasticity = 0.1f;
    [Range(0, 1)]
    public float m_Stiffness = 0.1f;
    [Range(0, 1)]
    public float m_Inert = 0;
    public float m_Radius = 0;

    Vector3 m_ObjectMove = Vector3.zero;
    Vector3 m_ObjectPrevPosition = Vector3.zero;
    float m_BoneTotalLength = 0;
    float m_ObjectScale = 1.0f;
    float m_Time = 0;

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
        InitTransforms();
    }

    void LateUpdate()
    {
        UpdateDynamicBones(Time.deltaTime);
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

            p.m_Damping = Mathf.Clamp01(p.m_Damping);
            p.m_Elasticity = Mathf.Clamp01(p.m_Elasticity);
            p.m_Stiffness = Mathf.Clamp01(p.m_Stiffness);
            p.m_Inert = Mathf.Clamp01(p.m_Inert);
            p.m_Radius = Mathf.Max(p.m_Radius, 0);
        }
        
    }

    void AppendParticles(Transform b, int parentIndex, float boneLength)
    {
        if(b.GetComponent<Renderer>() != null)
        {
            return;
        }

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
            p.m_EndOffset = pb.InverseTransformPoint(pb.position);
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
            for(int i = 0; i < b.childCount; i++)
            {
                AppendParticles(b.GetChild(i), index, boneLength);
            }
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
                p.m_Position.x = p.m_Position.x + fact * v.x + rmove.x;
                p.m_Position.y = p.m_Position.y + fact * v.y + rmove.y;
                p.m_Position.z = p.m_Position.z + fact * v.z + rmove.z;
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
            float stiffness = p.m_Stiffness;
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
                float stiffness = p.m_Stiffness;
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
