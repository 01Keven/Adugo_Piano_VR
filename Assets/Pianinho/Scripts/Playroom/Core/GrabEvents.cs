using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Playroom
{
    /// <summary>
    /// Adaptador fino sobre o XR Grab Interactable. Os demais scripts do salão dependem só desta
    /// classe para saber se o objeto está na mão, sem conhecer o XRI diretamente.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabEvents : MonoBehaviour
    {
        XRGrabInteractable m_Interactable;

        public bool isHeld => m_Interactable != null && m_Interactable.isSelected;

        /// <summary>O objeto foi pego por uma mão ou controle.</summary>
        public event Action Grabbed;

        /// <summary>O objeto foi solto.</summary>
        public event Action Released;

        void Awake() => m_Interactable = GetComponent<XRGrabInteractable>();

        void OnEnable()
        {
            m_Interactable.selectEntered.AddListener(OnSelectEntered);
            m_Interactable.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
            m_Interactable.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs args) => Grabbed?.Invoke();

        void OnSelectExited(SelectExitEventArgs args) => Released?.Invoke();
    }
}
