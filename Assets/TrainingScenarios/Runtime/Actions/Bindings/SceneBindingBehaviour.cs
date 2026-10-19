using UnityEngine;

namespace TrainingScenarios.Runtime.Actions.Bindings
{
    public abstract class SceneBindingBehaviour : MonoBehaviour, ISceneBinding
    {
        [SerializeField]
        private string _bindingId;

        public string BindingId => _bindingId;
    }
}
