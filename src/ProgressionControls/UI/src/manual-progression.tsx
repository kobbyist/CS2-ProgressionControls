import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Button, Icon, Scrollable } from "cs2/ui";
import { useEffect, useMemo, useState } from "react";
import {
  CompactModPanel,
  PanelSection,
  TopLeftEntryButton,
} from "./compact-mod-ui";
import styles from "./manual-progression.module.scss";

const bindingGroup = "Kobbyist.ProgressionControls";
const nativeMilestoneIcon = "Media/Game/Icons/Milestone.svg";
const nativeToolbarIcon = "Media/Game/Icons/Trophy.svg";
const nativeLockIcon = "Media/Glyphs/Lock.svg";
const nativeWarningIcon = "Media/Misc/Warning.svg";

const stateBinding = bindValue<string>(
  bindingGroup,
  "manualProgressionState",
  '{"available":false,"active":false,"heldXp":0,"cityXp":0,"effectiveXp":0,"claimPending":false,"dialog":"none","milestones":[],"nextMilestoneIndex":0,"nextRequiredXp":0,"nextImage":""}',
);

interface ManualMilestone {
  index: number;
  requiredXp: number;
  canClaim: boolean;
  image: string;
}

interface ManualProgressionState {
  available: boolean;
  active: boolean;
  heldXp: number;
  cityXp: number;
  effectiveXp: number;
  claimPending: boolean;
  dialog: "none" | "disable" | "restore";
  milestones: ManualMilestone[];
  nextMilestoneIndex: number;
  nextRequiredXp: number;
  nextImage: string;
}

type Translate = (id: string, fallback: string) => string;

const emptyState: ManualProgressionState = {
  available: false,
  active: false,
  heldXp: 0,
  cityXp: 0,
  effectiveXp: 0,
  claimPending: false,
  dialog: "none",
  milestones: [],
  nextMilestoneIndex: 0,
  nextRequiredXp: 0,
  nextImage: "",
};

const openListeners = new Set<() => void>();

function requestPanelOpen() {
  openListeners.forEach((listener) => listener());
}

function parseState(value: string): ManualProgressionState {
  try {
    const parsed = JSON.parse(value) as Partial<ManualProgressionState>;
    return {
      ...emptyState,
      ...parsed,
      milestones: Array.isArray(parsed.milestones)
        ? parsed.milestones
        : [],
    };
  } catch {
    return emptyState;
  }
}

function formatXp(value: number) {
  return Math.max(0, value).toLocaleString();
}

export const ManualProgressionToolbarButton = () => {
  const rawState = useValue(stateBinding);
  const state = useMemo(() => parseState(rawState), [rawState]);
  const { translate } = useLocalization();

  if (!state.available) {
    return null;
  }

  const title =
    translate(
      "Kobbyist.ProgressionControls.UI.Open",
      "Open Progression Controls",
    ) ?? "Open Progression Controls";

  return (
    <TopLeftEntryButton
      icon={nativeToolbarIcon}
      title={title}
      onSelect={requestPanelOpen}
    />
  );
};

