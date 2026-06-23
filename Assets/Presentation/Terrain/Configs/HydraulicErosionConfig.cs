using UnityEngine;

namespace Presentation.Terrain.Data
{
    [CreateAssetMenu(fileName = "HydraulicErosionConfig", menuName = "FantasyMayor/Terrain/Hydraulic Erosion Config")]
    public class HydraulicErosionConfig : ScriptableObject
    {
        [Tooltip("Вмикає або повністю вимикає застосування hydraulic erosion для terrain.")]
        [SerializeField] private bool _enableHydraulicErosion;

        [Tooltip("Кількість ітерацій симуляції ерозії. Більше значення дає сильніший і детальніший ефект, але збільшує час обчислення.")]
        [Min(1)]
        [SerializeField] private int _hydraulicIterations = 25;

        [Tooltip("Кількість води, яка додається на кожній ітерації як 'дощ'. Впливає на загальну інтенсивність ерозії.")]
        [Min(0f)]
        [SerializeField] private float _hydraulicRainAmount = 0.01f;

        [Tooltip("Частка води, яка перетікає між сусідніми вершинами за ітерацію. Більше значення прискорює рух води.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hydraulicFlowRate = 0.5f;

        [Tooltip("Частка води, яка випаровується на кожній ітерації. Більше значення швидше зменшує об'єм води.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hydraulicEvaporation = 0.08f;

        [Tooltip("Максимальна кількість осаду, яку потік води може переносити. Впливає на те, скільки матеріалу може бути знято з поверхні.")]
        [Min(0f)]
        [SerializeField] private float _hydraulicSedimentCapacity = 1.5f;

        [Tooltip("Швидкість, з якою вода розмиває поверхню, коли ще може переносити осад. Більше значення означає агресивнішу ерозію.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hydraulicErosionRate = 0.2f;

        [Tooltip("Швидкість, з якою вода відкладає осад, коли її поточної місткості вже недостатньо. Більше значення сильніше згладжує рельєф відкладенням.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hydraulicDepositionRate = 0.15f;

        public bool EnableHydraulicErosion => _enableHydraulicErosion;
        public int HydraulicIterations => Mathf.Max(1, _hydraulicIterations);
        public float HydraulicRainAmount => Mathf.Max(0f, _hydraulicRainAmount);
        public float HydraulicFlowRate => Mathf.Clamp01(_hydraulicFlowRate);
        public float HydraulicEvaporation => Mathf.Clamp01(_hydraulicEvaporation);
        public float HydraulicSedimentCapacity => Mathf.Max(0f, _hydraulicSedimentCapacity);
        public float HydraulicErosionRate => Mathf.Clamp01(_hydraulicErosionRate);
        public float HydraulicDepositionRate => Mathf.Clamp01(_hydraulicDepositionRate);
    }
}
