import { ModRegistrar } from "cs2/modding";
import {
  ManualMilestoneClaimsOverlay,
  ManualMilestoneClaimsToolbarButton,
} from "./manual-milestone-claims";

export const hasCSS = true;

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("GameTopLeft", ManualMilestoneClaimsToolbarButton);
  moduleRegistry.append("Game", ManualMilestoneClaimsOverlay);
};

export default register;
