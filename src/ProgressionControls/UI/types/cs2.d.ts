declare module "cs2/api" {
  export interface ValueBinding<T> {
    readonly value: T;
  }

  export function bindValue<T>(
    group: string,
    name: string,
    fallbackValue?: T,
  ): ValueBinding<T>;

  export function trigger(
    group: string,
    name: string,
    ...args: unknown[]
  ): void;

  export function useValue<T>(binding: ValueBinding<T>): T;
}

declare module "cs2/l10n" {
  export interface Localization {
    translate(id: string, fallback?: string): string | null | undefined;
  }

  export function useLocalization(): Localization;
}

declare module "cs2/modding" {
  import { ComponentType } from "react";

  export type ModuleRegistryAppend = ComponentType<{}> | (() => JSX.Element);

  export interface ModuleRegistry {
    append(
      target: "Game" | "GameTopLeft",
      appendedComponent: ModuleRegistryAppend,
      index?: number,
    ): void;
  }

  export type ModRegistrar = (moduleRegistry: ModuleRegistry) => void;

  export function getModule(modulePath: string, exportName: string): unknown;
}

declare module "cs2/ui" {
  import { ComponentType } from "react";

  export const Button: ComponentType<any>;
  export const Icon: ComponentType<any>;
  export const Panel: ComponentType<any>;
  export const Scrollable: ComponentType<any>;
  export const FOCUS_DISABLED: any;
}
