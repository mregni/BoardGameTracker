import { describe, expect, it, vi } from "vitest";
import type { Game, Location, Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { CreateGameNightModal } from "./CreateGameNightModal";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

const players = [{ id: 1, name: "Host", image: null }] as unknown as Player[];
const games = [{ id: 9, title: "Everdell", image: null }] as unknown as Game[];
const locations = [{ id: 3, name: "Living room", playCount: 0 }] as Location[];

const renderModal = (overrides: Partial<Parameters<typeof CreateGameNightModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		players,
		games,
		locations,
		isLoading: false,
		onSave: vi.fn(() => Promise.resolve()),
		...overrides,
	};
	renderWithProviders(<CreateGameNightModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

const selectTrigger = (label: string) => screen.getByRole("combobox", { name: label });

describe("CreateGameNightModal", () => {
	it("puts cancel on the left and the submit for the game night form on the right", () => {
		renderModal();

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "create.save"]);
		expect(buttons[1]).toHaveClass("bg-primary/60");
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveAttribute("form", "game-night-form");
		expect(document.getElementById("game-night-form")).toBeInTheDocument();
	});

	it("closes when cancel is clicked", async () => {
		const user = userEvent.setup();
		const { close } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
	});

	it("submits the form from the footer and does not save an incomplete game night", async () => {
		const user = userEvent.setup();
		const { onSave } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "create.save" }));

		expect(await screen.findByText("game-nights:validation.title-required")).toBeInTheDocument();
		await waitFor(() => expect(onSave).not.toHaveBeenCalled());
	});

	it("disables both footer buttons while saving", () => {
		renderModal({ isLoading: true });

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "create.save" })).toBeDisabled();
	});

	it("shows the placeholders of the host, location and multi-select fields", () => {
		renderModal();

		expect(selectTrigger("form.host.label")).toHaveTextContent("form.host.placeholder");
		expect(selectTrigger("form.location.label")).toHaveTextContent("form.location.placeholder");
		expect(selectTrigger("form.invited-players.label")).toHaveTextContent("form.invited-players.description");
		expect(selectTrigger("form.suggested-games.label")).toHaveTextContent("form.suggested-games.description");
	});

	it("truncates the value of a select and keeps it left aligned", () => {
		renderModal();

		const value = within(selectTrigger("form.host.label")).getByText("form.host.placeholder");
		expect(value.parentElement).toHaveClass("min-w-0", "flex-1", "truncate", "text-left");
	});
});
