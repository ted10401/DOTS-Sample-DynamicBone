using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{
    public Transform root = null;
    public float updateRate = 60.0f;
    [Range(0, 1)] public float damping = 0.1f;
    [Range(0, 1)] public float elasticity = 0.1f;
    [Range(0, 1)] public float stiffness = 0.1f;
    [Range(0, 1)] public float inert = 0;
    public float radius = 0;

    private Vector3 m_objectMove = Vector3.zero;
    private Vector3 m_objectPrevPosition = Vector3.zero;
    private float m_boneTotalLength = 0;
    private float m_objectScale = 1.0f;
    private float m_time = 0;

    private class Particle
    {
        public int parentIndex = -1;
        public float m_damping = 0;
        public float m_elasticity = 0;
        public float m_stiffness = 0;
        public float m_inert = 0;
        public float m_radius = 0;
        public float m_boneLength = 0;

        public Vector3 position = Vector3.zero;
        public Vector3 prevPosition = Vector3.zero;
        public Vector3 initLocalPosition = Vector3.zero;
        public Quaternion initLocalRotation = Quaternion.identity;
    }

    private List<Transform> m_particleTransforms = new List<Transform>();
    private List<Particle> m_particles = new List<Particle>();

    private void Start()
    {
        SetupParticles();
    }

    private void Update()
    {
        InitTransforms();
    }

    private void LateUpdate()
    {
        UpdateDynamicBones(Time.deltaTime);
    }

    private void OnEnable()
    {
        ResetParticlesPosition();
    }

    private void OnDisable()
    {
        InitTransforms();
    }

    private void OnValidate()
    {
        updateRate = Mathf.Max(updateRate, 0);
        damping = Mathf.Clamp01(damping);
        elasticity = Mathf.Clamp01(elasticity);
        stiffness = Mathf.Clamp01(stiffness);
        inert = Mathf.Clamp01(inert);
        radius = Mathf.Max(radius, 0);

        if (Application.isEditor && Application.isPlaying)
        {
            InitTransforms();
            SetupParticles();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!enabled || root == null)
        {
            return;
        }

        if (Application.isEditor && !Application.isPlaying && transform.hasChanged)
        {
            InitTransforms();
            SetupParticles();
        }

        Gizmos.color = Color.white;

        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];

            if (p.parentIndex >= 0)
            {
                Particle p0 = m_particles[p.parentIndex];
                Gizmos.DrawLine(p.position, p0.position);
            }

            if (p.m_radius > 0)
            {
                Gizmos.DrawWireSphere(p.position, p.m_radius * m_objectScale);
            }
        }
    }
    
    private void UpdateDynamicBones(float t)
    {
        if (root == null)
        {
            return;
        }

        m_objectScale = Mathf.Abs(transform.lossyScale.x);
        m_objectMove = transform.position - m_objectPrevPosition;
        m_objectPrevPosition = transform.position;

        int loop = 1;
        if (updateRate > 0)
        {
            float dt = 1.0f / updateRate;
            m_time += t;
            loop = 0;

            while (m_time >= dt)
            {
                m_time -= dt;
                if (++loop >= 3)
                {
                    m_time = 0;
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
                m_objectMove = Vector3.zero;
            }
        }
        else
        {
            SkipUpdateParticles();
        }

        ApplyParticlesToTransforms();
    }

    private void SetupParticles()
    {
        m_particleTransforms.Clear();
        m_particles.Clear();

        if (root == null)
        {
            return;
        }
        
        m_objectScale = transform.lossyScale.x;
        m_objectPrevPosition = transform.position;
        m_objectMove = Vector3.zero;
        m_boneTotalLength = 0;
        AppendParticles(root, -1, 0);

        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];
            p.m_damping = damping;
            p.m_elasticity = elasticity;
            p.m_stiffness = stiffness;
            p.m_inert = inert;
            p.m_radius = radius;

            p.m_damping = Mathf.Clamp01(p.m_damping);
            p.m_elasticity = Mathf.Clamp01(p.m_elasticity);
            p.m_stiffness = Mathf.Clamp01(p.m_stiffness);
            p.m_inert = Mathf.Clamp01(p.m_inert);
            p.m_radius = Mathf.Max(p.m_radius, 0);
        }
        
    }

    private void AppendParticles(Transform b, int parentIndex, float boneLength)
    {
        if(b.GetComponent<Renderer>() != null)
        {
            return;
        }

        m_particleTransforms.Add(b);

        Particle p = new Particle();
        p.parentIndex = parentIndex;
        p.position = p.prevPosition = b.position;
        p.initLocalPosition = b.localPosition;
        p.initLocalRotation = b.localRotation;

        if (parentIndex >= 0)
        {
            boneLength += (m_particleTransforms[parentIndex].position - p.position).magnitude;
            p.m_boneLength = boneLength;
            m_boneTotalLength = Mathf.Max(m_boneTotalLength, boneLength);
        }

        int index = m_particles.Count;
        m_particles.Add(p);

        for (int i = 0; i < b.childCount; i++)
        {
            AppendParticles(b.GetChild(i), index, boneLength);
        }
    }

    private void InitTransforms()
    {
        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            if (pTrans != null)
            {
                pTrans.localPosition = p.initLocalPosition;
                pTrans.localRotation = p.initLocalRotation;
            }
        }
    }

    private void ResetParticlesPosition()
    {
        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            if (pTrans != null)
            {
                p.position = p.prevPosition = pTrans.position;
            }
        }

        m_objectPrevPosition = transform.position;
    }

    private void UpdateParticles1()
    {
        Vector3 v;
        Vector3 rmove;
        for (int i = 0, count = m_particles.Count; i < count; ++i)
        {
            Particle p = m_particles[i];
            if (p.parentIndex >= 0)
            {
                // verlet integration
                //Vector3 v = p.m_Position - p.m_PrevPosition;
                v = new Vector3(p.position.x - p.prevPosition.x,
                                p.position.y - p.prevPosition.y,
                                p.position.z - p.prevPosition.z);

                //Vector3 rmove = m_ObjectMove * p.m_Inert;
                rmove = new Vector3(m_objectMove.x * p.m_inert,
                                    m_objectMove.y * p.m_inert,
                                    m_objectMove.z * p.m_inert);

                //p.m_PrevPosition = p.m_Position + rmove;
                p.prevPosition.x = p.position.x + rmove.x;
                p.prevPosition.y = p.position.y + rmove.y;
                p.prevPosition.z = p.position.z + rmove.z;
                
                //p.m_Position += v * (1 - p.m_Damping) + force + rmove;
                float fact = (1 - p.m_damping);
                p.position.x = p.position.x + fact * v.x + rmove.x;
                p.position.y = p.position.y + fact * v.y + rmove.y;
                p.position.z = p.position.z + fact * v.z + rmove.z;
            }
            else
            {
                p.prevPosition = p.position;
                p.position = m_particleTransforms[i].position;
            }
        }
    }

    private void UpdateParticles2()
    {
        for (int i = 1, count_i = m_particles.Count; i < count_i; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            Particle p0 = m_particles[p.parentIndex];
            Transform pTrans0 = m_particleTransforms[p.parentIndex];

            Vector3 pos_p0 = pTrans0.position;
            Vector3 pos_p = pTrans.position;
            pos_p0.x -= pos_p.x;
            pos_p0.y -= pos_p.y;
            pos_p0.z -= pos_p.z;

            float restLen = pos_p0.magnitude;

            // keep shape
            float stiffness = p.m_stiffness;
            if (stiffness > 0 || p.m_elasticity > 0)
            {
                Matrix4x4 m0 = pTrans0.localToWorldMatrix;
                m0.SetColumn(3, p0.position);
                Vector3 restPos = m0.MultiplyPoint3x4(pTrans.localPosition);

                //Vector3 d = restPos - p.m_Position;
                Vector3 d = new Vector3(restPos.x - p.position.x,
                                        restPos.y - p.position.y,
                                        restPos.z - p.position.z);

                //p.m_Position += d * p.m_Elasticity;
                p.position.x += d.x * p.m_elasticity;
                p.position.y += d.y * p.m_elasticity;
                p.position.z += d.z * p.m_elasticity;

                if (stiffness > 0)
                {
                    //d = restPos - p.m_Position;
                    d.x = restPos.x - p.position.x;
                    d.y = restPos.y - p.position.y;
                    d.z = restPos.z - p.position.z;

                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                    {
                        // p.m_Position += d * ((len - maxlen) / len);
                        float f = ((len - maxlen) / len);
                        p.position.x += d.x * f;
                        p.position.y += d.y * f;
                        p.position.z += d.z * f;
                    }
                }
            }

            // keep length
            // Vector3 dd = p0.m_Position - p.m_Position;
            Vector3 dd = new Vector3(p0.position.x - p.position.x,
                                     p0.position.y - p.position.y,
                                     p0.position.z - p.position.z);

            float leng = dd.magnitude;
            if (leng > 0)
            {
                // p.m_Position += dd * ((leng - restLen) / leng);
                float f = ((leng - restLen) / leng);
                p.position.x += dd.x * f;
                p.position.y += dd.y * f;
                p.position.z += dd.z * f;
            }
        }
    }

    // only update stiffness and keep bone length
    private void SkipUpdateParticles()
    {
        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            if (p.parentIndex >= 0)
            {
                p.prevPosition += m_objectMove;
                p.position += m_objectMove;

                Particle p0 = m_particles[p.parentIndex];
                Transform pTrans0 = m_particleTransforms[p.parentIndex];

                float restLen = (pTrans0.position - pTrans.position).magnitude;

                // keep shape
                float stiffness = p.m_stiffness;
                if (stiffness > 0)
                {
                    Matrix4x4 m0 = pTrans0.localToWorldMatrix;
                    m0.SetColumn(3, p0.position);
                    Vector3 restPos = m0.MultiplyPoint3x4(pTrans.localPosition);

                    Vector3 d = restPos - p.position;
                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;

                    if (len > maxlen)
                    {
                        p.position += d * ((len - maxlen) / len);
                    }
                }

                // keep length
                Vector3 dd = p0.position - p.position;
                float leng = dd.magnitude;
                if (leng > 0)
                {
                    p.position += dd * ((leng - restLen) / leng);
                }
            }
            else
            {
                p.prevPosition = p.position;
                p.position = pTrans.position;
            }
        }
    }

    private void ApplyParticlesToTransforms()
    {
        for (int i = 1, count = m_particles.Count; i < count; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            Particle p0 = m_particles[p.parentIndex];
            Transform pTrans0 = m_particleTransforms[p.parentIndex];

            if (pTrans0.childCount <= 1)		// do not modify bone orientation if has more then one child
            {
                Vector3 v = pTrans.localPosition;
                Quaternion rot = Quaternion.FromToRotation(pTrans0.TransformDirection(v), p.position - p0.position);
                pTrans0.rotation = rot * pTrans0.rotation;
            }

            pTrans.position = p.position;
        }
    }
}
