using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Playroom
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabEvents : MonoBehaviour
    {
        XRGrabInteractable m_Interactable;

        public bool isHeld => m_Interactable != null && m_Interactable.isSelected;

        /// O objeto foi pego por uma mao ou controle
        public event Action Grabbed;

        /// solto
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
