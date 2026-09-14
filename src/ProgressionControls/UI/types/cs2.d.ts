// Verified subset of CS2 1.6.0f1. See docs/local-assembly-verification.md.
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
    translate: (id: string, fallback?: string) => string | null | undefined;
  }

  export function useLocalization(): Localization;
}

declare module "cs2/modding" {
  import { ComponentType } from "react";

  export type ModuleRegistryAppend = ComponentType | (() => JSX.Element);

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

declare module "cs2/input" {
  export interface FocusSymbol {
    readonly debugName: string;
    readonly r: number;
    toString(): string;
  }

  export type FocusKey = FocusSymbol | string | number;
  export const FOCUS_DISABLED: FocusSymbol;
}

declare module "cs2/ui" {
  import {
    ButtonHTMLAttributes,
    ComponentType,
    HTMLAttributes,
    ReactNode,
  } from "react";
  import { FocusKey } from "cs2/input";

  export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    variant?:
      | "default"
      | "primary"
      | "flat"
      | "round"
      | "menu"
      | "icon"
      | "floating"
      | "text";
    focusKey?: FocusKey | undefined;
    src?: string | undefined;
    tooltipLabel?: ReactNode;
    onSelect?: (() => void) | undefined;
  }

  export interface IconProps {
    src: string;
    tinted?: boolean | string | undefined;
    className?: string | undefined;
    children?: ReactNode;
  }

  export interface PanelProps extends HTMLAttributes<HTMLDivElement> {
    header?: ReactNode;
    contentClassName?: string | undefined;
  }

  export interface ScrollableProps {
    vertical?: boolean | undefined;
    horizontal?: boolean | undefined;
    className?: string | undefined;
    children?: ReactNode;
  }

  export const Button: ComponentType<ButtonProps>;
  export const Icon: ComponentType<IconProps>;
  export const Panel: ComponentType<PanelProps>;
  export const Scrollable: ComponentType<ScrollableProps>;
}
