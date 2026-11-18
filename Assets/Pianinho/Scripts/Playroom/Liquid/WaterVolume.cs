using System.Collections.Generic;
using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Volume de água do aquário. Fica num objeto com Collider "Is Trigger" cobrindo só a água.
    /// Todo Rigidbody que entra recebe empuxo e arrasto; ao sair, o arrasto original volta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Playroom/Water Volume")]
    public class WaterVolume : MonoBehaviour
    {
        [Header("Padrão (objetos sem Buoyant Body)")]
        [SerializeField, Min(0f)] float m_DefaultFloatFactor = 0.9f;
        [SerializeField, Min(0.01f)] float m_DefaultSubmersionDepth = 0.15f;

        [Header("Arrasto na água")]
        [SerializeField, Min(0f)] float m_LinearDamping = 3f;
        [SerializeField, Min(0f)] float m_AngularDamping = 2f;

        [Header("Respingo")]
        [SerializeField] ParticleSystem m_SplashPrefab;
        [SerializeField] AudioClip m_SplashClip;
        [SerializeField, Min(0f)] float m_MinSplashSpeed = 0.5f;

        class Entry
        {
            public int colliderCount;
            public float linearDamping;
            public float angularDamping;
            public BuoyantBody buoyant;
            public GrabEvents grab;
        }

        readonly Dictionary<Rigidbody, Entry> m_Bodies = new Dictionary<Rigidbody, Entry>();
        readonly List<Rigidbody> m_Destroyed = new List<Rigidbody>();
        Collider m_Volume;

        void Reset()
        {
            if (TryGetComponent<Collider>(out var volume))
                volume.isTrigger = true;
        }

        void Awake() => m_Volume = GetComponent<Collider>();

        void OnDisable()
        {
            foreach (var pair in m_Bodies)
                if (pair.Key != null)
                    Restore(pair.Key, pair.Value);
            m_Bodies.Clear();
        }

        void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null)
                return;

            // Um objeto com vários colliders entra várias vezes; só o primeiro configura.
            if (m_Bodies.TryGetValue(body, out var existing))
            {
                existing.colliderCount++;
                return;
            }

            var entry = new Entry
            {
                colliderCount = 1,
                linearDamping = body.linearDamping,
                angularDamping = body.angularDamping,
            };
            body.TryGetComponent(out entry.buoyant);
            body.TryGetComponent(out entry.grab);
            m_Bodies.Add(body, entry);

            body.linearDamping = m_LinearDamping;
            body.angularDamping = m_AngularDamping;
            Splash(body);
        }

        void OnTriggerExit(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || !m_Bodies.TryGetValue(body, out var entry))
                return;

            if (--entry.colliderCount > 0)
                return;

            Restore(body, entry);
            m_Bodies.Remove(body);
        }

        void FixedUpdate()
        {
            var surfaceY = m_Volume.bounds.max.y;
            var gravity = Physics.gravity;

            foreach (var pair in m_Bodies)
            {
                var body = pair.Key;
                if (body == null)
                {
                    m_Destroyed.Add(body);
                    continue;
                }

                var entry = pair.Value;
                if (entry.grab != null && entry.grab.isHeld)
                    continue;

                var factor = entry.buoyant != null ? entry.buoyant.floatFactor : m_DefaultFloatFactor;
                var range = entry.buoyant != null ? entry.buoyant.fullSubmersionDepth : m_DefaultSubmersionDepth;
                var depth = Mathf.Clamp01((surfaceY - body.worldCenterOfMass.y) / range);
                body.AddForce(-gravity * (factor * depth), ForceMode.Acceleration);
            }

            if (m_Destroyed.Count == 0)
                return;
            foreach (var gone in m_Destroyed)
                m_Bodies.Remove(gone);
            m_Destroyed.Clear();
        }

        void Splash(Rigidbody body)
        {
            if (body.linearVelocity.magnitude < m_MinSplashSpeed)
                return;

            var point = new Vector3(body.position.x, m_Volume.bounds.max.y, body.position.z);
            if (m_SplashPrefab != null)
            {
                var splash = Instantiate(m_SplashPrefab, point, Quaternion.identity);
                var main = splash.main;
                Destroy(splash.gameObject, main.duration + main.startLifetime.constantMax);
            }
            if (m_SplashClip != null)
                AudioSource.PlayClipAtPoint(m_SplashClip, point);
        }

        static void Restore(Rigidbody body, Entry entry)
        {
            body.linearDamping = entry.linearDamping;
            body.angularDamping = entry.angularDamping;
        }
    }
}
