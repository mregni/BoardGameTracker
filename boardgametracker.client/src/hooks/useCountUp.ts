import { useEffect, useRef, useState } from "react";
import { useMediaQuery } from "./useMediaQuery";

const DURATION_MS = 800;

export const useCountUp = (target: number): number => {
	const animate = useMediaQuery("(prefers-reduced-motion: no-preference)");
	const [value, setValue] = useState(animate ? 0 : target);
	const current = useRef(value);

	useEffect(() => {
		if (!animate) return;
		const from = current.current;
		const start = performance.now();
		let frame = 0;
		const step = (now: number) => {
			const progress = Math.min(1, (now - start) / DURATION_MS);
			const eased = progress === 1 ? 1 : 1 - 2 ** (-10 * progress);
			current.current = from + (target - from) * eased;
			setValue(current.current);
			if (progress < 1) frame = requestAnimationFrame(step);
		};
		frame = requestAnimationFrame(step);
		return () => cancelAnimationFrame(frame);
	}, [animate, target]);

	return animate ? Math.round(value) : target;
};
