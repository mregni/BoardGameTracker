import type { KeyboardEvent } from "react";

export const findSearchMatch = <T extends { label: string }>(items: T[], searchTerm: string): T | undefined => {
	const term = searchTerm.trim().toLowerCase();
	if (!term) {
		return undefined;
	}
	return items.find((item) => item.label.toLowerCase().includes(term));
};

export const handleSelectSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>, selectMatch: () => void) => {
	if (event.key === "Escape" || event.key === "Tab") {
		return;
	}
	event.stopPropagation();
	if (event.key === "Enter") {
		event.preventDefault();
		selectMatch();
	} else if (event.key === "ArrowDown") {
		event.preventDefault();
		event.currentTarget.closest('[role="listbox"]')?.querySelector<HTMLElement>('[role="option"]')?.focus();
	}
};
