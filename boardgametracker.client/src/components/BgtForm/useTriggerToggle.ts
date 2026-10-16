import type { SelectContentProps } from "@radix-ui/react-select";
import { type MouseEvent, type RefObject, useCallback, useRef } from "react";

type PointerDownOutsideEvent = Parameters<NonNullable<SelectContentProps["onPointerDownOutside"]>>[0];

const isOverElement = (element: HTMLElement | null, x: number, y: number) => {
	if (!element) {
		return false;
	}
	const rect = element.getBoundingClientRect();
	return x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
};

export const useTriggerToggle = (triggerRef: RefObject<HTMLButtonElement | null>) => {
	const closedFromTrigger = useRef(false);

	const reset = useCallback(() => {
		closedFromTrigger.current = false;
	}, []);

	const focusTriggerAfterClick = useCallback(() => {
		if (!closedFromTrigger.current) {
			return;
		}
		closedFromTrigger.current = false;
		triggerRef.current?.focus({ preventScroll: true });
	}, [triggerRef]);

	const onPointerDownOutside = useCallback(
		(event: PointerDownOutsideEvent) => {
			const { clientX, clientY } = event.detail.originalEvent;
			if (!isOverElement(triggerRef.current, clientX, clientY)) {
				return;
			}
			closedFromTrigger.current = true;
			document.addEventListener("click", focusTriggerAfterClick, { once: true });
		},
		[triggerRef, focusTriggerAfterClick],
	);

	const onTriggerClick = useCallback((event: MouseEvent<HTMLButtonElement>) => {
		if (!closedFromTrigger.current) {
			return;
		}
		closedFromTrigger.current = false;
		event.preventDefault();
		event.currentTarget.focus();
	}, []);

	return { onPointerDownOutside, onTriggerPointerDown: reset, onTriggerClick };
};
