using EcsExtensions;
using Friflo.Engine.ECS;
using Modules.Turn.Events;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Presentation.UI.MainHud.TurnPanel.Views
{
    /// <summary>
    ///     View for the End Turn button AND the owner of the shared bottom-panel shell reveal. Owns the
    ///     PanelRenderer's button + turn number, emits the turn-commit event on click, and switches between Ready
    ///     and Processing looks. The turn corner is the always-present part of the bottom panel, so this view
    ///     toggles the whole BottomPanel shell (Show/Hide) — never the panel root (that would blank the whole
    ///     Main UI). The CONTEXT sub-panel content is swapped independently by HexInfoPanelView. Holds no game
    ///     logic beyond raising the one-frame event — the Processing state is driven by TurnPanelViewSystem. The
    ///     full-screen root stays click-through; only the button blocks clicks (mirrors the generator UI rules).
    ///     PanelRenderer builds its tree asynchronously, so the button is hooked and state replayed in the reload
    ///     callback (re-hooked on every reload, since a reload recreates the button).
    /// </summary>
    public sealed class TurnPanelView : MonoBehaviour
    {
        private const string PanelName = "BottomPanel";
        private const string ButtonName = "EndTurnButton";
        private const string TurnNumberName = "TurnNumber";
        private const string ApCurrentName = "TurnApCurrent";
        private const string ApNextName = "TurnApNext";
        private const string ProcessingClass = "is-processing";

        // Labels are uppercased in DATA — USS has no text-transform (GENERAL_UI_STYLE.md §9).
        private const string ReadyLabel = "ЗАВЕРШИТИ ХІД";
        private const string ProcessingLabel = "ОБРАХУНОК ХОДУ…";

        [SerializeField] private PanelRenderer _renderer;

        private EntityStore _world;
        private VisualElement _root;
        private VisualElement _panel;
        private Button _button;
        private Label _turnNumber;
        private Label _apCurrentLabel;
        private Label _apNextLabel;
        private bool _cached;

        // Logical state replayed on (re)bind. Default hidden so the shell does not flash during MapCreation;
        // TurnPanelViewSystem re-pushes visibility / turn / processing every frame in Gameplay.
        private bool _visible;
        private bool _processing;
        private bool _turnValueSet;
        private int _turnValue;
        private bool _apCurrentSet;
        private int _apCurrentValue;
        private bool _apNextSet;
        private int _apNextValue;

        [Inject]
        public void Construct(EntityStore world)
        {
            _world = world;
        }

        private void OnEnable()
        {
            // PanelRenderer delivers the (re)built root via this callback — for both the initial build and a
            // late registration (it invokes a pending callback if the tree already exists) — so binding never
            // races the async tree build. The callback also re-fires on a live UXML reload.
            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            _renderer.UnregisterUIReloadCallback(OnUiReloaded);
            UnhookButton();
        }

        // PanelRenderer (re)built its visual tree: re-query elements, re-hook the (recreated) button, reapply
        // picking, and replay the last state.
        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            UnhookButton();
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            _button.clicked += OnClicked;
            ApplyRaycastTransparent();
            ApplyState();
        }

        /// <summary>
        ///     Reveals the whole bottom panel (turn sub-panel + context sub-panel). It spawns hidden so it does
        ///     not flash during map creation. Toggles the BottomPanel shell only — the PanelRenderer is shared, so
        ///     touching the panel root here would blank the whole Main UI. The context content inside is swapped
        ///     separately by HexInfoPanelView.
        /// </summary>
        public void Show()
        {
            _visible = true;
            if (TryCache())
                _panel.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _visible = false;
            if (TryCache())
                _panel.style.display = DisplayStyle.None;
        }

        /// <summary>Sets the turn-number label ("Хід N"). Driven by TurnPanelViewSystem from TurnCountComponent.</summary>
        public void SetTurnNumber(int turnNumber)
        {
            _turnValueSet = true;
            _turnValue = turnNumber;
            if (TryCache())
                _turnNumber.text = $"Хід {turnNumber}";
        }

        /// <summary>
        ///     «ДІЇ ЗАРАЗ» tile — the Mayor's live AP. Stateful guard: skips the UI write when the value is
        ///     unchanged, so the per-frame push from TurnPanelViewSystem does not redraw the label every frame.
        /// </summary>
        public void SetActionPointsCurrent(int value)
        {
            if (_apCurrentSet && _apCurrentValue == value)
                return;

            _apCurrentSet = true;
            _apCurrentValue = value;
            if (TryCache())
                _apCurrentLabel.text = value.ToString();
        }

        /// <summary>«НАСТ. ХІД» tile — the Mayor's per-turn AP restore amount. Same unchanged-value guard.</summary>
        public void SetActionPointsNext(int value)
        {
            if (_apNextSet && _apNextValue == value)
                return;

            _apNextSet = true;
            _apNextValue = value;
            if (TryCache())
                _apNextLabel.text = value.ToString();
        }

        /// <summary>
        ///     Ready ↔ Processing. Processing is non-interactive and relabeled — it mirrors the engine's
        ///     re-entry guard while a turn is being computed.
        /// </summary>
        public void SetProcessing(bool processing)
        {
            if (_processing == processing)
                return;

            _processing = processing;
            if (TryCache())
                ApplyProcessing();
        }

        private void OnClicked()
        {
            // Self-guard; the engine also ignores NextTurnEvent while a turn runs (defence in depth).
            if (_processing)
                return;

            _world.CreateEvent(new NextTurnEvent());
        }

        private void ApplyState()
        {
            _panel.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_turnValueSet)
                _turnNumber.text = $"Хід {_turnValue}";
            if (_apCurrentSet)
                _apCurrentLabel.text = _apCurrentValue.ToString();
            if (_apNextSet)
                _apNextLabel.text = _apNextValue.ToString();
            ApplyProcessing();
        }

        private void ApplyProcessing()
        {
            _button.text = _processing ? ProcessingLabel : ReadyLabel;
            _button.SetEnabled(!_processing);
            _button.EnableInClassList(ProcessingClass, _processing);
        }

        private void ApplyRaycastTransparent()
        {
            foreach (var element in _root.Query(className: "raycast-transparent").ToList())
                element.EnablePicking(false);
        }

        private void UnhookButton()
        {
            if (_button != null)
                _button.clicked -= OnClicked;
        }

        private bool TryCache()
        {
            EnsureCached();
            return _cached;
        }

        private void EnsureCached()
        {
            if (_cached)
                return;

            // PanelRenderer hands the root via the reload callback; until then there is nothing to bind.
            if (_root == null)
                return;

            _panel = _root.Q<VisualElement>(PanelName);
            _button = _root.Q<Button>(ButtonName);
            _turnNumber = _root.Q<Label>(TurnNumberName);
            _apCurrentLabel = _root.Q<Label>(ApCurrentName);
            _apNextLabel = _root.Q<Label>(ApNextName);
            _cached = true;
        }
    }
}
