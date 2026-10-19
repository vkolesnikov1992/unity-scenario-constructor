using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Sources
{
    [RequireComponent(typeof(Collider))]
    public sealed class CollisionActionSource : ActionSourceBehaviour
    {
        [SerializeField]
        private LayerMask _acceptedLayers = ~0;
        [SerializeField]
        private bool _raiseExitAction;

        private void OnCollisionEnter(Collision collision)
        {
            if (IsLayerAccepted(collision.gameObject.layer))
            {
                RaiseAction(ScenarioActionTypes.CollisionEntered);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (_raiseExitAction && IsLayerAccepted(collision.gameObject.layer))
            {
                RaiseAction(ScenarioActionTypes.CollisionExited);
            }
        }

        private bool IsLayerAccepted(int layer)
        {
            return (_acceptedLayers.value & (1 << layer)) != 0;
        }
    }
}
