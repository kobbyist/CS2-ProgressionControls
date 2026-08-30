import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Button, Icon, Scrollable } from "cs2/ui";
import { useEffect, useState } from "react";
import {
  CompactModPanel,
  PanelSection,
  TopLeftEntryButton,
} from "./compact-mod-ui";
import styles from "./manual-milestone-claims.module.scss";
import progressionControlsIcon from "./progression-controls.svg";

const bindingGroup = "Kobbyist.ProgressionControls";
const nativeMilestoneIcon = "Media/Game/Icons/Milestone.svg";
const nativeToolbarIcon = progressionControlsIcon;
const nativeLockIcon = "Media/Glyphs/Lock.svg";
const nativeWarningIcon = "Media/Misc/Warning.svg";
const emptyStateJson =
  '{"available":false,"active":false,"heldXp":0,"cityXp":0,"effectiveXp":0,"claimPending":false,"dialog":"none","milestones":[],"nextMilestoneIndex":0,"nextRequiredXp":0,"nextImage":"","nextRangeXp":0,"nextBackgroundColor":{"r":0,"g":0,"b":0,"a":0},"nextTextColor":{"r":0,"g":0,"b":0,"a":0}}';

const stateBinding = bindValue<string>(
  bindingGroup,
  "manualMilestoneClaimsState",
  emptyStateJson,
);
const availabilityBinding = bindValue<boolean>(
  bindingGroup,
  "manualMilestoneClaimsAvailable",
  false,
);

interface ManualMilestoneClaimsColor {
  r: number;
  g: number;
  b: number;
  a: number;
}

interface ManualMilestone {
  index: number;
  requiredXp: number;
  canClaim: boolean;
  image: string;
}

interface ManualMilestoneClaimsState {
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
  nextRangeXp: number;
  nextBackgroundColor: ManualMilestoneClaimsColor;
  nextTextColor: ManualMilestoneClaimsColor;
}

type Translate = (id: string, fallback: string) => string;

const emptyState: ManualMilestoneClaimsState = {
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
  nextRangeXp: 0,
  nextBackgroundColor: { r: 0, g: 0, b: 0, a: 0 },
  nextTextColor: { r: 0, g: 0, b: 0, a: 0 },
};

let cachedRawState: string | undefined;
let cachedParsedState = emptyState;

const toggleListeners = new Set<() => void>();

function requestPanelToggle() {
  toggleListeners.forEach((listener) => listener());
}

function parseState(value: string): ManualMilestoneClaimsState {
  if (value === cachedRawState) {
    return cachedParsedState;
  }

  cachedRawState = value;
  try {
    const parsed = JSON.parse(value) as Partial<ManualMilestoneClaimsState>;
    cachedParsedState = {
      ...emptyState,
      ...parsed,
      milestones: Array.isArray(parsed.milestones)
        ? parsed.milestones
        : [],
    };
  } catch {
    cachedParsedState = emptyState;
  }

  return cachedParsedState;
}

function formatXp(value: number) {
  return Math.max(0, value).toLocaleString();
}

function colorToCss(
  color: ManualMilestoneClaimsColor | undefined,
  fallback: string,
) {
  if (!color || !Number.isFinite(color.a) || color.a <= 0) {
    return fallback;
  }

  const channel = (value: number) =>
    Math.round(Math.min(1, Math.max(0, value)) * 255);
  const alpha = Math.min(1, Math.max(0, color.a));
  return `rgba(${channel(color.r)}, ${channel(color.g)}, ${channel(color.b)}, ${alpha})`;
}

export const ManualMilestoneClaimsToolbarButton = () => {
  const available = useValue(availabilityBinding);
  const { translate } = useLocalization();

  if (!available) {
    return null;
  }

  const title =
    translate(
      "Kobbyist.ProgressionControls.UI.Toggle",
      "Progression Controls",
    ) ?? "Progression Controls";

  return (
    <TopLeftEntryButton
      icon={nativeToolbarIcon}
      title={title}
      onSelect={requestPanelToggle}
    />
  );
};

