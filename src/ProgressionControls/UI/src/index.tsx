import { ModRegistrar } from "cs2/modding";
import {
  ManualProgressionOverlay,
  ManualProgressionToolbarButton,
} from "./manual-progression";

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("GameTopLeft", ManualProgressionToolbarButton);
  moduleRegistry.append("Game", ManualProgressionOverlay);
};

export default register;
