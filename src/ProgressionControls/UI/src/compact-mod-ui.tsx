import { getModule } from "cs2/modding";
import { Button, FOCUS_DISABLED, Panel } from "cs2/ui";
import { ReactNode } from "react";
import styles from "./compact-mod-ui.module.scss";

const roundHighlightTheme = getModule(
  "game-ui/common/input/button/themes/round-highlight-button.module.scss",
  "classes",
) as { button: string };

interface TopLeftEntryButtonProps {
  icon: string;
  title: string;
  onSelect: () => void;
}

export const TopLeftEntryButton = ({
  icon,
  title,
  onSelect,
}: TopLeftEntryButtonProps) => (
  <Button
    variant="floating"
    src={icon}
    tinted
    tooltipLabel={title}
    aria-label={title}
    onSelect={onSelect}
  />
);

interface CompactModPanelProps {
  title: string;
  icon?: string;
  variant?: "overlay" | "dialog";
  onClose?: () => void;
  labelledBy?: string;
  describedBy?: string;
  children: ReactNode;
}

export const CompactModPanel = ({
  title,
  icon,
  variant = "overlay",
  onClose,
  labelledBy,
  describedBy,
  children,
}: CompactModPanelProps) => (
  <Panel
    className={
      variant === "dialog" ? styles.dialogPanel : styles.overlayPanel
    }
    contentClassName={styles.panelContent}
    header={
      <div className={styles.header}>
        <div className={styles.titleArea}>
          {icon ? <img src={icon} className={styles.headerIcon} alt="" /> : null}
          <div className={styles.title} id={labelledBy}>
            {title}
          </div>
        </div>
        {onClose ? (
          <Button
            className={`${roundHighlightTheme.button} ${styles.closeButton}`}
            variant="icon"
            focusKey={FOCUS_DISABLED}
            aria-label="Close"
            onClick={onClose}
          >
            <img
              src="Media/Glyphs/Close.svg"
              className={styles.closeIcon}
              alt=""
            />
          </Button>
        ) : null}
      </div>
    }
    role={variant === "dialog" ? "dialog" : undefined}
    aria-modal={variant === "dialog" ? true : undefined}
    aria-labelledby={labelledBy}
    aria-describedby={describedBy}
  >
    {children}
  </Panel>
);

interface PanelSectionProps {
  title: string;
  summary?: ReactNode;
  children: ReactNode;
}

export const PanelSection = ({
  title,
  summary,
  children,
}: PanelSectionProps) => (
  <section className={styles.section}>
    <div className={styles.sectionHeader}>
      <span>{title}</span>
      {summary}
    </div>
    {children}
  </section>
);
