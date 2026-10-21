using UnityEngine;
using TrainingScenarios.Runtime.Core.Steps;

namespace TrainingScenarios.Demo.Presentation
{
    public sealed class WorldStepMarker : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField]
        private string _stepId;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private Color _inactiveColor = new Color(0.25f, 0.28f, 0.34f, 1f);
        [SerializeField]
        private Color _activeColor = new Color(0.1f, 0.55f, 1f, 1f);
        [SerializeField]
        private Color _completedColor = new Color(0.1f, 0.9f, 0.35f, 1f);
        [SerializeField]
        private Color _failedColor = new Color(1f, 0.18f, 0.18f, 1f);
        [SerializeField]
        private Color _skippedColor = new Color(0.38f, 0.38f, 0.42f, 1f);

        private MaterialPropertyBlock _propertyBlock;

        public string StepId => _stepId;

        public void ApplyOrder(int order)
        {
            foreach (var label in GetComponentsInChildren<TextMesh>(true))
            {
                label.text = ReplaceOrderPrefix(label.text, order);
            }
        }

        public void Apply(StepStatus status)
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            var color = GetColor(status);
            color.a = 1f;
            foreach (var targetRenderer in _renderers)
            {
                if (!targetRenderer)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                _propertyBlock.SetColor(EmissionColorId, color * (status == StepStatus.Active ? 1.4f : 0.25f));
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private Color GetColor(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Active:
                    return _activeColor;
                case StepStatus.Completed:
                    return _completedColor;
                case StepStatus.Failed:
                    return _failedColor;
                case StepStatus.Skipped:
                    return _skippedColor;
                default:
                    return _inactiveColor;
            }
        }

        private static string ReplaceOrderPrefix(string text, int order)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var lineBreakIndex = text.IndexOf('\n');
            var currentPrefix = lineBreakIndex >= 0
                ? text.Substring(0, lineBreakIndex)
                : text;
            var suffixIndex = currentPrefix.IndexOf('-');
            var suffix = suffixIndex >= 0
                ? currentPrefix.Substring(suffixIndex)
                : string.Empty;
            var remainingText = lineBreakIndex >= 0
                ? text.Substring(lineBreakIndex)
                : string.Empty;

            return order.ToString("00") + suffix + remainingText;
        }
    }
}
