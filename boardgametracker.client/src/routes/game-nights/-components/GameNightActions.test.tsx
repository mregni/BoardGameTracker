import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GameNight } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

const { toastError, toastSuccess, settingsMock } = vi.hoisted(() => ({
	toastError: vi.fn(),
	toastSuccess: vi.fn(),
	settingsMock: { current: { publicUrl: "https://games.example.com", emailEnabled: false } },
}));

vi.mock("sonner", () => ({
	toast: { success: toastSuccess, error: toastError },
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: settingsMock.current }),
}));

import { GameNightActions } from "./GameNightActions";

const gameNight = { id: 4, linkId: "abc-123" } as GameNight;

const openMenu = (user: ReturnType<typeof userEvent.setup>) =>
	user.click(screen.getByRole("button", { name: "card.actions" }));

const setClipboard = (clipboard: Partial<Clipboard> | undefined) => {
	Object.defineProperty(navigator, "clipboard", { value: clipboard, configurable: true });
};

describe("GameNightActions", () => {
	beforeEach(() => {
		toastError.mockClear();
		toastSuccess.mockClear();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: false };
	});

	it("copies the invite link and says so", async () => {
		const user = userEvent.setup();
		const writeText = vi.fn().mockResolvedValue(undefined);
		setClipboard({ writeText });
		renderWithProviders(<GameNightActions invitable gameNight={gameNight} onEdit={vi.fn()} onDelete={vi.fn()} />);
		await openMenu(user);

		await user.click(screen.getByRole("menuitem", { name: "card.copy-link" }));

		expect(writeText).toHaveBeenCalledWith("https://games.example.com/rsvp?linkId=abc-123");
		await waitFor(() => expect(toastSuccess).toHaveBeenCalledWith("card.copied"));
		expect(toastError).not.toHaveBeenCalled();
	});

	it("reports a failed copy instead of claiming success when the clipboard is unavailable", async () => {
		const user = userEvent.setup();
		setClipboard(undefined);
		renderWithProviders(<GameNightActions invitable gameNight={gameNight} onEdit={vi.fn()} onDelete={vi.fn()} />);
		await openMenu(user);

		await user.click(screen.getByRole("menuitem", { name: "card.copy-link" }));

		await waitFor(() => expect(toastError).toHaveBeenCalledWith("card.copy-failed"));
		expect(toastSuccess).not.toHaveBeenCalled();
	});

	it("offers no invite actions once the night has started", async () => {
		const user = userEvent.setup();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: true };
		renderWithProviders(
			<GameNightActions invitable={false} gameNight={gameNight} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);
		await openMenu(user);

		expect(screen.queryByRole("menuitem", { name: "card.copy-link" })).not.toBeInTheDocument();
	});

	it("edits and deletes the game night", async () => {
		const user = userEvent.setup();
		const onEdit = vi.fn();
		const onDelete = vi.fn();
		renderWithProviders(<GameNightActions invitable gameNight={gameNight} onEdit={onEdit} onDelete={onDelete} />);
		await openMenu(user);

		await user.click(screen.getByRole("menuitem", { name: "common:edit" }));
		await openMenu(user);
		await user.click(screen.getByRole("menuitem", { name: "common:delete.button" }));

		expect(onEdit).toHaveBeenCalledWith(gameNight);
		expect(onDelete).toHaveBeenCalledWith(gameNight);
	});
});
