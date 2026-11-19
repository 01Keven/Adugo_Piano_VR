using System.Collections.Generic;
using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Volume de água do aquário. Fica num objeto com Collider "Is Trigger" cobrindo a água.
    /// Todo Rigidbody que entra recebe empuxo e arrasto; ao sair, o arrasto original volta.
    /// Com uma <see cref="WaterSurface"/> ligada, o empuxo segue a altura da onda, objetos que boiam
    /// escorregam para o vale da onda e quem se mexe perto da superfície cria ondulações.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Playroom/Water Volume")]
    public class WaterVolume : MonoBehaviour
    {
        
        [SerializeField] WaterSurface m_Surface;

        [SerializeField, Min(0f)] float m_DefaultFloatFactor = 0.9f;
        [SerializeField, Min(0.01f)] float m_DefaultSubmersionDepth = 0.15f;

        [SerializeField, Min(0f)] float m_LinearDamping = 3f;
        [SerializeField, Min(0f)] float m_AngularDamping = 2f;

        [SerializeField, Range(0f, 1f)] float m_WaveDrift = 0.25f;

        
        [SerializeField, Min(0f)] float m_RippleHeightPerSpeed = 0.006f;

        [Tooltip("Abaixo dessa velocidade (m/s) o objeto não faz ondas.")]
        [SerializeField, Min(0f)] float m_MinRippleSpeed = 0.15f;

        [SerializeField, Min(0.02f)] float m_RippleInterval = 0.12f;

        [SerializeField, Min(0f)] float m_RippleReach = 0.08f;

        [SerializeField] ParticleSystem m_SplashPrefab;
        [SerializeField] AudioClip m_SplashClip;
        [SerializeField, Min(0f)] float m_MinSplashSpeed = 0.5f;

        // Cair na água faz uma ondulação mais forte do que simplesmente mexer nela
        const float k_ImpactRippleBoost = 2f;

        class Entry
        {
            public int colliderCount;
            public float linearDamping;
            public float angularDamping;
            public float nextRippleTime;
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

        void Awake()
        {
            m_Volume = GetComponent<Collider>();
            if (m_Surface == null)
                m_Surface = GetComponentInChildren<WaterSurface>();
        }

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

            // O arrasto novo vale depois que a velocidade de entrada é lida pelo respingo.
            Splash(body);
            body.linearDamping = m_LinearDamping;
            body.angularDamping = m_AngularDamping;
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
                var center = body.worldCenterOfMass;
                // Positivo = o centro do objeto está abaixo da superfície naquele ponto.
                var submersion = SurfaceHeight(center) - center.y;

                // Objeto na mão também mexe na água, mesmo sem sofrer empuxo.
                EmitRipples(body, entry, center, submersion);

                if (entry.grab != null && entry.grab.isHeld)
                    continue;

                var factor = entry.buoyant != null ? entry.buoyant.floatFactor : m_DefaultFloatFactor;
                var range = entry.buoyant != null ? entry.buoyant.fullSubmersionDepth : m_DefaultSubmersionDepth;
                var depth = Mathf.Clamp01(submersion / range);
                body.AddForce(-gravity * (factor * depth), ForceMode.Acceleration);

                Drift(body, center, submersion, range);
            }

            if (m_Destroyed.Count == 0)
                return;
            foreach (var gone in m_Destroyed)
                m_Bodies.Remove(gone);
            m_Destroyed.Clear();
        }

        /// Altura (mundo) da água no ponto: a da onda, ou o topo do collider se não há superfície
        float SurfaceHeight(Vector3 worldPoint) =>
            m_Surface != null ? m_Surface.HeightAt(worldPoint) : m_Volume.bounds.max.y;

        ///Quem boia perto da superfície escorrega para o vale da onda
        void Drift(Rigidbody body, Vector3 center, float submersion, float range)
        {
            if (m_Surface == null || m_WaveDrift <= 0f)
                return;

            // No fundo a água não carrega nada; o efeito some com a distância da superfície.
            var nearSurface = 1f - Mathf.Clamp01(Mathf.Abs(submersion) / (range * 2f));
            if (nearSurface <= 0f)
                return;

            var downhill = m_Surface.DownhillAt(center);
            body.AddForce(downhill * (Physics.gravity.magnitude * m_WaveDrift * nearSurface), ForceMode.Acceleration);
        }

        void EmitRipples(Rigidbody body, Entry entry, Vector3 center, float submersion)
        {
            if (m_Surface == null || Time.time < entry.nextRippleTime || Mathf.Abs(submersion) > m_RippleReach)
                return;

            var speed = body.linearVelocity.magnitude;
            if (speed < m_MinRippleSpeed)
                return;

            entry.nextRippleTime = Time.time + m_RippleInterval;
            m_Surface.AddRipple(center, speed * m_RippleHeightPerSpeed);
        }

        void Splash(Rigidbody body)
        {
            var speed = body.linearVelocity.magnitude;
            if (speed < m_MinSplashSpeed)
                return;

            var position = body.position;
            var point = new Vector3(position.x, SurfaceHeight(position), position.z);

            if (m_Surface != null)
                m_Surface.AddRipple(point, speed * m_RippleHeightPerSpeed * k_ImpactRippleBoost);

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