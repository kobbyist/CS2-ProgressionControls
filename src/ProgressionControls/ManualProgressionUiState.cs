using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Kobbyist.ProgressionControls
{
    internal enum ManualProgressionDialogKind
    {
        None,
        Disable,
        Restore,
    }

    internal enum ManualProgressionDecision
    {
        None,
        Release,
        Discard,
        Cancel,
        Restore,
        Later,
    }

    [DataContract]
    internal sealed class ManualProgressionViewState
    {
        [DataMember(Name = "available", Order = 1)]
        public bool Available { get; set; }

        [DataMember(Name = "active", Order = 2)]
        public bool Active { get; set; }

        [DataMember(Name = "heldXp", Order = 3)]
        public long HeldXp { get; set; }

        [DataMember(Name = "cityXp", Order = 4)]
        public int CityXp { get; set; }

        [DataMember(Name = "effectiveXp", Order = 5)]
        public long EffectiveXp { get; set; }

        [DataMember(Name = "claimPending", Order = 6)]
        public bool ClaimPending { get; set; }

        [DataMember(Name = "dialog", Order = 7)]
        public string Dialog { get; set; }

        [DataMember(Name = "milestones", Order = 8)]
        public ManualProgressionMilestoneView[] Milestones
        {
            get;
            set;
        }

        [DataMember(Name = "nextMilestoneIndex", Order = 9)]
        public int NextMilestoneIndex { get; set; }

        [DataMember(Name = "nextRequiredXp", Order = 10)]
        public int NextRequiredXp { get; set; }

        [DataMember(Name = "nextImage", Order = 11)]
        public string NextImage { get; set; }

        [DataMember(Name = "nextRangeXp", Order = 12)]
        public long NextRangeXp { get; set; }

        [DataMember(Name = "nextBackgroundColor", Order = 13)]
        public ManualProgressionColorView NextBackgroundColor { get; set; }

        [DataMember(Name = "nextTextColor", Order = 14)]
        public ManualProgressionColorView NextTextColor { get; set; }

        public static ManualProgressionViewState Empty =>
            new ManualProgressionViewState
            {
                Dialog = "none",
                Milestones = Array.Empty<ManualProgressionMilestoneView>(),
            };
    }

    [DataContract]
    internal struct ManualProgressionColorView
    {
        public ManualProgressionColorView(
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
    internal sealed class ManualProgressionMilestoneView
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

    internal static class ManualProgressionViewStateJson
    {
        private static readonly DataContractJsonSerializer s_Serializer =
            new DataContractJsonSerializer(
                typeof(ManualProgressionViewState));

        public static string Serialize(ManualProgressionViewState state)
        {
            using (var stream = new MemoryStream())
            {
                s_Serializer.WriteObject(
                    stream,
                    state ?? ManualProgressionViewState.Empty);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
