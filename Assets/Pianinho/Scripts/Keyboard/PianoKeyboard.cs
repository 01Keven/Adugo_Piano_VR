using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>
    /// Teclado com visual de piano e número de teclas variável (<see cref="SessionConfig.keyCount"/>).
    /// Só as teclas brancas são tocáveis; as pretas são decorativas.
    /// O teclado olha para +Z: o jogador fica do lado -Z, e as teclas se estendem para longe dele.
    /// </summary>
    public class PianoKeyboard : MonoBehaviour
    {
        [Header("Dimensões (metros)")]
        [SerializeField] float m_KeyWidth = 0.085f;
        [SerializeField] float m_KeyGap = 0.006f;
        [SerializeField] float m_KeyLength = 0.20f;
        [SerializeField] float m_KeyHeight = 0.025f;

        [Header("Aparência")]
        [SerializeField] KeyPalette m_Palette;
        [SerializeField] Material m_KeyMaterial;
        [SerializeField] bool m_ShowBlackKeys = true;

        [Header("Som")]
        [SerializeField] bool m_ClickOnPress = true;
        [SerializeField, Range(0f, 1f)] float m_ClickVolume = 0.35f;

        readonly List<PianoKey> m_Keys = new List<PianoKey>();
        Transform m_KeysRoot;
        Transform m_DecorRoot;
        AudioSource m_Audio;
        AudioClip m_Click;
        Material m_RuntimeMaterial;
        int m_BuiltCount = -1;
        float m_BuiltDepth = -1f;

        public IReadOnlyList<PianoKey> keys => m_Keys;
        public KeyPalette palette => m_Palette != null ? m_Palette : KeyPalette.Default;

        /// <summary>Largura total das teclas, em metros.</summary>
        public float width => m_Keys.Count * m_KeyWidth + Mathf.Max(0, m_Keys.Count - 1) * m_KeyGap;

        public float keyLength => m_KeyLength;

        /// <summary>Uma tecla foi tocada até o fim. Informa qual tecla e com qual mão.</summary>
        public event Action<PianoKey, InteractorHandedness> KeyPressed;
        public event Action<PianoKey> KeyReleased;
        /// <summary>O teclado foi reconstruído (por exemplo, mudou o número de teclas).</summary>
        public event Action Rebuilt;

        void OnEnable()
        {
            Session.Changed += OnSessionChanged;
            Build();
        }

        void OnDisable() => Session.Changed -= OnSessionChanged;

        void OnDestroy()
        {
            if (m_RuntimeMaterial != null)
                Destroy(m_RuntimeMaterial);
            if (m_Click != null)
                Destroy(m_Click);
        }

        void OnSessionChanged(SessionConfig config)
        {
            if (config.keyCount != m_BuiltCount || !Mathf.Approximately(config.pressDepth, m_BuiltDepth))
                Build();
        }

        /// <summary>Recria as teclas a partir da sessão atual.</summary>
        public void Build()
        {
            var config = Session.Current;
            var count = Mathf.Clamp(config.keyCount, SessionConfig.MinKeys, SessionConfig.MaxKeys);

            EnsureRoots();
            foreach (var key in m_Keys)
            {
                key.Pressed -= OnKeyPressed;
                key.Released -= OnKeyReleased;
                Destroy(key.gameObject);
            }
            m_Keys.Clear();
            for (var i = m_DecorRoot.childCount - 1; i >= 0; i--)
                Destroy(m_DecorRoot.GetChild(i).gameObject);

            var material = GetMaterial();
            var size = new Vector3(m_KeyWidth, m_KeyHeight, m_KeyLength);
            var totalWidth = count * m_KeyWidth + (count - 1) * m_KeyGap;
            var startX = -totalWidth * 0.5f + m_KeyWidth * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var go = new GameObject();
                go.transform.SetParent(m_KeysRoot, false);
                // A superfície das teclas fica em y = 0 do teclado.
                go.transform.localPosition = new Vector3(startX + i * (m_KeyWidth + m_KeyGap), -config.pressDepth, 0f);
                var key = go.AddComponent<PianoKey>();
                key.Setup(i, size, config.pressDepth, palette, material);
                key.Pressed += OnKeyPressed;
                key.Released += OnKeyReleased;
                m_Keys.Add(key);
            }

            BuildDecor(count, totalWidth, startX, material);

            m_BuiltCount = count;
            m_BuiltDepth = config.pressDepth;
            Rebuilt?.Invoke();
        }

        /// <summary>Muda a cor de todas as teclas para um estado.</summary>
        public void SetAll(KeyState state)
        {
            foreach (var key in m_Keys)
                key.SetState(state);
        }

        void BuildDecor(int count, float totalWidth, float startX, Material material)
        {
            // Base e trilho traseiro: dão a leitura de "piano" e um limite físico às teclas.
            var baseHeight = 0.02f;
            CreateBox("Base", new Vector3(0f, -m_KeyHeight - baseHeight * 0.5f - 0.002f, 0.01f),
                new Vector3(totalWidth + 0.05f, baseHeight, m_KeyLength + 0.06f), palette.body, material);
            CreateBox("Back Rail", new Vector3(0f, 0.012f - m_KeyHeight * 0.5f, m_KeyLength * 0.5f + 0.03f),
                new Vector3(totalWidth + 0.05f, m_KeyHeight + 0.024f, 0.05f), palette.body, material);

            if (!m_ShowBlackKeys)
                return;

            // Padrão de pretas a partir do Dó: entre Dó-Ré, Ré-Mi, Fá-Sol, Sol-Lá, Lá-Si.
            var blackWidth = m_KeyWidth * 0.5f;
            var blackLength = m_KeyLength * 0.45f;
            var blackHeight = 0.014f;
            for (var i = 0; i < count - 1; i++)
            {
                var degree = i % 7;
                if (degree == 2 || degree == 6)
                    continue;
                var x = startX + i * (m_KeyWidth + m_KeyGap) + (m_KeyWidth + m_KeyGap) * 0.5f;
                CreateBox($"Black {i + 1}", new Vector3(x, blackHeight * 0.5f, m_KeyLength * 0.5f - blackLength * 0.5f),
                    new Vector3(blackWidth, blackHeight, blackLength), palette.blackKey, material);
            }
        }

        void CreateBox(string boxName, Vector3 localPos, Vector3 size, Color color, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = boxName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(m_DecorRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }

        void EnsureRoots()
        {
            if (m_KeysRoot == null)
            {
                m_KeysRoot = new GameObject("Keys").transform;
                m_KeysRoot.SetParent(transform, false);
            }
            if (m_DecorRoot == null)
            {
                m_DecorRoot = new GameObject("Decor").transform;
                m_DecorRoot.SetParent(transform, false);
            }
        }

        Material GetMaterial()
        {
            if (m_KeyMaterial != null)
                return m_KeyMaterial;
            if (m_RuntimeMaterial == null)
            {
                var source = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
                if (source == null)
                {
                    Debug.LogError("Pianinho: nenhum material padrão do URP encontrado. Atribua um Key Material no teclado.", this);
                    return null;
                }
                m_RuntimeMaterial = new Material(source) { name = "Pianinho Key (runtime)" };
                if (m_RuntimeMaterial.HasProperty("_Smoothness"))
                    m_RuntimeMaterial.SetFloat("_Smoothness", 0.35f);
            }
            return m_RuntimeMaterial;
        }

        void OnKeyPressed(PianoKey key, InteractorHandedness hand)
        {
            if (m_ClickOnPress)
                PlayClick(key.transform.position);
            KeyPressed?.Invoke(key, hand);
        }

        void OnKeyReleased(PianoKey key) => KeyReleased?.Invoke(key);

        void PlayClick(Vector3 position)
        {
            if (m_Audio == null)
            {
                m_Audio = gameObject.AddComponent<AudioSource>();
                m_Audio.playOnAwake = false;
                m_Audio.spatialBlend = 0.6f;
            }
            m_Click ??= CreateClick();
            m_Audio.transform.position = transform.position;
            m_Audio.PlayOneShot(m_Click, m_ClickVolume);
        }

        /// <summary>Clique curto e suave gerado em código, para não depender de arquivo de áudio.</summary>
        static AudioClip CreateClick()
        {
            const int sampleRate = 44100;
            const float duration = 0.06f;
            var samples = new float[(int)(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 70f);
                samples[i] = envelope * (0.7f * Mathf.Sin(2f * Mathf.PI * 660f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 1320f * t));
            }
            var clip = AudioClip.Create("Pianinho Click", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