export const ManualProgressionOverlay = () => {
  const rawState = useValue(stateBinding);
  const state = useMemo(() => parseState(rawState), [rawState]);
  const [open, setOpen] = useState(false);
  const { translate } = useLocalization();

  useEffect(() => {
    const openPanel = () => setOpen(true);
    openListeners.add(openPanel);
    return () => {
      openListeners.delete(openPanel);
    };
  }, []);

  useEffect(() => {
    if (state.dialog !== "none") {
      setOpen(true);
    }
  }, [state.dialog]);

  if (!state.available || !open) {
    return null;
  }

  const t: Translate = (id, fallback) =>
    translate("Kobbyist.ProgressionControls.UI." + id, fallback) ??
    fallback;
  const milestoneName = (index: number) =>
    translate("Progression.MILESTONE_NAME:" + index, "Milestone " + index) ??
    "Milestone " + index;
  const readyCount = state.milestones.length;

  return (
    <>
      <div className={styles.panelPosition}>
        <CompactModPanel
          title={t("Title", "Progression Controls")}
          icon={nativeToolbarIcon}
          onClose={() => setOpen(false)}
        >
          <div className={styles.summary}>
            <SummaryValue
              label={t("HeldXp", "Held XP")}
              value={formatXp(state.heldXp)}
            />
            <div className={styles.summaryDivider} />
            <SummaryValue
              label={t("EffectiveXp", "Effective XP")}
              value={formatXp(state.effectiveXp)}
            />
          </div>

          {readyCount > 0 ? (
            <>
              <PanelSection
                title={t("MilestoneQueue", "Milestone queue")}
                summary={
                  <span className={styles.claimableCount}>
                    <strong>{readyCount}</strong>{" "}
                    {t("Claimable", "claimable")}
                  </span>
                }
              >
              <Scrollable vertical className={styles.milestoneScroll}>
                <div className={styles.milestoneList} role="list">
                  {state.milestones.map((milestone, position) => (
                    <MilestoneRow
                      key={milestone.index}
                      milestone={milestone}
                      isFirst={position === 0}
                      milestoneName={milestoneName(milestone.index)}
                      previousMilestoneName={
                        position > 0
                          ? milestoneName(
                              state.milestones[position - 1].index,
                            )
                          : ""
                      }
                      claimPending={state.claimPending}
                      t={t}
                    />
                  ))}
                </div>
              </Scrollable>
              </PanelSection>
            </>
          ) : state.nextMilestoneIndex > 0 ? (
            <NextMilestoneProgress
              state={state}
              milestoneName={milestoneName(state.nextMilestoneIndex)}
              t={t}
            />
          ) : (
            <div className={styles.completeState}>
              {t("Complete", "Every milestone has been reached.")}
            </div>
          )}
        </CompactModPanel>
      </div>

      {state.dialog !== "none" ? (
        <DecisionDialog state={state} t={t} />
      ) : null}
    </>
  );
};

const SummaryValue = ({
  label,
  value,
}: {
  label: string;
  value: string;
}) => (
  <div className={styles.summaryValue}>
    <span>{label}</span>
    <strong>{value}</strong>
  </div>
);

const MilestoneIcon = ({
  image,
  compact = false,
}: {
  image: string;
  compact?: boolean;
}) => (
  <div
    className={
      compact ? styles.milestoneIconCompact : styles.milestoneIcon
    }
  >
    {image ? (
      <img src={image} alt="" aria-hidden="true" />
    ) : (
      <Icon src={nativeMilestoneIcon} />
    )}
  </div>
);

const MilestoneRow = ({
  milestone,
  isFirst,
  milestoneName,
  previousMilestoneName,
  claimPending,
  t,
}: {
  milestone: ManualMilestone;
  isFirst: boolean;
  milestoneName: string;
  previousMilestoneName: string;
  claimPending: boolean;
  t: Translate;
}) => {
  const claiming = isFirst && claimPending;

  return (
    <div
      className={
        milestone.canClaim
          ? styles.milestoneRowClaimable
          : styles.milestoneRow
      }
      role="listitem"
      aria-busy={claiming}
    >
      <div className={styles.railIcon}>
        <MilestoneIcon image={milestone.image} />
      </div>
      <div className={styles.milestoneCopy}>
        <strong>{milestoneName}</strong>
        <span>{formatXp(milestone.requiredXp)} XP</span>
      </div>
      {milestone.canClaim || claiming ? (
        <Button
          variant="primary"
          className={styles.claimButton}
          disabled={claimPending}
          onSelect={() =>
            trigger(bindingGroup, "claimMilestone", milestone.index)
          }
        >
          {claiming
            ? t("Claiming", "Claiming...")
            : t("Claim", "Claim")}
        </Button>
      ) : (
        <div className={styles.lockedReason}>
          <Icon
            src={nativeLockIcon}
            tinted
            className={styles.lockIcon}
          />
          <span>
            {t(
              "ClaimPreviousFirst",
              "Claim " + previousMilestoneName + " first",
            )}
          </span>
        </div>
      )}
    </div>
  );
};

