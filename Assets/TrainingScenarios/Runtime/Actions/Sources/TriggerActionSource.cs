using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Sources
{
    [RequireComponent(typeof(Collider))]
    public sealed class TriggerActionSource : ActionSourceBehaviour
    {
        [SerializeField]
        private LayerMask _acceptedLayers = ~0;
        [SerializeField]
        private bool _raiseExitAction;

        private void OnTriggerEnter(Collider other)
        {
            if (IsLayerAccepted(other.gameObject.layer))
            {
                RaiseAction(ScenarioActionTypes.TriggerEntered);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_raiseExitAction && IsLayerAccepted(other.gameObject.layer))
            {
                RaiseAction(ScenarioActionTypes.TriggerExited);
            }
        }

        private bool IsLayerAccepted(int layer)
        {
            return (_acceptedLayers.value & (1 << layer)) != 0;
        }
    }
}
