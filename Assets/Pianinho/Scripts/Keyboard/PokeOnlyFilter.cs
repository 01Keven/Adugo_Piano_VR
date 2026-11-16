using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>
    /// Deixa só o toque direto (poke) interagir com a tecla. Raios, laser e pegar com o grip
    /// são ignorados: o Pianinho é tátil, a tecla precisa ser encostada.
    /// </summary>
    public sealed class PokeOnlyFilter : IXRHoverFilter, IXRSelectFilter
    {
        public static readonly PokeOnlyFilter Instance = new PokeOnlyFilter();

        public bool canProcess => true;

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => interactor is XRPokeInteractor;

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => interactor is XRPokeInteractor;
    }
}