const NextMilestoneProgress = ({
  state,
  milestoneName,
  t,
}: {
  state: ManualProgressionState;
  milestoneName: string;
  t: Translate;
}) => {
  const progress = Math.min(
    100,
    (state.effectiveXp / state.nextRequiredXp) * 100,
  );

  return (
    <div className={styles.nextMilestone}>
      <PanelSection title={t("NextMilestone", "Next milestone")}>
      <div className={styles.nextMilestoneBody}>
        <MilestoneIcon image={state.nextImage} compact />
        <div className={styles.nextMilestoneCopy}>
          <strong>{milestoneName}</strong>
          <span>
            {formatXp(state.effectiveXp)} /{" "}
            {formatXp(state.nextRequiredXp)} XP
          </span>
        </div>
      </div>
      <div
        className={styles.progressTrack}
        role="progressbar"
        aria-label={t(
          "XpProgress",
          "XP progress to the next milestone",
        )}
        aria-valuemin={0}
        aria-valuemax={state.nextRequiredXp}
        aria-valuenow={Math.min(
          state.effectiveXp,
          state.nextRequiredXp,
        )}
      >
        <div
          className={styles.progressFill}
          style={{ width: progress + "%" }}
        />
      </div>
      </PanelSection>
    </div>
  );
};

const DecisionDialog = ({
  state,
  t,
}: {
  state: ManualProgressionState;
  t: Translate;
}) => {
  const restoring = state.dialog === "restore";
  const resolve = (decision: string) =>
    trigger(bindingGroup, "resolveManualProgression", decision);
  const title = restoring
    ? t("RecoveryTitle", "Held XP found")
    : t("DisableTitle", "Turn off manual milestone claims?");
  const body = restoring
    ? t(
        "RecoveryBody",
        "is stored by Progression Controls for this city.",
      )
    : t(
        "DisableBody",
        "is held outside the city save. Choose what happens before turning this off.",
      );

  return (
    <div className={styles.modalScrim}>
      <CompactModPanel
        variant="dialog"
        title={title}
        labelledBy="progression-controls-dialog-title"
        describedBy="progression-controls-dialog-body"
      >
        <p
          className={styles.dialogBody}
          id="progression-controls-dialog-body"
        >
          <strong>{formatXp(state.heldXp)} XP</strong> {body}
        </p>

        {state.claimPending ? (
          <div className={styles.pendingNotice} role="status">
            {t(
              "PendingClaim",
              "Waiting for the game to confirm the current milestone claim.",
            )}
          </div>
        ) : null}

        <div className={styles.dialogActions}>
          {restoring ? (
            <>
              <Button
                variant="primary"
                onSelect={() => resolve("Restore")}
              >
                {t("Restore", "Restore manual claims")}
              </Button>
              <Button
                variant="default"
                className={styles.dangerButton}
                onSelect={() => resolve("Discard")}
              >
                {t("Discard", "Discard permanently")}
              </Button>
              <Button
                variant="default"
                onSelect={() => resolve("Later")}
              >
                {t("Later", "Decide later")}
              </Button>
            </>
          ) : (
            <>
              <Button
                variant="primary"
                disabled={state.claimPending}
                onSelect={() => resolve("Release")}
              >
                {t(
                  "Release",
                  "Release to vanilla and turn off",
                )}
              </Button>
              <Button
                variant="default"
                className={styles.dangerButton}
                disabled={state.claimPending}
                onSelect={() => resolve("Discard")}
              >
                {t("Discard", "Discard permanently")}
              </Button>
              <Button
                variant="default"
                onSelect={() => resolve("Cancel")}
              >
                {t("Cancel", "Cancel")}
              </Button>
            </>
          )}
        </div>

        <div className={styles.discardWarning}>
          <Icon
            src={nativeWarningIcon}
            className={styles.warningIcon}
          />
          <span>
            {t("DiscardWarning", "Discarding cannot be undone.")}
          </span>
        </div>
      </CompactModPanel>
    </div>
  );
};
