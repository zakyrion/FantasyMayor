using DefaultEcs;
using DefaultECSExtensions;
using Modules.Turn.Events;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Modules.MainUI.EndTurn.Views
{
    /// <summary>
    ///     View for the end-turn button. Owns the UIDocument's button, emits the turn-commit event on click,
    ///     and switches between Ready and Processing looks. Holds no game logic beyond raising the one-frame
    ///     event — the Processing state is driven from outside by EndTurnSystem. The full-screen root stays
    ///     click-through so map clicks pass through; only the button itself blocks clicks (mirrors the generator
    ///     UI and HexInfoPanelView picking rules).
    /// </summary>
    public sealed class EndTurnView : MonoBehaviour
    {
        private const string ClusterName = "TurnCluster";
        private const string ButtonName = "EndTurnButton";
        private const string TurnNumberName = "TurnNumber";
        private const string ProcessingClass = "is-processing";

        // Labels are uppercased in DATA — USS has no text-transform (GENERAL_UI_STYLE.md §9).
        private const string ReadyLabel = "ЗАВЕРШИТИ ХІД";
        private const string ProcessingLabel = "ОБРАХУНОК ХОДУ…";

        [SerializeField] private UIDocument _document;

        private World _world;
        private VisualElement _cluster;
        private Button _button;
        private Label _turnNumber;
        private bool _processing;
        private bool _cached;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

        private void Start()
        {
            EnsureCached();
            _button.clicked += OnClicked;
            ApplyRaycastTransparent();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.clicked -= OnClicked;
        }

        /// <summary>
        ///     Reveals the whole turn cluster (card + "Хід N" + button). It spawns hidden so it does not flash
        ///     during map creation. Toggles the cluster element only — the UIDocument is shared with the hex info
        ///     panel, so touching the document root here would blank the whole Main UI (mirrors HexInfoPanelView
        ///     toggling its own card).
        /// </summary>
        public void Show()
        {
            EnsureCached();
            _cluster.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            EnsureCached();
            _cluster.style.display = DisplayStyle.None;
        }

        /// <summary>Sets the turn-number label ("Хід N"). Driven by EndTurnSystem from TurnCountComponent.</summary>
        public void SetTurnNumber(int turnNumber)
        {
            EnsureCached();
            _turnNumber.text = $"Хід {turnNumber}";
        }

        /// <summary>
        ///     Ready ↔ Processing. Processing is non-interactive and relabeled — it mirrors the engine's
        ///     re-entry guard while a turn is being computed.
        /// </summary>
        public void SetProcessing(bool processing)
        {
            EnsureCached();
            if (_processing == processing)
                return;

            _processing = processing;
            _button.text = processing ? ProcessingLabel : ReadyLabel;
            _button.SetEnabled(!processing);
            _button.EnableInClassList(ProcessingClass, processing);
        }

        private void OnClicked()
        {
            // Self-guard; the engine also ignores NextTurnEvent while a turn runs (defence in depth).
            if (_processing)
                return;

            var entity = _world.CreateEntity();
            entity.Set(new NextTurnEvent());
            entity.Set(new EventTag());
        }

        private void ApplyRaycastTransparent()
        {
            foreach (var element in _document.rootVisualElement.Query(className: "raycast-transparent").ToList())
                element.EnablePicking(false);
        }

        private void EnsureCached()
        {
            if (_cached)
                return;

            var root = _document.rootVisualElement;
            _cluster = root.Q<VisualElement>(ClusterName);
            _button = root.Q<Button>(ButtonName);
            _turnNumber = root.Q<Label>(TurnNumberName);
            _cached = true;
        }
    }
}
