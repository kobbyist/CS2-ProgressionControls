import { bindValue, trigger } from "cs2/api";

const group = "Kobbyist.ProgressionControls";

export const visible$ = bindValue(group, "Visible", false);
export const ready$ = bindValue(group, "Ready", false);
export const active$ = bindValue(group, "Active", false);
export const currentPopulation$ = bindValue(
  group,
  "CurrentPopulation",
  0
);
export const historicalMaximumPopulation$ = bindValue(
  group,
  "HistoricalMaximumPopulation",
  0
);
export const populationToResume$ = bindValue(
  group,
  "PopulationToResume",
  0
);
export const mostRecentPopulationXpAward$ = bindValue(
  group,
  "MostRecentPopulationXpAward",
  "0"
);
export const positionX$ = bindValue(group, "PositionX", 0);
export const positionY$ = bindValue(group, "PositionY", 0);

export const savePosition = (x: number, y: number) => {
  trigger(group, "PositionChanged", `${Math.round(x)},${Math.round(y)}`);
};
