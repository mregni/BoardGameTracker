import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	saveExpansions: vi.fn(),
}));

vi.mock("../-hooks/useExpansionSelectorModal", () => ({
	useExpansionSelectorModal: () => ({
		expansions: [
			{ id: 11, value: "Base expansion" },
			{ id: 22, value: "Second expansion" },
			{ id: 33, value: "Third expansion" },
		],
		isLoading: false,
		isPending: false,
		saveExpansions: mocks.saveExpansions,
	}),
}));

import { ExpansionSelectorModal } from "./ExpansionSelectorModal";

describe("ExpansionSelectorModal", () => {
	beforeEach(() => {
		mocks.saveExpansions.mockReset().mockResolvedValue(undefined);
	});

	it("shows each tick and saves every selected expansion, not only the last click", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithProviders(<ExpansionSelectorModal open close={close} gameId={5} selectedExpansions={[11]} />);

		await user.click(screen.getByRole("checkbox", { name: "Second expansion" }));
		await user.click(screen.getByRole("checkbox", { name: "Third expansion" }));

		expect(screen.getByRole("checkbox", { name: "Base expansion" })).toBeChecked();
		expect(screen.getByRole("checkbox", { name: "Second expansion" })).toBeChecked();
		expect(screen.getByRole("checkbox", { name: "Third expansion" })).toBeChecked();

		await user.click(screen.getByRole("button", { name: "expansions.update" }));

		expect(mocks.saveExpansions).toHaveBeenCalledWith({ gameId: 5, expansionBggIds: [11, 22, 33] });
	});

	it("puts cancel on the left and update on the right of the footer", () => {
		renderWithProviders(<ExpansionSelectorModal open close={vi.fn()} gameId={5} selectedExpansions={[]} />);

		const footer = document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
		const buttons = within(footer).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "expansions.update"]);
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from cancel without saving", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithProviders(<ExpansionSelectorModal open close={close} gameId={5} selectedExpansions={[]} />);

		await user.click(screen.getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(mocks.saveExpansions).not.toHaveBeenCalled();
	});

	it("closes after the update is saved", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithProviders(<ExpansionSelectorModal open close={close} gameId={5} selectedExpansions={[11]} />);

		await user.click(screen.getByRole("button", { name: "expansions.update" }));

		expect(mocks.saveExpansions).toHaveBeenCalledWith({ gameId: 5, expansionBggIds: [11] });
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("unticks an expansion that was already linked", async () => {
		const user = userEvent.setup();
		renderWithProviders(<ExpansionSelectorModal open close={vi.fn()} gameId={5} selectedExpansions={[11, 22]} />);

		await user.click(screen.getByRole("checkbox", { name: "Base expansion" }));
		await user.click(screen.getByRole("button", { name: "expansions.update" }));

		expect(screen.getByRole("checkbox", { name: "Base expansion" })).not.toBeChecked();
		expect(mocks.saveExpansions).toHaveBeenCalledWith({ gameId: 5, expansionBggIds: [22] });
	});
});
