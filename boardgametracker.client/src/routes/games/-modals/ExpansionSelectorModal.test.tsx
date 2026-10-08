import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

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

	it("unticks an expansion that was already linked", async () => {
		const user = userEvent.setup();
		renderWithProviders(<ExpansionSelectorModal open close={vi.fn()} gameId={5} selectedExpansions={[11, 22]} />);

		await user.click(screen.getByRole("checkbox", { name: "Base expansion" }));
		await user.click(screen.getByRole("button", { name: "expansions.update" }));

		expect(screen.getByRole("checkbox", { name: "Base expansion" })).not.toBeChecked();
		expect(mocks.saveExpansions).toHaveBeenCalledWith({ gameId: 5, expansionBggIds: [22] });
	});
});
