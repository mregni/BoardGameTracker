import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Player } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { CreatePlayerModal } from "./CreatePlayerModal";

const mocks = vi.hoisted(() => ({
	savePlayer: vi.fn(),
	uploadImage: vi.fn(),
	isLoading: false,
}));

vi.mock("../-hooks/usePlayerModal", () => ({
	usePlayerModal: () => ({
		savePlayer: mocks.savePlayer,
		uploadImage: mocks.uploadImage,
		isLoading: mocks.isLoading,
	}),
}));

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("CreatePlayerModal", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.isLoading = false;
	});

	it("puts cancel on the left and the save submit on the right", () => {
		renderWithTheme(<CreatePlayerModal open close={vi.fn()} />);

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "new.save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("has a title without a filler subtitle", () => {
		renderWithTheme(<CreatePlayerModal open close={vi.fn()} />);

		expect(screen.getByText("new.title")).toBeInTheDocument();
		expect(screen.queryByText("new.description")).not.toBeInTheDocument();
	});

	it("closes from cancel without saving", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithTheme(<CreatePlayerModal open close={close} />);

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(mocks.savePlayer).not.toHaveBeenCalled();
	});

	it("saves the player from the footer submit and hands it back", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		const onPlayerCreated = vi.fn();
		const saved = { id: 5, name: "Kathleen", image: null, email: null, badges: [] } as Player;
		mocks.savePlayer.mockResolvedValue(saved);
		renderWithTheme(<CreatePlayerModal open close={close} onPlayerCreated={onPlayerCreated} />);

		await user.type(screen.getByLabelText("common:name"), "Kathleen");
		await user.click(within(footer()).getByRole("button", { name: "new.save" }));

		await waitFor(() =>
			expect(mocks.savePlayer).toHaveBeenCalledWith(expect.objectContaining({ id: 0, name: "Kathleen", email: null })),
		);
		expect(mocks.uploadImage).not.toHaveBeenCalled();
		await waitFor(() => expect(onPlayerCreated).toHaveBeenCalledWith(saved));
		expect(close).toHaveBeenCalledTimes(1);
	});

	it("does not save without a name", async () => {
		const user = userEvent.setup();
		renderWithTheme(<CreatePlayerModal open close={vi.fn()} />);

		await user.click(within(footer()).getByRole("button", { name: "new.save" }));

		await waitFor(() => expect(mocks.savePlayer).not.toHaveBeenCalled());
	});

	it("disables both footer buttons while saving", () => {
		mocks.isLoading = true;
		renderWithTheme(<CreatePlayerModal open close={vi.fn()} />);

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "new.save" })).toBeDisabled();
	});
});
