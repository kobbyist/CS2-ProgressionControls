declare module "cs2/api" {
  export interface ValueBinding<T> {
    readonly value: T;
    subscribe(listener?: (value: T) => void): {
      readonly value: T;
      dispose(): void;
    };
    dispose(): void;
  }

  export function bindValue<T>(
    group: string,
    name: string,
    fallbackValue?: T
  ): ValueBinding<T>;

  export function useValue<T>(binding: ValueBinding<T>): T;

  export function trigger(
    group: string,
    name: string,
    ...args: unknown[]
  ): void;
}

declare module "cs2/l10n" {
  export interface Localization {
    translate(id: string, fallback?: string | null): string | null;
  }

  export function useLocalization(): Localization;
}

declare module "cs2/modding" {
  import { ComponentType } from "react";

  export interface ModuleRegistry {
    append(
      target: "Game",
      component: ComponentType<Record<string, never>>
    ): void;
  }

  export type ModRegistrar = (moduleRegistry: ModuleRegistry) => void;
}

declare module "*.module.scss" {
  const classes: Record<string, string>;
  export default classes;
}
