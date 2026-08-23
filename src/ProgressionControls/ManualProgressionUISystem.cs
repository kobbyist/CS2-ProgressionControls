using Colossal.UI.Binding;
using Game.UI;

namespace Kobbyist.ProgressionControls
{
    internal partial class ManualProgressionUISystem : UISystemBase
    {
        internal const string BindingGroup =
            "Kobbyist.ProgressionControls";

        private ProgressionControlSystem m_ProgressionControlSystem;
        private ValueBinding<string> m_StateBinding;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ProgressionControlSystem =
                World.GetOrCreateSystemManaged<ProgressionControlSystem>();
            m_StateBinding = new ValueBinding<string>(
                BindingGroup,
                "manualProgressionState",
                ManualProgressionViewStateJson.Serialize(
                    ManualProgressionViewState.Empty));

            AddBinding(m_StateBinding);
            AddBinding(new TriggerBinding<int>(
                BindingGroup,
                "claimMilestone",
                index => m_ProgressionControlSystem
                    .RequestManualMilestoneClaim(index)));
            AddBinding(new TriggerBinding<string>(
                BindingGroup,
                "resolveManualProgression",
                decision => m_ProgressionControlSystem
                    .RequestManualProgressionDecision(decision)));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var state = m_ProgressionControlSystem == null
                ? ManualProgressionViewState.Empty
                : m_ProgressionControlSystem
                    .GetManualProgressionViewState();
            m_StateBinding.Update(
                ManualProgressionViewStateJson.Serialize(state));
        }
    }
}