export const ManualMilestoneClaimsOverlay = () => {
  const state = parseState(useValue(stateBinding));
  const [open, setOpen] = useState(false);
  const { translate } = useLocalization();

  useEffect(() => {
    const togglePanel = () => setOpen((current) => !current);
    toggleListeners.add(togglePanel);
    return () => {
      toggleListeners.delete(togglePanel);
    };
  }, []);

  useEffect(() => {
    trigger(bindingGroup, "setManualMilestoneClaimsPanelOpen", open);
  }, [open]);

  useEffect(
    () => () => {
      trigger(bindingGroup, "setManualMilestoneClaimsPanelOpen", false);
    },
    [],
  );

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
          <XpLedger heldXp={state.heldXp} t={t} />

          {state.nextMilestoneIndex > 0 ? (
            <NextMilestoneBanner
              state={state}
              milestoneName={milestoneName(state.nextMilestoneIndex)}
              t={t}
            />
          ) : null}

          {readyCount > 0 ? (
            <>
              <PanelSection
                title={t("MilestoneQueue", "Earned milestones")}
                summary={
                  <span
                    className={styles.queueCount}
                    aria-label={`${readyCount} ${t("Claimable", "claimable")}`}
                  >
                    {readyCount}
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
                        position={position}
                        milestoneName={milestoneName(milestone.index)}
                        claimPending={state.claimPending}
                        t={t}
                      />
                    ))}
                  </div>
                </Scrollable>
              </PanelSection>
            </>
          ) : state.nextMilestoneIndex <= 0 ? (
            <div className={styles.completeState}>
              {t("Complete", "Every milestone has been reached.")}
            </div>
          ) : null}
        </CompactModPanel>
      </div>

      {state.dialog !== "none" ? (
        <DecisionDialog state={state} t={t} />
      ) : null}
    </>
  );
};

const XpLedger = ({
  heldXp,
  t,
}: {
  heldXp: number;
  t: Translate;
}) => (
  <div className={styles.xpLedger}>
    <span className={styles.ledgerLabel}>{t("HeldXp", "Held XP")}</span>
    <strong className={styles.ledgerValue}>{formatXp(heldXp)}</strong>
  </div>
);

const MilestoneIcon = ({
  image,
}: {
  image: string;
}) => (
  <div className={styles.milestoneIcon}>
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
  position,
  milestoneName,
  claimPending,
  t,
}: {
  milestone: ManualMilestone;
  isFirst: boolean;
  position: number;
  milestoneName: string;
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
      <div className={styles.queueOrder} aria-hidden="true">
        {String(position + 1).padStart(2, "0")}
      </div>
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
        <div className={styles.queuedStatus}>
          <Icon
            src={nativeLockIcon}
            tinted
            className={styles.lockIcon}
          />
          <span>{t("Queued", "Queued")}</span>
        </div>
      )}
    </div>
  );
};

const NextMilestoneBanner = ({
  state,
  milestoneName,
  t,
}: {
  state: ManualMilestoneClaimsState;
  milestoneName: string;
  t: Translate;
}) => {
  const visibleXp = state.nextRequiredXp > 0
    ? state.nextRangeXp
    : 0;
  const progress = state.nextRequiredXp > 0
    ? Math.min(
        100,
        (visibleXp / state.nextRequiredXp) * 100,
      )
    : 0;
  const backgroundColor = colorToCss(
    state.nextBackgroundColor,
    "#b9cdd1",
  );
  const textColor = colorToCss(
    state.nextTextColor,
    "#101c27",
  );

  return (
    <PanelSection title={t("NextMilestone", "Next milestone")}>
      <div
        className={styles.nextMilestoneBanner}
        style={{ backgroundColor, color: textColor }}
      >
        {state.nextImage ? (
          <img
            src={state.nextImage}
            className={styles.milestoneArtwork}
            alt=""
            aria-hidden="true"
          />
        ) : (
          <Icon
            src={nativeMilestoneIcon}
            className={styles.milestoneArtworkFallback}
          />
        )}
        <div className={styles.milestoneRangeReadout}>
          <strong>{formatXp(visibleXp)}</strong>
          <span>/ {formatXp(state.nextRequiredXp)} XP</span>
        </div>
        <div
          className={styles.milestoneRangeTrack}
          role="progressbar"
          aria-label={t(
            "XpProgress",
            "XP progress to the next milestone",
          )}
          aria-valuemin={0}
          aria-valuemax={state.nextRequiredXp}
          aria-valuenow={visibleXp}
        >
          <div
            className={styles.milestoneRangeFill}
            style={{ width: progress + "%" }}
          />
        </div>
        <div className={styles.nextMilestoneIdentity}>
          <span>
            {t("MilestoneLabel", "Milestone")} {state.nextMilestoneIndex}
          </span>
          <strong>{milestoneName}</strong>
        </div>
      </div>
    </PanelSection>
  );
};

const DecisionDialog = ({
  state,
  t,
}: {
  state: ManualMilestoneClaimsState;
  t: Translate;
}) => {
  const restoring = state.dialog === "restore";
  const resolve = (decision: string) =>
    trigger(bindingGroup, "resolveManualMilestoneClaims", decision);
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
