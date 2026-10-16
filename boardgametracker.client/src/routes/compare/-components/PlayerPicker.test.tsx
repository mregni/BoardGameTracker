import type { i18n as I18n } from "i18next";
import { afterAll, beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen, userEvent } from "@/test/test-utils";
import { createCompareI18n, playerOne, playerTwo, reggi, translationFor } from "../-test/compareTestSetup";
import { PlayerPicker } from "./PlayerPicker";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

describe("PlayerPicker", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
		HTMLElement.prototype.hasPointerCapture = vi.fn(() => false);
		HTMLElement.prototype.setPointerCapture = vi.fn();
		HTMLElement.prototype.releasePointerCapture = vi.fn();
		HTMLElement.prototype.scrollIntoView = vi.fn();
	});

	afterAll(() => {
		vi.restoreAllMocks();
	});

	it("leaves out the player chosen on the other side and reports the new choice", async () => {
		const user = userEvent.setup();
		const onPlayerChange = vi.fn();
		render(
			<PlayerPicker
				side="playerOne"
				player={playerOne}
				players={[playerOne, playerTwo, reggi]}
				excludeId={playerTwo.id}
				summary="142 games · 38% won"
				onPlayerChange={onPlayerChange}
			/>,
		);

		const trigger = screen.getByRole("combobox", { name: "Change left player, now Mikhaël" });
		expect(trigger).toHaveTextContent("Mikhaël142 games · 38% won");
		expect(trigger.className).toContain("bg-player-one/10");

		await user.click(trigger);
		expect(screen.queryByRole("option", { name: /Kathleen/ })).toBeNull();
		await user.click(await screen.findByRole("option", { name: /reggi/ }));

		expect(onPlayerChange).toHaveBeenCalledWith(reggi.id);
	});

	it("uses the blue side and its own name on the right", () => {
		render(
			<PlayerPicker
				side="playerTwo"
				player={playerTwo}
				players={[playerOne, playerTwo]}
				excludeId={playerOne.id}
				summary="1 game · 0% won"
				onPlayerChange={vi.fn()}
			/>,
		);

		expect(screen.getByRole("combobox", { name: "Change right player, now Kathleen" }).className).toContain(
			"bg-player-two/10",
		);
	});
});
