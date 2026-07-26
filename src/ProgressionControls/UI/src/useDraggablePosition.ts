import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type MouseEvent as ReactMouseEvent,
  type RefObject
} from "react";
import { savePosition } from "./bindings";

type Position = {
  x: number;
  y: number;
};

type DragOrigin = Position & {
  pointerX: number;
  pointerY: number;
  left: number;
  top: number;
  width: number;
  height: number;
};

const clampCurrent = (
  element: HTMLDivElement,
  candidate: Position
): Position => {
  const rect = element.getBoundingClientRect();
  let x = candidate.x;
  let y = candidate.y;

  if (rect.left < 0) {
    x -= rect.left;
  } else if (rect.right > window.innerWidth) {
    x -= rect.right - window.innerWidth;
  }

  if (rect.top < 0) {
    y -= rect.top;
  } else if (rect.bottom > window.innerHeight) {
    y -= rect.bottom - window.innerHeight;
  }

  return {
    x: Math.round(x),
    y: Math.round(y)
  };
};

export const useDraggablePosition = (
  savedX: number,
  savedY: number,
  layoutKey: string
): {
  dragging: boolean;
  position: Position;
  rootRef: RefObject<HTMLDivElement>;
  startDragging: (event: ReactMouseEvent<HTMLDivElement>) => void;
} => {
  const rootRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState<Position>({
    x: savedX,
    y: savedY
  });
  const [dragging, setDragging] = useState(false);
  const positionRef = useRef(position);
  const dragOriginRef = useRef<DragOrigin | null>(null);
  const frameRef = useRef<number | null>(null);

  const updatePosition = useCallback((next: Position) => {
    positionRef.current = next;
    setPosition(next);
  }, []);

  const clampCurrentPosition = useCallback(() => {
    const element = rootRef.current;
    if (element === null) {
      return;
    }

    updatePosition(clampCurrent(element, positionRef.current));
  }, [updatePosition]);

  useEffect(() => {
    updatePosition({
      x: Number.isFinite(savedX) ? Math.round(savedX) : 0,
      y: Number.isFinite(savedY) ? Math.round(savedY) : 0
    });
  }, [savedX, savedY, updatePosition]);

  useEffect(() => {
    frameRef.current = window.requestAnimationFrame(clampCurrentPosition);
    const timeout = window.setTimeout(clampCurrentPosition, 150);
    window.addEventListener("resize", clampCurrentPosition);

    return () => {
      if (frameRef.current !== null) {
        window.cancelAnimationFrame(frameRef.current);
        frameRef.current = null;
      }
      window.clearTimeout(timeout);
      window.removeEventListener("resize", clampCurrentPosition);
    };
  }, [clampCurrentPosition, layoutKey]);

  useEffect(() => {
    if (!dragging) {
      return;
    }

    const move = (event: MouseEvent) => {
      const origin = dragOriginRef.current;
      const element = rootRef.current;
      if (origin === null || element === null) {
        return;
      }

      const deltaX = event.clientX - origin.pointerX;
      const deltaY = event.clientY - origin.pointerY;
      let x = origin.x + deltaX;
      let y = origin.y + deltaY;
      const left = origin.left + deltaX;
      const top = origin.top + deltaY;
      const right = left + origin.width;
      const bottom = top + origin.height;

      if (left < 0) {
        x -= left;
      } else if (right > window.innerWidth) {
        x -= right - window.innerWidth;
      }

      if (top < 0) {
        y -= top;
      } else if (bottom > window.innerHeight) {
        y -= bottom - window.innerHeight;
      }

      positionRef.current = {
        x: Math.round(x),
        y: Math.round(y)
      };

      if (frameRef.current === null) {
        frameRef.current = window.requestAnimationFrame(() => {
          frameRef.current = null;
          setPosition(positionRef.current);
        });
      }
    };

    const stop = () => {
      if (frameRef.current !== null) {
        window.cancelAnimationFrame(frameRef.current);
        frameRef.current = null;
      }

      dragOriginRef.current = null;
      setDragging(false);
      setPosition(positionRef.current);
      savePosition(positionRef.current.x, positionRef.current.y);
    };

    document.addEventListener("mousemove", move);
    document.addEventListener("mouseup", stop);
    return () => {
      document.removeEventListener("mousemove", move);
      document.removeEventListener("mouseup", stop);
    };
  }, [dragging]);

  useEffect(
    () => () => {
      if (frameRef.current !== null) {
        window.cancelAnimationFrame(frameRef.current);
      }
    },
    []
  );

  const startDragging = useCallback(
    (event: ReactMouseEvent<HTMLDivElement>) => {
      if (event.button !== 0) {
        return;
      }

      const element = rootRef.current;
      if (element === null) {
        return;
      }

      const rect = element.getBoundingClientRect();
      event.preventDefault();
      event.stopPropagation();
      dragOriginRef.current = {
        x: positionRef.current.x,
        y: positionRef.current.y,
        pointerX: event.clientX,
        pointerY: event.clientY,
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height
      };
      setDragging(true);
    },
    []
  );

  return {
    dragging,
    position,
    rootRef,
    startDragging
  };
};
