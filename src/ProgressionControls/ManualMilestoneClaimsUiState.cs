using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Kobbyist.ProgressionControls
{
    internal enum ManualMilestoneClaimsDialogKind
    {
        None,
        Disable,
        Restore,
    }

    internal enum ManualMilestoneClaimsDecision
    {
        None,
        Release,
        Discard,
        Cancel,
        Restore,
        Later,
    }

    internal readonly struct ManualMilestoneClaimsViewKey :
        IEquatable<ManualMilestoneClaimsViewKey>
    {
        public ManualMilestoneClaimsViewKey(
            bool available,
            int cityXp,
            int achievedMilestone,
            long heldXp,
            bool claimPending,
            bool active,
            ManualMilestoneClaimsDialogKind dialog,
            int catalogRevision)
        {
            Available = available;
            CityXp = cityXp;
            AchievedMilestone = achievedMilestone;
            HeldXp = heldXp;
            ClaimPending = claimPending;
            Active = active;
            Dialog = dialog;
            CatalogRevision = catalogRevision;
        }

        public bool Available { get; }

        public int CityXp { get; }

        public int AchievedMilestone { get; }

        public long HeldXp { get; }

        public bool ClaimPending { get; }

        public bool Active { get; }

        public ManualMilestoneClaimsDialogKind Dialog { get; }

        public int CatalogRevision { get; }

        public bool Equals(ManualMilestoneClaimsViewKey other)
        {
            return Available == other.Available &&
                CityXp == other.CityXp &&
                AchievedMilestone == other.AchievedMilestone &&
                HeldXp == other.HeldXp &&
                ClaimPending == other.ClaimPending &&
                Active == other.Active &&
                Dialog == other.Dialog &&
                CatalogRevision == other.CatalogRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is ManualMilestoneClaimsViewKey other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Available.GetHashCode();
                hash = hash * 397 ^ CityXp;
                hash = hash * 397 ^ AchievedMilestone;
                hash = hash * 397 ^ HeldXp.GetHashCode();
                hash = hash * 397 ^ ClaimPending.GetHashCode();
                hash = hash * 397 ^ Active.GetHashCode();
                hash = hash * 397 ^ (int)Dialog;
                hash = hash * 397 ^ CatalogRevision;
                return hash;
            }
        }
    }

    [DataContract]
    internal sealed class ManualMilestoneClaimsViewState
    {
        private static readonly ManualMilestoneClaimsViewState s_Empty =
            new ManualMilestoneClaimsViewState
            {
                Dialog = "none",
                Milestones = Array.Empty<ManualMilestoneClaimView>(),
            };

        [DataMember(Name = "heldXp", Order = 1)]
        public long HeldXp { get; set; }

        [DataMember(Name = "claimPending", Order = 2)]
        public bool ClaimPending { get; set; }

        [DataMember(Name = "dialog", Order = 3)]
        public string Dialog { get; set; }

        [DataMember(Name = "milestones", Order = 4)]
        public ManualMilestoneClaimView[] Milestones
        {
            get;
            set;
        }

        [DataMember(Name = "nextMilestoneIndex", Order = 5)]
        public int NextMilestoneIndex { get; set; }

        [DataMember(Name = "nextRequiredXp", Order = 6)]
        public int NextRequiredXp { get; set; }

        [DataMember(Name = "nextImage", Order = 7)]
        public string NextImage { get; set; }

        [DataMember(Name = "nextRangeXp", Order = 8)]
        public long NextRangeXp { get; set; }

        [DataMember(Name = "nextBackgroundColor", Order = 9)]
        public MilestoneCardColorView NextBackgroundColor { get; set; }

        [DataMember(Name = "nextTextColor", Order = 10)]
        public MilestoneCardColorView NextTextColor { get; set; }

        [DataMember(Name = "catalogAvailable", Order = 11)]
        public bool CatalogAvailable { get; set; }

        [DataMember(Name = "finalMilestoneReached", Order = 12)]
        public bool FinalMilestoneReached { get; set; }

        public static ManualMilestoneClaimsViewState Empty => s_Empty;
    }

    [DataContract]
    internal struct MilestoneCardColorView
    {
        public MilestoneCardColorView(
            float red,
            float green,
            float blue,
            float alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        [DataMember(Name = "r", Order = 1)]
        public float Red { get; set; }

        [DataMember(Name = "g", Order = 2)]
        public float Green { get; set; }

        [DataMember(Name = "b", Order = 3)]
        public float Blue { get; set; }

        [DataMember(Name = "a", Order = 4)]
        public float Alpha { get; set; }
    }

    [DataContract]
    internal sealed class ManualMilestoneClaimView
    {
        [DataMember(Name = "index", Order = 1)]
        public int Index { get; set; }

        [DataMember(Name = "requiredXp", Order = 2)]
        public int RequiredXp { get; set; }

        [DataMember(Name = "canClaim", Order = 3)]
        public bool CanClaim { get; set; }

        [DataMember(Name = "image", Order = 4)]
        public string Image { get; set; }
    }

    internal static class ManualMilestoneClaimsViewStateJson
    {
        private static readonly DataContractJsonSerializer s_Serializer =
            new DataContractJsonSerializer(
                typeof(ManualMilestoneClaimsViewState));

        public static string Serialize(ManualMilestoneClaimsViewState state)
        {
            using (var stream = new MemoryStream())
            {
                s_Serializer.WriteObject(
                    stream,
                    state ?? ManualMilestoneClaimsViewState.Empty);
                return Encoding.UTF8.GetString(
                    stream.GetBuffer(),
                    index: 0,
                    count: (int)stream.Length);
            }
        }
    }
}
