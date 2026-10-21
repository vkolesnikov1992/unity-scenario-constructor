using UnityEngine;

namespace TrainingScenarios.Demo.Presentation
{
    public sealed class PanelToggle : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panel;

        public void Toggle()
        {
            if (_panel)
            {
                _panel.SetActive(!_panel.activeSelf);
            }
        }

        public void Close()
        {
            if (_panel)
            {
                _panel.SetActive(false);
            }
        }
    }
}
