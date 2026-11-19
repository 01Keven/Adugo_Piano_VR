using System;
using UnityEngine;

namespace Playroom
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Playroom/Water Surface")]
    public class WaterSurface : MonoBehaviour
    {
        public struct Wave
        {
            public float amplitude;

            public float wavelength;

            public float speed;

            [Range(0f, 360f)] public float direction;
        }

        struct Ripple
        {
            public float x;
            public float z;
            public float startTime;
            public float amplitude;
        }

        const int k_MaxRipples = 16;

        [SerializeField] Vector2 m_Size = new Vector2(1f, 0.5f);

        [SerializeField, Range(8, 64)] int m_Resolution = 32;

        [SerializeField] Wave[] m_Waves =
        {
            new Wave { amplitude = 0.006f, wavelength = 0.55f, speed = 0.45f, direction = 20f },
            new Wave { amplitude = 0.004f, wavelength = 0.31f, speed = 0.35f, direction = 100f },
            new Wave { amplitude = 0.0025f, wavelength = 0.17f, speed = 0.50f, direction = 200f },
        };

        [SerializeField, Range(0f, 3f)] float m_WaveScale = 1f;

        [SerializeField, Min(0.05f)] float m_RippleSpeed = 0.35f;

        [SerializeField, Min(0.01f)] float m_RippleWavelength = 0.06f;

        [SerializeField, Min(0.01f)] float m_RippleWidth = 0.1f;

        [SerializeField, Min(0.1f)] float m_RippleLifetime = 3f;

        [SerializeField, Min(0f)] float m_MaxRippleHeight = 0.02f;

        
        [SerializeField] Vector2 m_TextureScroll = new Vector2(0.01f, 0.006f);

        readonly Ripple[] m_Ripples = new Ripple[k_MaxRipples];
        int m_NextRipple;

        Vector2[] m_Directions;
        float[] m_WaveNumbers;

        Mesh m_Mesh;
        Material m_Material;
        MeshRenderer m_Renderer;
        Vector3[] m_Vertices;
        Vector3[] m_Normals;
        float[] m_Heights;
        Vector2 m_Step;

        /// Tamanho da água, em metros (X e Z)
        public Vector2 size => m_Size;

        void Awake()
        {
            m_Renderer = GetComponent<MeshRenderer>();
            Prepare();
            BuildMesh();
        }

        void Update() => UpdateMesh(Time.time);

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
            if (m_Material != null)
                Destroy(m_Material);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_Size.x, 0.002f, m_Size.y));
        }

        

        ///Altura (mundo) da superfície no ponto, já com ondas e ondulações
        public float HeightAt(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            var height = SampleLocal(local.x, local.z, Time.time);
            return transform.TransformPoint(new Vector3(local.x, height, local.z)).y;
        }

       
        /// Direção horizontal "ladeira abaixo" da onda no ponto (mundo). O comprimento é a inclinação:
        /// 0 = água plana ali. Serve para empurrar o que boia para o vale da onda.
       
        public Vector3 DownhillAt(Vector3 worldPoint)
        {
            const float step = 0.02f;
            var local = transform.InverseTransformPoint(worldPoint);
            var time = Time.time;
            var dx = (SampleLocal(local.x + step, local.z, time) - SampleLocal(local.x - step, local.z, time)) / (2f * step);
            var dz = (SampleLocal(local.x, local.z + step, time) - SampleLocal(local.x, local.z - step, time)) / (2f * step);
            return transform.TransformDirection(new Vector3(-dx, 0f, -dz));
        }

        /// Cria uma ondulação circular no ponto (mundo). A amplitude é limitada pelo máximo
        public void AddRipple(Vector3 worldPoint, float amplitude)
        {
            if (amplitude <= 0f)
                return;

            var local = transform.InverseTransformPoint(worldPoint);
            m_Ripples[m_NextRipple] = new Ripple
            {
                x = local.x,
                z = local.z,
                startTime = Time.time,
                amplitude = Mathf.Min(amplitude, m_MaxRippleHeight),
            };
            // Anel de 16 posições: quando enche, a ondulação mais antiga é substituída.
            m_NextRipple = (m_NextRipple + 1) % k_MaxRipples;
        }

       

        float SampleLocal(float x, float z, float time)
        {
            // Se alguém consultar antes do Awake (objeto desligado na cena), prepara na hora.
            if (m_Directions == null)
                Prepare();

            var height = 0f;

            var waveCount = Mathf.Min(m_Waves != null ? m_Waves.Length : 0, m_Directions.Length);
            for (var i = 0; i < waveCount; i++)
            {
                var wave = m_Waves[i];
                var phase = m_WaveNumbers[i] * (m_Directions[i].x * x + m_Directions[i].y * z - wave.speed * time);
                height += wave.amplitude * Mathf.Sin(phase);
            }
            height *= m_WaveScale;

            for (var i = 0; i < k_MaxRipples; i++)
            {
                var ripple = m_Ripples[i];
                var age = time - ripple.startTime;
                if (ripple.amplitude <= 0f || age < 0f || age >= m_RippleLifetime)
                    continue;

                var dx = x - ripple.x;
                var dz = z - ripple.z;
                var distance = Mathf.Sqrt(dx * dx + dz * dz);

                // Pacote de onda: um anel que cresce, enfraquece com a distância e some com o tempo.
                var offset = distance - m_RippleSpeed * age;
                var envelope = Mathf.Exp(-(offset * offset) / (m_RippleWidth * m_RippleWidth));
                if (envelope < 0.001f)
                    continue;

                var fade = 1f - age / m_RippleLifetime;
                var spread = 1f / (1f + distance * 8f);
                height += ripple.amplitude * fade * spread * envelope
                          * Mathf.Cos(offset * 2f * Mathf.PI / m_RippleWavelength);
            }
            return height;
        }

        /// Pré-calcula direção e número de onda; refeito a cada quadro, então dá para ajustar em jogo
        void Prepare()
        {
            var count = m_Waves != null ? m_Waves.Length : 0;
            if (m_Directions == null || m_Directions.Length != count)
            {
                m_Directions = new Vector2[count];
                m_WaveNumbers = new float[count];
            }
            for (var i = 0; i < count; i++)
            {
                var angle = m_Waves[i].direction * Mathf.Deg2Rad;
                m_Directions[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                m_WaveNumbers[i] = 2f * Mathf.PI / Mathf.Max(m_Waves[i].wavelength, 0.01f);
            }
        }


        void BuildMesh()
        {
            var n = m_Resolution + 1;
            m_Vertices = new Vector3[n * n];
            m_Normals = new Vector3[n * n];
            m_Heights = new float[n * n];
            m_Step = new Vector2(m_Size.x / m_Resolution, m_Size.y / m_Resolution);

            var uv = new Vector2[n * n];
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    m_Vertices[i] = new Vector3(-m_Size.x * 0.5f + x * m_Step.x, 0f, -m_Size.y * 0.5f + z * m_Step.y);
                    m_Normals[i] = Vector3.up;
                    uv[i] = new Vector2((float)x / m_Resolution, (float)z / m_Resolution);
                }
            }

            var triangles = new int[m_Resolution * m_Resolution * 6];
            var t = 0;
            for (var z = 0; z < m_Resolution; z++)
            {
                for (var x = 0; x < m_Resolution; x++)
                {
                    var i = z * n + x;
                    triangles[t++] = i;
                    triangles[t++] = i + n;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + n;
                    triangles[t++] = i + n + 1;
                }
            }

            m_Mesh = new Mesh { name = "Water Surface (runtime)" };
            m_Mesh.MarkDynamic();
            m_Mesh.SetVertices(m_Vertices);
            m_Mesh.SetNormals(m_Normals);
            m_Mesh.SetUVs(0, uv);
            m_Mesh.SetTriangles(triangles, 0);
            GetComponent<MeshFilter>().sharedMesh = m_Mesh;
        }

        void UpdateMesh(float time)
        {
            Prepare();

            var resolution = m_Resolution;
            var n = resolution + 1;

            for (var i = 0; i < m_Vertices.Length; i++)
                m_Heights[i] = SampleLocal(m_Vertices[i].x, m_Vertices[i].z, time);

            for (var z = 0; z < n; z++)
            {
                var zl = Mathf.Max(z - 1, 0);
                var zr = Mathf.Min(z + 1, resolution);
                for (var x = 0; x < n; x++)
                {
                    var xl = Mathf.Max(x - 1, 0);
                    var xr = Mathf.Min(x + 1, resolution);
                    var i = z * n + x;

                    var vertex = m_Vertices[i];
                    vertex.y = m_Heights[i];
                    m_Vertices[i] = vertex;

                    // Normal pela inclinação entre os vizinhos: mais barato que RecalculateNormals.
                    var dx = (m_Heights[z * n + xr] - m_Heights[z * n + xl]) / ((xr - xl) * m_Step.x);
                    var dz = (m_Heights[zr * n + x] - m_Heights[zl * n + x]) / ((zr - zl) * m_Step.y);
                    m_Normals[i] = new Vector3(-dx, 1f, -dz).normalized;
                }
            }

            m_Mesh.SetVertices(m_Vertices);
            m_Mesh.SetNormals(m_Normals);

            if (m_TextureScroll != Vector2.zero)
            {
                if (m_Material == null)
                    m_Material = m_Renderer.material; // instância própria: não mexe no asset
                m_Material.mainTextureOffset = new Vector2(
                    Mathf.Repeat(m_TextureScroll.x * time, 1f),
                    Mathf.Repeat(m_TextureScroll.y * time, 1f));
            }
        }
    }
}