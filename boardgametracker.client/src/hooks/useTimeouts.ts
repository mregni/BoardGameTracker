import { useCallback, useEffect, useRef } from "react";

export const useTimeouts = () => {
	const timers = useRef(new Set<ReturnType<typeof setTimeout>>());

	useEffect(() => {
		const pending = timers.current;
		return () => {
			for (const timer of pending) {
				clearTimeout(timer);
			}
			pending.clear();
		};
	}, []);

	return useCallback((callback: () => void, delay: number) => {
		const timer = setTimeout(() => {
			timers.current.delete(timer);
			callback();
		}, delay);
		timers.current.add(timer);
	}, []);
};
