using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>
    /// Posiciona o teclado em relação à cabeça do jogador, para jogar sentado ou em pé.
    /// A distância e a altura ficam na sessão (<see cref="SessionConfig.keyboardDistance"/> e
    /// <see cref="SessionConfig.keyboardBelowHead"/>), então cada paciente pode ter a sua.
    /// </summary>
    public class KeyboardCalibration : MonoBehaviour
    {
        [Tooltip("O que é movido. Vazio = este objeto.")]
        [SerializeField] Transform m_Target;

        [Tooltip("Cabeça do jogador. Vazio = Camera.main.")]
        [SerializeField] Transform m_Head;

        [Tooltip("Posiciona automaticamente ao iniciar, quando o rastreamento da cabeça começa.")]
        [SerializeField] bool m_PlaceOnStart = true;

        [Tooltip("Quanto cada toque nos botões de ajuste move o teclado, em metros.")]
        [SerializeField] float m_NudgeStep = 0.03f;

        Transform target => m_Target != null ? m_Target : transform;

        Transform head
        {
            get
            {
                if (m_Head == null && Camera.main != null)
                    m_Head = Camera.main.transform;
                return m_Head;
            }
        }

        public float nudgeStep => m_NudgeStep;

        IEnumerator Start()
        {
            if (!m_PlaceOnStart)
                yield break;

            // O rastreamento da cabeça (ou o simulador) leva alguns quadros para entregar a primeira pose.
            for (var i = 0; i < 30 && (head == null || head.localPosition == Vector3.zero); i++)
                yield return null;
            PlaceInFront();
        }

        /// <summary>Coloca o teclado na frente da cabeça, usando a distância e a altura da sessão.</summary>
        public void PlaceInFront()
        {
            if (head == null)
                return;

            var config = Session.Current;
            var forward = FlatForward();
            target.SetPositionAndRotation(
                head.position + forward * config.keyboardDistance + Vector3.down * config.keyboardBelowHead,
                Quaternion.LookRotation(forward, Vector3.up));
        }

        /// <summary>
        /// Calibra pela posição das mãos: o paciente estende as mãos onde é confortável tocar e
        /// o teclado vai para ali. A distância e a altura resultantes ficam salvas na sessão.
        /// Retorna falso se nenhuma mão ou controle estiver sendo rastreado.
        /// </summary>
        public bool CalibrateToHands()
        {
            if (head == null)
                return false;

            var sum = Vector3.zero;
            var found = 0;
            foreach (var poke in FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None))
            {
                if (!poke.isActiveAndEnabled || poke.handedness == InteractorHandedness.None)
                    continue;
                sum += poke.attachTransform != null ? poke.attachTransform.position : poke.transform.position;
                found++;
            }
            if (found == 0)
                return false;

            var handsCenter = sum / found;
            var offset = handsCenter - head.position;
            var forward = FlatForward();

            var config = Session.Current;
            // A superfície das teclas vai para onde estão as pontas dos dedos.
            config.keyboardDistance = Vector3.Dot(offset, forward);
            config.keyboardBelowHead = -offset.y;
            Session.NotifyChanged();
            PlaceInFront();
            return true;
        }

        /// <summary>Sobe (positivo) ou desce (negativo) o teclado.</summary>
        public void NudgeHeight(int steps)
        {
            Session.Current.keyboardBelowHead -= steps * m_NudgeStep;
            Session.NotifyChanged();
            ApplyFromSessionKeepingFacing();
        }

        /// <summary>Afasta (positivo) ou aproxima (negativo) o teclado.</summary>
        public void NudgeDistance(int steps)
        {
            Session.Current.keyboardDistance += steps * m_NudgeStep;
            Session.NotifyChanged();
            ApplyFromSessionKeepingFacing();
        }

        void ApplyFromSessionKeepingFacing()
        {
            if (head == null)
                return;
            // Mantém a direção atual do teclado; só ajusta altura e distância.
            var forward = target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f)
                forward = FlatForward();
            forward.Normalize();
            var config = Session.Current;
            var headFlat = head.position;
            target.SetPositionAndRotation(
                headFlat + forward * config.keyboardDistance + Vector3.down * config.keyboardBelowHead,
                Quaternion.LookRotation(forward, Vector3.up));
        }

        Vector3 FlatForward()
        {
            var forward = head != null ? head.forward : Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
        }
    }
}
