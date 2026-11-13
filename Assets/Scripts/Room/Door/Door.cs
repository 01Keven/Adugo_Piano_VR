using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// PORTA QUE ABRE COM A MAO
//
// Como montar:
// 1. Na folha da porta (o objeto que gira): Box Collider, Rigidbody, Hinge Joint,
//    XR Grab Interactable e este script.
// 2. No Hinge Joint, ajuste so duas coisas no Inspector:
//      - Anchor: na borda onde ficam as dobradicas
//      - Axis:   (0, 1, 0) para girar em torno do eixo vertical
//    Deixe "Connected Body" vazio (a porta fica presa ao mundo).
// 3. Deixe uma folga de 1 a 2 cm entre a porta e o batente/chao, senao os
//    colliders se prendem e a porta trava.
//
// Limites de angulo, mola de fechamento e modo de agarrar sao configurados
// por este script no Awake.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(XRGrabInteractable))]
public class Door : MonoBehaviour
{
    [Header("Abertura")]
    [Tooltip("Limite de um lado, em graus. 0 = posicao em que a porta esta na cena (fechada). Use -110 para um vai-e-vem.")]
    [SerializeField] float anguloMinimo = 0f;
    [Tooltip("Angulo maximo de abertura, em graus. Se abrir para o lado errado, troque o Axis do Hinge Joint para (0, -1, 0).")]
    [SerializeField] float anguloMaximo = 110f;

    [Header("Fechar sozinha")]
    [SerializeField] bool fecharSozinha = true;
    [Tooltip("Forca da mola que puxa a porta de volta.")]
    [SerializeField] float forcaDaMola = 3f;
    [Tooltip("Quanto maior, mais devagar e suave ela fecha.")]
    [SerializeField] float amortecimento = 1f;

    [Header("Som (opcional)")]
    [Tooltip("AudioSource com um rangido em loop. O volume acompanha a velocidade da porta.")]
    [SerializeField] AudioSource somDeRangido;
    [Tooltip("Velocidade (graus/s) em que o rangido chega ao volume maximo.")]
    [SerializeField] float velocidadeParaVolumeMaximo = 90f;

    HingeJoint dobradica;

    void Awake()
    {
        dobradica = GetComponent<HingeJoint>();

        dobradica.useLimits = true;
        dobradica.limits = new JointLimits { min = anguloMinimo, max = anguloMaximo };

        dobradica.useSpring = fecharSozinha;
        dobradica.spring = new JointSpring
        {
            spring = forcaDaMola,
            damper = amortecimento,
            // 0 e a posicao em que a porta foi colocada na cena, ou seja, fechada.
            targetPosition = Mathf.Clamp(0f, anguloMinimo, anguloMaximo)
        };

        // Velocity Tracking faz a porta "tentar seguir" a mao usando fisica,
        // entao a dobradica continua mandando no movimento.
        var agarrar = GetComponent<XRGrabInteractable>();
        agarrar.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        agarrar.trackRotation = false;   // quem gira a porta e a dobradica, nao a mao
        agarrar.useDynamicAttach = true; // agarra no ponto onde a mao tocou (macaneta, borda...)
        agarrar.throwOnDetach = false;

        if (somDeRangido != null)
        {
            somDeRangido.loop = true;
            somDeRangido.spatialBlend = 1f; // som 3D
            somDeRangido.volume = 0f;
        }
    }

    void Update()
    {
        if (somDeRangido == null)
            return;

        float velocidade = Mathf.Abs(dobradica.velocity); // graus por segundo
        float volumeAlvo = Mathf.InverseLerp(5f, velocidadeParaVolumeMaximo, velocidade);
        somDeRangido.volume = Mathf.MoveTowards(somDeRangido.volume, volumeAlvo, Time.deltaTime * 4f);

        if (somDeRangido.volume > 0.01f && !somDeRangido.isPlaying)
            somDeRangido.Play();
        else if (somDeRangido.volume <= 0.01f && somDeRangido.isPlaying)
            somDeRangido.Pause();
    }
}