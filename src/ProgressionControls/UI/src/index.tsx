import type { ModRegistrar } from "cs2/modding";
import { ProgressionWidget } from "./ProgressionWidget";

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("Game", ProgressionWidget);
};

export default register;
