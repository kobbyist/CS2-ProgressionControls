import { useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import {
  active$,
  currentPopulation$,
  historicalMaximumPopulation$,
  mostRecentPopulationXpAward$,
  populationToResume$,
  positionX$,
  positionY$,
  ready$,
  visible$
} from "./bindings";
import { useDraggablePosition } from "./useDraggablePosition";
import styles from "./ProgressionWidget.module.scss";

const localePrefix = "ProgressionControls.Widget.";

const formatInteger = (value: number) =>
  Math.max(0, Math.round(value)).toLocaleString();

export const ProgressionWidget = () => {
  const visible = useValue(visible$);
  const ready = useValue(ready$);

  if (!visible || !ready) {
    return null;
  }

  return <ProgressionWidgetContent />;
};

const ProgressionWidgetContent = () => {
  const localization = useLocalization();
  const active = useValue(active$);
  const currentPopulation = useValue(currentPopulation$);
  const historicalMaximum = useValue(historicalMaximumPopulation$);
  const populationToResume = useValue(populationToResume$);
  const mostRecentAward = useValue(mostRecentPopulationXpAward$);
  const savedX = useValue(positionX$);
  const savedY = useValue(positionY$);

  const text = (name: string, fallback: string) =>
    localization.translate(`${localePrefix}${name}`, fallback) ?? fallback;

  const layoutKey = `${active}`;
  const {
    dragging,
    position,
    rootRef,
    startDragging
  } = useDraggablePosition(savedX, savedY, layoutKey);

  const parsedAward = Number(mostRecentAward);
  const hasAward =
    Number.isFinite(parsedAward) && parsedAward > 0;

  return (
    <div
      ref={rootRef}
      className={`${styles.widget} ${dragging ? styles.dragging : ""}`}
      style={{
        transform: `translate(${position.x}px, ${position.y}px)`
      }}
    >
      <div
        className={styles.header}
        onMouseDown={startDragging}
        title={text("DragLabel", "Drag to move")}
      >
        <span className={styles.title}>
          {text("Title", "Progression Controls")}
        </span>
        <span className={styles.dragHandle} aria-hidden="true">
          <span />
          <span />
          <span />
        </span>
      </div>

      {!active ? (
        <div className={styles.vanillaMode}>
          <span className={styles.statusDot} />
          {text("VanillaActive", "Vanilla progression active")}
        </div>
      ) : (
        <div className={styles.content}>
          <div className={styles.mode}>
            <span className={styles.statusDot} />
            {text("CustomActive", "Custom progression active")}
          </div>

          <div className={styles.row}>
            <span>{text("CurrentPopulation", "Current population")}</span>
            <strong>{formatInteger(currentPopulation)}</strong>
          </div>
          <div className={styles.row}>
            <span>
              {text("HistoricalMaximum", "Population record")}
            </span>
            <strong>{formatInteger(historicalMaximum)}</strong>
          </div>
          <div className={styles.row}>
            <span>{text("GrowthStatus", "Growth status")}</span>
            <strong>
              {populationToResume <= 0
                ? text("AtRecord", "At population record")
                : `${formatInteger(populationToResume)} ${text(
                    "ToResume",
                    "to resume"
                  )}`}
            </strong>
          </div>
          <div className={styles.row}>
            <span>
              {text("MostRecentAward", "Latest population XP")}
            </span>
            <strong className={hasAward ? styles.award : undefined}>
              {hasAward
                ? `+${formatInteger(parsedAward)} XP`
                : text("NoAward", "None yet")}
            </strong>
          </div>
        </div>
      )}
    </div>
  );
};
