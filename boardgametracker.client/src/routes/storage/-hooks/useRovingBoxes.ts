import { type KeyboardEvent, useId, useState } from "react";

const steps: Record<string, (index: number, last: number) => number> = {
	ArrowRight: (index) => index + 1,
	ArrowDown: (index) => index + 1,
	ArrowLeft: (index) => index - 1,
	ArrowUp: (index) => index - 1,
	Home: () => 0,
	End: (_, last) => last,
};

export interface RovingBoxes {
	tabIndexFor: (key: string) => 0 | -1;
	describedByFor: (key: string) => string | undefined;
	hintId: string;
	onFocusBox: (key: string) => void;
	onKeyDown: (event: KeyboardEvent<HTMLElement>) => void;
}

export const useRovingBoxes = (keys: string[], selectedKey: string | null): RovingBoxes => {
	const hintId = useId();
	const [focused, setFocused] = useState<string | null>(null);
	const active = [focused, selectedKey, keys[0]].find((key) => key !== null && keys.includes(key)) ?? null;

	const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
		const step = steps[event.key];
		const current = (event.target as HTMLElement).closest("[data-box]")?.getAttribute("data-box");
		const index = current ? keys.indexOf(current) : -1;
		if (!step || index < 0) return;
		event.preventDefault();
		const next = keys[Math.max(0, Math.min(keys.length - 1, step(index, keys.length - 1)))];
		setFocused(next);
		document.querySelector<HTMLElement>(`[data-box="${next}"]`)?.focus();
	};

	return {
		tabIndexFor: (key) => (key === active ? 0 : -1),
		describedByFor: (key) => (key === active ? hintId : undefined),
		hintId,
		onFocusBox: setFocused,
		onKeyDown,
	};
};
