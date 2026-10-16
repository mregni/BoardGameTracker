import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GameNight } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

const { toastError } = vi.hoisted(() => ({ toastError: vi.fn() }));

vi.mock("sonner", () => ({
	toast: { success: vi.fn(), error: toastError },
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: { publicUrl: "https://games.example.com", emailEnabled: false } }),
}));

vi.mock("@/services/gameNightService", () => ({
	sendInvitesCall: vi.fn(),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

import { GameNightActions } from "./GameNightActions";

const gameNight = { id: 4, linkId: "abc-123" } as GameNight;

const setClipboard = (clipboard: Partial<Clipboard> | undefined) => {
	Object.defineProperty(navigator, "clipboard", { value: clipboard, configurable: true });
};

describe("GameNightActions", () => {
	beforeEach(() => {
		toastError.mockClear();
	});

	it("copies the invite link and says so", async () => {
		const user = userEvent.setup();
		const writeText = vi.fn().mockResolvedValue(undefined);
		setClipboard({ writeText });
		renderWithProviders(<GameNightActions gameNight={gameNight} onEdit={vi.fn()} onDelete={vi.fn()} />);

		await user.click(screen.getByRole("button", { name: "card.copy-link" }));

		expect(writeText).toHaveBeenCalledWith("https://games.example.com/rsvp?linkId=abc-123");
		expect(await screen.findByRole("button", { name: "card.copied" })).toBeInTheDocument();
		expect(toastError).not.toHaveBeenCalled();
	});

	it("reports a failed copy instead of claiming success when the clipboard is unavailable", async () => {
		const user = userEvent.setup();
		setClipboard(undefined);
		renderWithProviders(<GameNightActions gameNight={gameNight} onEdit={vi.fn()} onDelete={vi.fn()} />);

		await user.click(screen.getByRole("button", { name: "card.copy-link" }));

		await waitFor(() => expect(toastError).toHaveBeenCalledWith("card.copy-failed"));
		expect(screen.getByRole("button", { name: "card.copy-link" })).toBeInTheDocument();
	});

	it("edits and deletes the game night", async () => {
		const user = userEvent.setup();
		const onEdit = vi.fn();
		const onDelete = vi.fn();
		renderWithProviders(<GameNightActions gameNight={gameNight} onEdit={onEdit} onDelete={onDelete} />);

		await user.click(screen.getByRole("button", { name: "edit" }));
		await user.click(screen.getByRole("button", { name: "delete.button" }));

		expect(onEdit).toHaveBeenCalledWith(gameNight);
		expect(onDelete).toHaveBeenCalledWith(gameNight);
	});
});
