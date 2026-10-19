using UnityEngine;
using UnityEngine.UI;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Sources
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonActionSource : ActionSourceBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            RaiseAction(ScenarioActionTypes.ButtonClicked);
        }
    }
}
