import { useSyncExternalStore } from "react";

export const useMediaQuery = (query: string): boolean => {
	const subscribe = (callback: () => void) => {
		const list = window.matchMedia(query);
		list.addEventListener("change", callback);
		return () => list.removeEventListener("change", callback);
	};

	return useSyncExternalStore(
		subscribe,
		() => window.matchMedia(query).matches,
		() => false,
	);
};
