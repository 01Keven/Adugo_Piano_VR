using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace Pianinho
{
    /// <summary>
    /// Durante a partida só vale o toque direto: esconde os lasers (near-far e teleporte) e desliga a
    /// locomoção, para o paciente não se mover sem querer pelo joystick. Tudo volta ao sair da partida.
    /// </summary>
    public class GameplayInputMode : MonoBehaviour
    {
        [Tooltip("Rig XR a ajustar. Vazio = procura na cena.")]
        [SerializeField] Transform m_Rig;

        readonly List<GameObject> m_HiddenObjects = new List<GameObject>();
        readonly List<Behaviour> m_DisabledBehaviours = new List<Behaviour>();

        public bool inGameplay { get; private set; }

        public void EnterGameplay()
        {
            if (inGameplay)
                return;
            inGameplay = true;

            var root = m_Rig != null ? m_Rig : null;
            foreach (var interactor in Find<NearFarInteractor>(root))
                Hide(interactor.gameObject);
            foreach (var interactor in Find<XRRayInteractor>(root))
                Hide(interactor.gameObject);
            foreach (var provider in Find<LocomotionProvider>(root))
                Disable(provider);

            // O gerenciador de entrada dos controles do Starter Assets religa os lasers quando o
            // joystick é usado; ele fica desligado durante a partida.
            foreach (var behaviour in Find<MonoBehaviour>(root))
                if (behaviour.GetType().Name == "ControllerInputActionManager")
                    Disable(behaviour);
        }

        public void ExitGameplay()
        {
            if (!inGameplay)
                return;
            inGameplay = false;

            foreach (var behaviour in m_DisabledBehaviours)
                if (behaviour != null)
                    behaviour.enabled = true;
            foreach (var go in m_HiddenObjects)
                if (go != null)
                    go.SetActive(true);
            m_DisabledBehaviours.Clear();
            m_HiddenObjects.Clear();
        }

        void OnDisable() => ExitGameplay();

        void Hide(GameObject go)
        {
            if (!go.activeSelf)
                return;
            go.SetActive(false);
            m_HiddenObjects.Add(go);
        }

        void Disable(Behaviour behaviour)
        {
            if (!behaviour.enabled)
                return;
            behaviour.enabled = false;
            m_DisabledBehaviours.Add(behaviour);
        }

        static T[] Find<T>(Transform root) where T : Component =>
            root != null ? root.GetComponentsInChildren<T>(false) : FindObjectsByType<T>(FindObjectsSortMode.None);
    }
}
