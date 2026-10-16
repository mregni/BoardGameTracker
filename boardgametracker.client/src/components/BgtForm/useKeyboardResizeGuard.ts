import { type RefObject, useCallback, useEffect, useRef } from "react";

const RESIZE_WINDOW_MS = 150;

export const useKeyboardResizeGuard = (searchInput: RefObject<HTMLInputElement | null>) => {
	const lastResize = useRef(Number.NEGATIVE_INFINITY);

	useEffect(() => {
		const onResize = () => {
			lastResize.current = performance.now();
		};
		window.addEventListener("resize", onResize);
		return () => window.removeEventListener("resize", onResize);
	}, []);

	return useCallback(
		() =>
			performance.now() - lastResize.current < RESIZE_WINDOW_MS &&
			searchInput.current !== null &&
			document.activeElement === searchInput.current,
		[searchInput],
	);
};
