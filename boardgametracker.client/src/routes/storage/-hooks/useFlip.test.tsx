import { render } from "@testing-library/react";
import { useRef } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useFlip } from "./useFlip";

interface Spot {
	key: string;
	x: number;
	w?: number;
}

const Shelf = ({ scope, spots }: { scope: number; spots: Spot[] }) => {
	const ref = useRef<HTMLDivElement>(null);
	useFlip(ref, scope);
	return (
		<div ref={ref}>
			{spots.map((spot) => (
				<div key={spot.key} data-box={spot.key} data-x={spot.x} data-w={spot.w ?? 40}>
					<span />
				</div>
			))}
		</div>
	);
};

const size = (name: string, read: (element: HTMLElement) => number | Element | null) =>
	Object.defineProperty(HTMLElement.prototype, name, {
		configurable: true,
		get() {
			return read(this);
		},
	});

const animate = vi.fn();
const cancel = vi.fn();
const reduced = vi.fn(() => false);
let animateBefore: typeof HTMLElement.prototype.animate;

describe("useFlip", () => {
	beforeEach(() => {
		animate.mockReset();
		cancel.mockReset();
		reduced.mockReturnValue(false);
		animateBefore = HTMLElement.prototype.animate;
		HTMLElement.prototype.animate = animate;
		HTMLElement.prototype.getAnimations = () =>
			[
				{ id: "flip", cancel },
				{ id: "glow", cancel },
			] as unknown as Animation[];
		size("offsetLeft", (element) => Number(element.dataset.x ?? 0));
		size("offsetTop", () => 0);
		size("offsetWidth", (element) => Number(element.dataset.w ?? 0));
		size("offsetHeight", () => 30);
		size("offsetParent", (element) => element.parentElement);
		vi.spyOn(window, "matchMedia").mockImplementation(() => ({ matches: reduced() }) as MediaQueryList);
	});

	afterEach(() => {
		HTMLElement.prototype.animate = animateBefore;
		vi.restoreAllMocks();
	});

	it("slides a box from where it was to where it lands, above the boxes that moved less", () => {
		const { rerender } = render(<Shelf scope={1} spots={[{ key: "a", x: 0 }]} />);
		expect(animate).not.toHaveBeenCalled();

		rerender(<Shelf scope={1} spots={[{ key: "a", x: 100 }]} />);

		expect(cancel).toHaveBeenCalledTimes(1);
		expect(animate).toHaveBeenCalledTimes(2);
		expect(animate.mock.calls[0][0]).toEqual([
			{ transform: "translate(-100px, 0px) scale(1, 1)" },
			{ transform: "none" },
		]);
		expect(animate.mock.calls[0][1]).toMatchObject({ id: "flip", composite: "add" });
		expect(animate.mock.calls[1][0]).toEqual([{ zIndex: 100 }, { zIndex: 100 }]);
	});

	it("morphs a box that turned and fades its label back in", () => {
		const { rerender } = render(<Shelf scope={1} spots={[{ key: "a", x: 0, w: 40 }]} />);

		rerender(<Shelf scope={1} spots={[{ key: "a", x: 0, w: 80 }]} />);

		expect(animate.mock.calls[0][0][0]).toEqual({ transform: "translate(-20px, 0px) scale(0.5, 1)" });
		expect(animate.mock.calls[2][0]).toEqual([{ opacity: 0 }, { opacity: 1 }]);
	});

	it("leaves boxes alone that stayed put, are new or have no size", () => {
		const { rerender } = render(
			<Shelf
				scope={1}
				spots={[
					{ key: "a", x: 0 },
					{ key: "b", x: 50 },
				]}
			/>,
		);

		rerender(
			<Shelf
				scope={1}
				spots={[
					{ key: "a", x: 0 },
					{ key: "b", x: 90, w: 0 },
					{ key: "c", x: 10 },
				]}
			/>,
		);

		expect(animate).not.toHaveBeenCalled();
	});

	it("does not animate when another cabinet is shown or the window was resized", () => {
		const { rerender } = render(<Shelf scope={1} spots={[{ key: "a", x: 0 }]} />);

		rerender(<Shelf scope={2} spots={[{ key: "a", x: 100 }]} />);
		vi.spyOn(window, "innerWidth", "get").mockReturnValue(500);
		rerender(<Shelf scope={2} spots={[{ key: "a", x: 0 }]} />);

		expect(animate).not.toHaveBeenCalled();
	});

	it("keeps still for reduced motion and for browsers without animations", () => {
		reduced.mockReturnValue(true);
		const { rerender } = render(<Shelf scope={1} spots={[{ key: "a", x: 0 }]} />);
		rerender(<Shelf scope={1} spots={[{ key: "a", x: 100 }]} />);

		reduced.mockReturnValue(false);
		HTMLElement.prototype.animate = undefined as unknown as typeof HTMLElement.prototype.animate;
		rerender(<Shelf scope={1} spots={[{ key: "a", x: 0 }]} />);

		expect(animate).not.toHaveBeenCalled();
	});
});
