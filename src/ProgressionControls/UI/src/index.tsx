import type { ModRegistrar } from "cs2/modding";
import { ProgressionWidget } from "./ProgressionWidget";

// CS2 only loads the matching extracted stylesheet when the UI module
// explicitly advertises that one is present.
export const hasCSS = true;

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("Game", ProgressionWidget);
};

export default register;
