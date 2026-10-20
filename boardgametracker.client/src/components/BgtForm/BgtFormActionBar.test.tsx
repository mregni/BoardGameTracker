import { beforeEach, describe, expect, it } from "vitest";
import { useBottomNavStore } from "@/hooks/useHideBottomNav";
import { renderWithTheme, screen } from "@/test/test-utils";
import { BgtFormActionBar } from "./BgtFormActionBar";

const barOf = (element: HTMLElement) => element.closest("[data-slot='form-action-bar']") as HTMLElement;

describe("BgtFormActionBar", () => {
	beforeEach(() => {
		useBottomNavStore.setState({ hideRequests: 0 });
	});

	it("renders its actions", () => {
		renderWithTheme(
			<BgtFormActionBar>
				<button type="submit">Save</button>
			</BgtFormActionBar>,
		);

		expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
	});

	it("pins to the bottom of the viewport on phones and flows inline from md up", () => {
		renderWithTheme(
			<BgtFormActionBar className="md:mt-2">
				<button type="submit">Save</button>
			</BgtFormActionBar>,
		);

		const bar = barOf(screen.getByRole("button", { name: "Save" }));
		expect(bar).toHaveClass(
			"fixed",
			"inset-x-0",
			"bottom-0",
			"h-(--form-action-bar-h)",
			"pb-[env(safe-area-inset-bottom)]",
			"md:static",
			"md:h-auto",
			"md:p-0",
			"md:mt-2",
		);
	});

	it("reserves room so the last field can scroll above the bar", () => {
		const { container } = renderWithTheme(
			<BgtFormActionBar>
				<button type="submit">Save</button>
			</BgtFormActionBar>,
		);

		const spacer = container.querySelector("[data-slot='form-action-bar-spacer']");
		expect(spacer).toHaveAttribute("aria-hidden", "true");
		expect(spacer).toHaveClass("h-(--form-action-bar-h)", "md:hidden");
	});

	it("hides the bottom nav while it is shown", () => {
		const { unmount } = renderWithTheme(
			<BgtFormActionBar>
				<button type="submit">Save</button>
			</BgtFormActionBar>,
		);

		expect(useBottomNavStore.getState().hideRequests).toBe(1);

		unmount();

		expect(useBottomNavStore.getState().hideRequests).toBe(0);
	});
});
