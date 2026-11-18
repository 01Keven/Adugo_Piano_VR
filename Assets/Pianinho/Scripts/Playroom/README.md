# Playroom: setup rápido

Copie a pasta `Playroom` para `Assets/Playroom`. O `Playroom.asmdef` referencia só o XR Interaction Toolkit e **não depende do Pianinho**, então os dois podem evoluir separados. O código do Pianinho continua no assembly padrão e não precisa de nenhuma mudança.

Convenções iguais às do Pianinho: campos `m_`, propriedades em camelCase, comentários em português, namespace plano (`Playroom`). As subpastas servem só para organizar.

## Scripts

| Pasta | Script | Função |
|---|---|---|
| Core | `GrabEvents` | Adaptador do XR Grab Interactable (eventos Grabbed/Released e isHeld). |
| Fall | `RespawnOnFall` | Objeto que cai fora do salão volta ao ponto inicial. |
| Fall | `ImpactSound` | Som 3D de impacto proporcional à velocidade. |
| Float | `FloatingBody` | Balões: sem gravidade, sobem até um teto macio e balançam. |
| Liquid | `WaterVolume` | Empuxo e arrasto para tudo que entra no trigger do aquário. |
| Liquid | `BuoyantBody` | Opcional: define se o objeto flutua ou afunda. |
| Audio | `SpatialAudioSource` | Configura um AudioSource para som 3D. |
| Ragdoll | `PlushRagdoll` | Estabiliza o ragdoll e amortece os membros ao segurar. |

## Convivência com o Pianinho

- O `GameplayInputMode` esconde o `NearFarInteractor` e os raios, e desliga a locomoção, **enquanto uma partida está rodando**. Se o salão e o piano estiverem na mesma cena, nada será pegável durante a partida. Isso é o esperado, e tudo volta no `ExitGameplay`.
- O `PokeOnlyFilter` vale só para as teclas (`XRSimpleInteractable` delas). Os objetos do salão usam `XRGrabInteractable` normal e não são afetados.
- Os dois usam o mesmo rig XR. Para pegar objetos, o rig precisa ter o **Near-Far Interactor** ativo (o do Starter Assets já tem).

## Receitas

**Queda (bola/bloco):** Rigidbody (massa 0.3), collider com Physics Material (Bounciness 0.5), XR Grab Interactable (Velocity Tracking, Throw On Detach ligado), `RespawnOnFall`, `ImpactSound` + AudioSource.

**Balão:** Rigidbody (massa 0.05, Linear Damping 1.5, Angular Damping 2), Sphere Collider, XR Grab Interactable (Throw On Detach desligado), `FloatingBody`. Ajuste `Ceiling Y` para o teto do salão.

**Aquário:** vidro com colliders normais; água = cubo interno com Box Collider **Is Trigger** + `WaterVolume`. Sem `BuoyantBody` o objeto afunda devagar; com `BuoyantBody` (Float Factor 1.3) ele sobe até a superfície.

**Som 3D:** AudioSource + `SpatialAudioSource` num objeto do cenário. Em Project Settings > Audio, escolha um Spatializer Plugin se o headset tiver um.

**Ragdoll (onça):** modelo com rig; `GameObject > 3D Object > Ragdoll...`; no osso raiz coloque XR Grab Interactable (Velocity Tracking) + `PlushRagdoll` (+ `ImpactSound`). Os colliders dos ossos filhos servem de pegada.

## Notas

- APIs do Unity 6: `linearVelocity`, `linearDamping`, `angularDamping`.
- Apague os `Balloon.cs`, `ToyRespawner.cs` e `WaterVolume.cs` antigos da raiz do projeto.
