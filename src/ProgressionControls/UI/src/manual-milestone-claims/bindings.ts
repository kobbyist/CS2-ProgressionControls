import { bindValue, trigger } from "cs2/api";

const bindingGroup = "Kobbyist.ProgressionControls";

export interface ManualMilestoneClaimsColor {
  r: number;
  g: number;
  b: number;
  a: number;
}

export interface ManualMilestone {
  index: number;
  requiredXp: number;
  canClaim: boolean;
  image: string;
}

export interface ManualMilestoneClaimsState {
  heldXp: number;
  claimPending: boolean;
  dialog: "none" | "disable" | "restore";
  milestones: ManualMilestone[];
  nextMilestoneIndex: number;
  nextRequiredXp: number;
  nextImage: string | null;
  nextRangeXp: number;
  nextBackgroundColor: ManualMilestoneClaimsColor;
  nextTextColor: ManualMilestoneClaimsColor;
  catalogAvailable: boolean;
  finalMilestoneReached: boolean;
}

type ManualMilestoneClaimsDecision =
  "Release" | "Discard" | "Cancel" | "Restore" | "Later";

const emptyState: ManualMilestoneClaimsState = {
  heldXp: 0,
  claimPending: false,
  dialog: "none",
  milestones: [],
  nextMilestoneIndex: 0,
  nextRequiredXp: 0,
  nextImage: "",
  nextRangeXp: 0,
  nextBackgroundColor: { r: 0, g: 0, b: 0, a: 0 },
  nextTextColor: { r: 0, g: 0, b: 0, a: 0 },
  catalogAvailable: false,
  finalMilestoneReached: false,
};

export const stateBinding = bindValue<string>(
  bindingGroup,
  "manualMilestoneClaimsState",
  JSON.stringify(emptyState),
);
export const availabilityBinding = bindValue<boolean>(
  bindingGroup,
  "manualMilestoneClaimsAvailable",
  false,
);

let cachedRawState: string | undefined;
let cachedParsedState = emptyState;

export function parseState(value: string): ManualMilestoneClaimsState {
  if (value === cachedRawState) {
    return cachedParsedState;
  }

  cachedRawState = value;
  try {
    const parsed = JSON.parse(value) as Partial<ManualMilestoneClaimsState>;
    cachedParsedState = {
      ...emptyState,
      ...parsed,
      milestones: Array.isArray(parsed.milestones) ? parsed.milestones : [],
    };
  } catch {
    cachedParsedState = emptyState;
  }

  return cachedParsedState;
}

export function setManualMilestoneClaimsPanelOpen(open: boolean) {
  trigger(bindingGroup, "setManualMilestoneClaimsPanelOpen", open);
}

export function requestMilestoneClaim(index: number) {
  trigger(bindingGroup, "claimMilestone", index);
}

export function resolveManualMilestoneClaims(
  decision: ManualMilestoneClaimsDecision,
) {
  trigger(bindingGroup, "resolveManualMilestoneClaims", decision);
}
