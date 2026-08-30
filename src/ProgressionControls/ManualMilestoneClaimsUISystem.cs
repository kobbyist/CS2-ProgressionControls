using Colossal.UI.Binding;
using Game.UI;

namespace Kobbyist.ProgressionControls
{
    internal partial class ManualMilestoneClaimsUISystem : UISystemBase
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
                "manualMilestoneClaimsState",
                ManualMilestoneClaimsViewStateJson.Serialize(
                    ManualMilestoneClaimsViewState.Empty));

            AddBinding(m_StateBinding);
            AddBinding(new TriggerBinding<int>(
                BindingGroup,
                "claimMilestone",
                index => m_ProgressionControlSystem
                    .RequestManualMilestoneClaim(index)));
            AddBinding(new TriggerBinding<string>(
                BindingGroup,
                "resolveManualMilestoneClaims",
                decision => m_ProgressionControlSystem
                    .RequestManualMilestoneClaimsDecision(decision)));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var state = m_ProgressionControlSystem == null
                ? ManualMilestoneClaimsViewState.Empty
                : m_ProgressionControlSystem
                    .GetManualMilestoneClaimsViewState();
            m_StateBinding.Update(
                ManualMilestoneClaimsViewStateJson.Serialize(state));
        }
    }
}
