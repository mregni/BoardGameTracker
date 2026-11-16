import { type RefObject, useLayoutEffect, useRef } from "react";

interface Box {
	x: number;
	y: number;
	width: number;
	height: number;
}

const measure = (element: HTMLElement, root: HTMLElement): Box => {
	let x = 0;
	let y = 0;
	let node: HTMLElement | null = element;
	while (node && node !== root) {
		x += node.offsetLeft;
		y += node.offsetTop;
		node = node.offsetParent as HTMLElement | null;
	}
	return { x, y, width: element.offsetWidth, height: element.offsetHeight };
};

const timing = { duration: 320, easing: "cubic-bezier(0.16, 1, 0.3, 1)" };

export const useFlip = (ref: RefObject<HTMLElement | null>, scope: number) => {
	const previous = useRef<{ scope: number; boxes: Map<string, Box> } | null>(null);

	useLayoutEffect(() => {
		const root = ref.current;
		if (!root) return;
		const elements = [...root.querySelectorAll<HTMLElement>("[data-box]")];
		const boxes = new Map(elements.map((element) => [element.dataset.box ?? "", measure(element, root)]));
		const before = previous.current?.scope === scope ? previous.current.boxes : null;
		previous.current = { scope, boxes };
		if (!before || typeof root.animate !== "function") return;
		if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

		for (const element of elements) {
			const key = element.dataset.box ?? "";
			const from = before.get(key);
			const to = boxes.get(key);
			if (!from || !to || to.width === 0 || to.height === 0) continue;
			const dx = from.x + from.width / 2 - (to.x + to.width / 2);
			const dy = from.y + from.height / 2 - (to.y + to.height / 2);
			const sx = from.width / to.width;
			const sy = from.height / to.height;
			const resized = Math.abs(sx - 1) > 0.01 || Math.abs(sy - 1) > 0.01;
			if (!resized && Math.abs(dx) < 1 && Math.abs(dy) < 1) continue;
			element.animate([{ transform: `translate(${dx}px, ${dy}px) scale(${sx}, ${sy})` }, { transform: "none" }], {
				...timing,
				composite: "add",
			});
			if (resized) {
				for (const child of element.children) child.animate([{ opacity: 0 }, { opacity: 1 }], timing);
			}
		}
	});
};
