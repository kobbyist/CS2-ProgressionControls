using Colossal.UI.Binding;
using Game.UI;

namespace Kobbyist.ProgressionControls
{
    internal partial class ManualMilestoneClaimsUISystem : UISystemBase
    {
        internal const string BindingGroup =
            "Kobbyist.ProgressionControls";

        private ProgressionControlSystem m_ProgressionControlSystem;
        private ValueBinding<bool> m_AvailabilityBinding;
        private ValueBinding<string> m_StateBinding;
        private ManualMilestoneClaimsViewKey m_LastViewKey;
        private bool m_HasLastViewKey;
        private bool m_PanelOpen;
        private bool m_StateUpdateRequested;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ProgressionControlSystem =
                World.GetOrCreateSystemManaged<ProgressionControlSystem>();
            m_AvailabilityBinding = new ValueBinding<bool>(
                BindingGroup,
                "manualMilestoneClaimsAvailable",
                false);
            m_StateBinding = new ValueBinding<string>(
                BindingGroup,
                "manualMilestoneClaimsState",
                ManualMilestoneClaimsViewStateJson.Serialize(
                    ManualMilestoneClaimsViewState.Empty));
            m_LastViewKey = default;
            m_HasLastViewKey = true;

            AddBinding(m_AvailabilityBinding);
            AddBinding(m_StateBinding);
            AddBinding(new TriggerBinding<bool>(
                BindingGroup,
                "setManualMilestoneClaimsPanelOpen",
                open =>
                {
                    m_PanelOpen = open;
                    m_StateUpdateRequested =
                        m_StateUpdateRequested || open;
                }));
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

            var viewKey = m_ProgressionControlSystem == null
                ? default
                : m_ProgressionControlSystem
                    .GetManualMilestoneClaimsViewKey(
                        includePanelState: m_PanelOpen ||
                            m_StateUpdateRequested);
            m_AvailabilityBinding.Update(viewKey.Available);
            var dialogRequiresState = viewKey.Dialog !=
                ManualMilestoneClaimsDialogKind.None;
            var stateChanged = !m_HasLastViewKey ||
                !viewKey.Equals(m_LastViewKey);
            var shouldPublish = m_StateUpdateRequested ||
                (stateChanged &&
                    (m_PanelOpen ||
                        dialogRequiresState ||
                        !viewKey.Available));
            if (!shouldPublish)
            {
                m_LastViewKey = viewKey;
                m_HasLastViewKey = true;
                return;
            }

            var state = viewKey.Available &&
                m_ProgressionControlSystem != null
                ? m_ProgressionControlSystem
                    .GetManualMilestoneClaimsViewState()
                : ManualMilestoneClaimsViewState.Empty;
            m_StateBinding.Update(
                ManualMilestoneClaimsViewStateJson.Serialize(state));
            m_LastViewKey = viewKey;
            m_HasLastViewKey = true;
            m_StateUpdateRequested = false;
        }
    }
}
