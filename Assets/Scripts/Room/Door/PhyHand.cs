using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// MAO COM COLISAO (para empurrar objetos sem agarrar)
//
// O controle do XR Origin nao tem colisao fisica: ele atravessa tudo. Este
// script cria uma "mao fisica" que segue o controle e empurra o que encostar:
// a porta, os baloes, os objetos caindo, a pelucia. Ela tambem faz o splash
// ao entrar na agua do aquario.
//
// Como montar (repita para a mao esquerda e a direita):
// 1. Crie um objeto vazio na raiz da cena (NAO como filho do controle),
//    por exemplo "MaoFisicaDireita".
// 2. Adicione Sphere Collider (Radius 0.04, Is Trigger desligado), Rigidbody
//    e este script.
// 3. Em "Alvo", arraste o objeto do controle (ex.: Right Controller).
// 4. Em "Interator", arraste o Near-Far Interactor (ou XR Direct Interactor)
//    dessa mesma mao.
//
// Enquanto a mao segura alguma coisa, a colisao dela e desligada para nao
// brigar com o objeto que esta sendo segurado.
[RequireComponent(typeof(Rigidbody))]
public class MaoFisica : MonoBehaviour
{
    [Tooltip("Transform do controle que esta mao deve seguir.")]
    [SerializeField] Transform alvo;
    [Tooltip("Interator desta mao. Serve para desligar a colisao enquanto ela segura algo.")]
    [SerializeField] XRBaseInteractor interator;
    [Tooltip("Segundos sem colisao depois de soltar, para nao dar um tranco no objeto solto.")]
    [SerializeField] float esperaAposSoltar = 0.25f;
    [Tooltip("Se a mao ficar mais longe que isto do controle (teleporte, perda de rastreio), ela pula direto para ele.")]
    [SerializeField] float distanciaParaPular = 0.5f;

    Rigidbody corpo;
    Collider[] colisores;
    bool colisoresLigados = true;
    float religarEm;

    void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        corpo.isKinematic = true; // a mao empurra, mas nada empurra a mao
        corpo.useGravity = false;
        corpo.interpolation = RigidbodyInterpolation.Interpolate;
        corpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        colisores = GetComponentsInChildren<Collider>();

        // Precisa ficar solta na cena: como filha do controle ela se moveria
        // por teleporte a cada quadro e empurraria os objetos aos trancos.
        transform.SetParent(null, true);
    }

    void FixedUpdate()
    {
        if (alvo == null)
            return;

        bool segurando = interator != null && interator.hasSelection;
        if (segurando)
            religarEm = Time.time + esperaAposSoltar;
        LigarColisores(!segurando && Time.time >= religarEm);

        float distancia = Vector3.Distance(corpo.position, alvo.position);
        if (distancia > distanciaParaPular)
        {
            corpo.position = alvo.position;
            corpo.rotation = alvo.rotation;
        }
        else
        {
            // MovePosition faz o corpo cinematico percorrer o caminho com velocidade,
            // e e essa velocidade que empurra os outros objetos de forma suave.
            corpo.MovePosition(alvo.position);
            corpo.MoveRotation(alvo.rotation);
        }
    }

    void LigarColisores(bool ligar)
    {
        if (ligar == colisoresLigados)
            return;

        colisoresLigados = ligar;
        foreach (Collider colisor in colisores)
            colisor.enabled = ligar;
    }
}
