using System;
using System.Collections.Generic;
using System.Globalization;
using Colossal.UI.Binding;
using Game.UI;

namespace Kobbyist.ProgressionControls
{
    public partial class ProgressionWidgetUISystem : UISystemBase
    {
        internal const string BindingGroup =
            "Kobbyist.ProgressionControls";

        private ProgressionControlSystem m_ProgressionSystem;
        private ValueBinding<bool> m_VisibleBinding;
        private ValueBinding<bool> m_ReadyBinding;
        private ValueBinding<bool> m_ActiveBinding;
        private ValueBinding<int> m_CurrentPopulationBinding;
        private ValueBinding<int> m_HistoricalMaximumBinding;
        private ValueBinding<int> m_PopulationToResumeBinding;
        private ValueBinding<string> m_MostRecentAwardBinding;
        private ValueBinding<int> m_PositionXBinding;
        private ValueBinding<int> m_PositionYBinding;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ProgressionSystem =
                World.GetOrCreateSystemManaged<
                    ProgressionControlSystem>();

            m_VisibleBinding = AddValueBinding(
                "Visible",
                initialValue: true);
            m_ReadyBinding = AddValueBinding(
                "Ready",
                initialValue: false);
            m_ActiveBinding = AddValueBinding(
                "Active",
                initialValue: false);
            m_CurrentPopulationBinding = AddValueBinding(
                "CurrentPopulation",
                initialValue: 0);
            m_HistoricalMaximumBinding = AddValueBinding(
                "HistoricalMaximumPopulation",
                initialValue: 0);
            m_PopulationToResumeBinding = AddValueBinding(
                "PopulationToResume",
                initialValue: 0);
            m_MostRecentAwardBinding = AddValueBinding(
                "MostRecentPopulationXpAward",
                initialValue: "0");
            m_PositionXBinding = AddValueBinding(
                "PositionX",
                initialValue: 0);
            m_PositionYBinding = AddValueBinding(
                "PositionY",
                initialValue: 0);

            AddBinding(
                new TriggerBinding<string>(
                    BindingGroup,
                    "PositionChanged",
                    HandlePositionChanged));
        }

        protected override void OnUpdate()
        {
            var settings = Mod.Settings;
            if (settings == null)
            {
                UpdateIfChanged(m_VisibleBinding, false);
                return;
            }

            UpdateIfChanged(
                m_VisibleBinding,
                settings.ShowStatusWidget);
            UpdateIfChanged(
                m_PositionXBinding,
                settings.WidgetPositionX);
            UpdateIfChanged(
                m_PositionYBinding,
                settings.WidgetPositionY);

            if (!settings.ShowStatusWidget)
            {
                return;
            }

            var snapshot =
                m_ProgressionSystem.GetWidgetSnapshot();
            UpdateIfChanged(m_ReadyBinding, snapshot.Ready);
            UpdateIfChanged(m_ActiveBinding, snapshot.Active);
            UpdateIfChanged(
                m_CurrentPopulationBinding,
                snapshot.CurrentPopulation);
            UpdateIfChanged(
                m_HistoricalMaximumBinding,
                snapshot.HistoricalMaximumPopulation);
            UpdateIfChanged(
                m_PopulationToResumeBinding,
                snapshot.PopulationToResume);
            UpdateIfChanged(
                m_MostRecentAwardBinding,
                snapshot.MostRecentPopulationXpAward.ToString(
                    CultureInfo.InvariantCulture));
        }

        private ValueBinding<T> AddValueBinding<T>(
            string name,
            T initialValue)
        {
            var binding = new ValueBinding<T>(
                BindingGroup,
                name,
                initialValue);
            AddBinding(binding);
            return binding;
        }

        private static void UpdateIfChanged<T>(
            ValueBinding<T> binding,
            T value)
        {
            if (!EqualityComparer<T>.Default.Equals(
                binding.value,
                value))
            {
                binding.Update(value);
            }
        }

        private static void HandlePositionChanged(
            string serializedPosition)
        {
            var settings = Mod.Settings;
            if (settings == null ||
                string.IsNullOrWhiteSpace(serializedPosition))
            {
                return;
            }

            var parts = serializedPosition.Split(',');
            if (parts.Length != 2 ||
                !int.TryParse(
                    parts[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var positionX) ||
                !int.TryParse(
                    parts[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var positionY))
            {
                Mod.Log.Warn(
                    "Ignored an invalid widget position update");
                return;
            }

            if (settings.WidgetPositionX == positionX &&
                settings.WidgetPositionY == positionY)
            {
                return;
            }

            settings.WidgetPositionX = positionX;
            settings.WidgetPositionY = positionY;
            settings.ApplyAndSave();
        }
    }
}
