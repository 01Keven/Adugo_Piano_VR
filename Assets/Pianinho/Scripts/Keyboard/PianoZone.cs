using System;
using System.Collections.Generic;
using Pianinho.UI;
using UnityEngine;

namespace Pianinho
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Collider))]
    public class PianoZone : MonoBehaviour
    {
        [Header("O que a área liga")]
        [Tooltip("Opcional. Objeto que contém o piano. Fica desligado até o jogador entrar.")]
        [SerializeField] GameObject m_PianoRoot;

        [Tooltip("Vazio = procura dentro do Piano Root.")]
        [SerializeField] MainMenu m_Menu;

        [Tooltip("Vazio = procura dentro do Piano Root.")]
        [SerializeField] KeyboardCalibration m_Calibration;

        [Tooltip("Vazio = procura dentro do Piano Root.")]
        [SerializeField] RhythmGame m_Game;

        [Tooltip("Outros objetos que só devem existir depois que o jogador entrar na área.")]
        [SerializeField] GameObject[] m_ExtraObjects;

        [Header("Detecção")]
        [Tooltip("Cabeça do jogador. Vazio = Camera.main. Se apontar para o rig, usa a câmera que está dentro dele.")]
        [SerializeField] Transform m_Head;

        [Tooltip("Ignora a altura: basta a cabeça estar sobre a área, mesmo que ela seja só uma placa no chão.")]
        [SerializeField] bool m_IgnoreHeight = true;

        [Tooltip("Ao começar o jogo, espera esse tempo (segundos) antes de detectar.")]
        [SerializeField, Min(0f)] float m_StartDelay = 1f;

        readonly List<GameObject> m_Controlled = new List<GameObject>();
        Collider m_Area;
        float m_ReadyTime;

        public bool isOpen { get; private set; }

        public event Action Opened;

        Transform head
        {
            get
            {
                if (m_Head == null && Camera.main != null)
                    m_Head = Camera.main.transform;
                return m_Head;
            }
        }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void Awake()
        {
            m_Area = GetComponent<Collider>();
            m_ReadyTime = Time.unscaledTime + m_StartDelay;
            ResolveHead();

            if (m_PianoRoot != null)
            {
                if (m_Menu == null)
                    m_Menu = m_PianoRoot.GetComponentInChildren<MainMenu>(true);
                if (m_Calibration == null)
                    m_Calibration = m_PianoRoot.GetComponentInChildren<KeyboardCalibration>(true);
                if (m_Game == null)
                    m_Game = m_PianoRoot.GetComponentInChildren<RhythmGame>(true);
            }

            AddControlled(m_PianoRoot);
            if (m_Game != null)
                AddControlled(m_Game.gameObject);
            if (m_Calibration != null)
                AddControlled(m_Calibration.gameObject);
            if (m_Menu != null)
                AddControlled(m_Menu.gameObject);
            if (m_ExtraObjects != null)
                foreach (var extra in m_ExtraObjects)
                    AddControlled(extra);

            if (m_Controlled.Count == 0)
            {
                Debug.LogError("Pianinho: a área do piano não encontrou nada para ligar. Atribua o Piano Root ou o Menu.", this);
                enabled = false;
                return;
            }

            SetControlledActive(false);
        }

        void Update()
        {
            if (Time.unscaledTime < m_ReadyTime || !IsHeadInside())
                return;
            Open();
        }

        public void Open()
        {
            if (isOpen)
                return;
            isOpen = true;
            SetControlledActive(true);

            if (m_Calibration != null)
                m_Calibration.PlaceInFront();
            if (m_Menu != null)
                m_Menu.Open();

            Debug.Log("Pianinho: jogador entrou na área do piano.", this);
            Opened?.Invoke();
            enabled = false;
        }

        void AddControlled(GameObject go)
        {
            if (go == null || m_Controlled.Contains(go))
                return;

            if (transform.IsChildOf(go.transform))
            {
                Debug.LogWarning($"Pianinho: '{go.name}' contém a área do piano e foi ignorado. Tire a área de dentro dele.", this);
                return;
            }
            m_Controlled.Add(go);
        }

        void SetControlledActive(bool active)
        {
            foreach (var go in m_Controlled)
                if (go != null)
                    go.SetActive(active);
        }

        void ResolveHead()
        {
            if (m_Head == null || m_Head.GetComponent<Camera>() != null)
                return;
            var camera = m_Head.GetComponentInChildren<Camera>(true);
            if (camera != null)
                m_Head = camera.transform;
        }

        bool IsHeadInside()
        {
            var tracked = head;
            if (tracked == null)
                return false;

            var point = tracked.position;
            if (m_IgnoreHeight)
                point.y = m_Area.bounds.center.y;

            return (m_Area.ClosestPoint(point) - point).sqrMagnitude < 1e-6f;
        }

        void OnDrawGizmos()
        {
            var area = GetComponent<Collider>();
            if (area == null)
                return;

            Gizmos.color = isOpen ? new Color(0.50f, 0.77f, 0.57f, 0.9f) : new Color(0.42f, 0.62f, 0.86f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            switch (area)
            {
                case BoxCollider box:
                    Gizmos.DrawWireCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                    break;
            }
        }
    }
}