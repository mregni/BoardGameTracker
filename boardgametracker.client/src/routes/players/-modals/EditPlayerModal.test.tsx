import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Player } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { EditPlayerModal } from "./EditPlayerModal";

const mocks = vi.hoisted(() => ({
	updatePlayer: vi.fn(),
	uploadImage: vi.fn(),
	isLoading: false,
}));

vi.mock("../-hooks/usePlayerModal", () => ({
	usePlayerModal: () => ({
		updatePlayer: mocks.updatePlayer,
		uploadImage: mocks.uploadImage,
		isLoading: mocks.isLoading,
	}),
}));

const player = { id: 5, name: "Kathleen", image: null, email: "kathleen@example.com", badges: [] } as Player;

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("EditPlayerModal", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.isLoading = false;
	});

	it("puts cancel on the left and the save submit on the right", () => {
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "update.save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from cancel without saving", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithTheme(<EditPlayerModal open close={close} player={player} />);

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(mocks.updatePlayer).not.toHaveBeenCalled();
	});

	it("saves the changed player from the footer submit and closes", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.updatePlayer.mockResolvedValue(undefined);
		renderWithTheme(<EditPlayerModal open close={close} player={player} />);

		const name = screen.getByLabelText("common:name");
		expect(name).toHaveValue("Kathleen");
		await user.clear(name);
		await user.type(name, "Kat");
		await user.click(within(footer()).getByRole("button", { name: "update.save" }));

		await waitFor(() =>
			expect(mocks.updatePlayer).toHaveBeenCalledWith(
				expect.objectContaining({ id: 5, name: "Kat", email: "kathleen@example.com" }),
			),
		);
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("disables both footer buttons while saving", () => {
		mocks.isLoading = true;
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "update.save" })).toBeDisabled();
	});
});
