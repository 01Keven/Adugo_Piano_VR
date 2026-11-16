using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>
    /// Uma tecla branca tocada por toque direto. O objeto fica no plano em que o toque passa a valer
    /// (superfície da tecla menos <see cref="pressDepth"/>); a parte visual afunda conforme o dedo ou o
    /// controle empurra.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PianoKey : MonoBehaviour
    {
        [SerializeField] Transform m_Visual;
        [SerializeField] Renderer m_Renderer;

        public int index { get; private set; }
        public KeyState state { get; private set; } = KeyState.Idle;
        public bool isPressed => m_Interactable != null && m_Interactable.isSelected;

        /// <summary>Tecla afundada até o fim. Informa a mão (esquerda ou direita) que tocou.</summary>
        public event Action<PianoKey, InteractorHandedness> Pressed;
        public event Action<PianoKey> Released;

        XRSimpleInteractable m_Interactable;
        XRPokeFilter m_PokeFilter;
        KeyPalette m_Palette;
        MaterialPropertyBlock m_Block;
        float m_PressDepth;
        float m_VisualRestY;
        Vector3 m_HalfSize;
        readonly List<IXRHoverInteractor> m_Hovering = new List<IXRHoverInteractor>();

        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");

        public float pressDepth => m_PressDepth;

        /// <summary>Monta a tecla. <paramref name="size"/> é largura x altura x comprimento, em metros.</summary>
        public void Setup(int keyIndex, Vector3 size, float depth, KeyPalette palette, Material material)
        {
            index = keyIndex;
            m_Palette = palette != null ? palette : KeyPalette.Default;
            m_PressDepth = depth;
            m_HalfSize = size * 0.5f;
            name = $"Key {keyIndex + 1}";

            if (m_Visual == null)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                Destroy(visual.GetComponent<Collider>());
                visual.transform.SetParent(transform, false);
                m_Visual = visual.transform;
                m_Renderer = visual.GetComponent<Renderer>();
                m_Renderer.sharedMaterial = material;
            }

            // A superfície da tecla fica pressDepth acima deste objeto.
            m_Visual.localScale = size;
            m_VisualRestY = depth - m_HalfSize.y;
            m_Visual.localPosition = new Vector3(0f, m_VisualRestY, 0f);

            // Colisor cobre a tecla e um pouco acima dela, para o toque ser detectado ao se aproximar.
            var box = GetComponent<BoxCollider>();
            const float hoverHeadroom = 0.03f;
            var bottom = depth - size.y;
            var top = depth + hoverHeadroom;
            box.size = new Vector3(size.x, top - bottom, size.z);
            box.center = new Vector3(0f, (top + bottom) * 0.5f, 0f);

            if (m_Interactable == null)
            {
                m_Interactable = gameObject.AddComponent<XRSimpleInteractable>();
                m_Interactable.hoverFilters.Add(PokeOnlyFilter.Instance);
                m_Interactable.selectFilters.Add(PokeOnlyFilter.Instance);
                m_Interactable.hoverEntered.AddListener(OnHoverEntered);
                m_Interactable.hoverExited.AddListener(OnHoverExited);
                m_Interactable.selectEntered.AddListener(OnSelectEntered);
                m_Interactable.selectExited.AddListener(OnSelectExited);

                // O XRPokeFilter só lê a configuração no Start, então ela é definida aqui, antes disso.
                m_PokeFilter = gameObject.AddComponent<XRPokeFilter>();
                m_PokeFilter.pokeConfiguration.Value = new PokeThresholdData
                {
                    pokeDirection = PokeAxis.NegativeY,
                    interactionDepthOffset = 0f,
                    enablePokeAngleThreshold = true,
                    pokeAngleThreshold = 60f,
                };
            }

            SetState(KeyState.Idle);
        }

        public void SetState(KeyState newState)
        {
            state = newState;
            ApplyColor();
        }

        void Update()
        {
            if (m_Visual == null)
                return;

            // Afunda a parte visual até onde o dedo ou o controle empurrou.
            var push = 0f;
            if (isPressed)
            {
                push = m_PressDepth;
            }
            else
            {
                foreach (var interactor in m_Hovering)
                {
                    var tip = transform.InverseTransformPoint(interactor.GetAttachTransform(m_Interactable).position);
                    if (Mathf.Abs(tip.x) > m_HalfSize.x || Mathf.Abs(tip.z) > m_HalfSize.z)
                        continue;
                    push = Mathf.Max(push, Mathf.Clamp(m_PressDepth - tip.y, 0f, m_PressDepth));
                }
            }

            var target = m_VisualRestY - push;
            var pos = m_Visual.localPosition;
            if (!Mathf.Approximately(pos.y, target))
            {
                pos.y = target;
                m_Visual.localPosition = pos;
                ApplyColor();
            }
        }

        void ApplyColor()
        {
            if (m_Renderer == null)
                return;
            m_Block ??= new MaterialPropertyBlock();
            var color = m_Palette.Get(state);
            var pushed = m_PressDepth > 0f ? Mathf.Clamp01((m_VisualRestY - m_Visual.localPosition.y) / m_PressDepth) : 0f;
            color *= 1f - pushed * m_Palette.pressedDarken;
            color.a = 1f;
            m_Block.SetColor(s_BaseColor, color);
            m_Renderer.SetPropertyBlock(m_Block);
        }

        void OnHoverEntered(HoverEnterEventArgs args) => m_Hovering.Add(args.interactorObject);

        void OnHoverExited(HoverExitEventArgs args) => m_Hovering.Remove(args.interactorObject);

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            var hand = args.interactorObject.handedness;

            // Com controle há vibração; com hand tracking o retorno é visual e sonoro.
            var haptics = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (haptics != null)
                haptics.SendHapticImpulse(0.4f, 0.08f);

            Pressed?.Invoke(this, hand);
        }

        void OnSelectExited(SelectExitEventArgs args) => Released?.Invoke(this);

        /// <summary>Atalho para testes: dispara um toque como se a tecla tivesse sido pressionada.</summary>
        public void SimulatePress(InteractorHandedness hand) => Pressed?.Invoke(this, hand);
    }
}
