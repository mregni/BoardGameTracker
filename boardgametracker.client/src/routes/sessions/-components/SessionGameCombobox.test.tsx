import type { AnyFieldApi } from "@tanstack/react-form";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import type { Game } from "@/models";
import { renderWithTheme, screen, userEvent, within } from "@/test/test-utils";
import { SessionGameCombobox } from "./SessionGameCombobox";

const game = (id: number, title: string) => ({ id, title, image: null }) as unknown as Game;

const games = [game(1, "Azul"), game(2, "Carcassonne"), game(3, "Catan"), game(4, "Everdell")];

const Harness = ({ onChange = vi.fn(), initial = 0 }: { onChange?: (value: number) => void; initial?: number }) => {
	const [value, setValue] = useState(initial);
	const field = {
		state: { value, meta: { errors: [] } },
		handleChange: (next: number) => {
			setValue(next);
			onChange(next);
		},
		handleBlur: vi.fn(),
	} as unknown as AnyFieldApi;

	return (
		<SessionGameCombobox field={field} label="Game" games={games} recentGameIds={[3, 4]} placeholder="Choose game" />
	);
};

const optionNames = () => screen.getAllByRole("option").map((x) => x.getAttribute("aria-label"));

describe("SessionGameCombobox", () => {
	it("names the trigger with the label", () => {
		renderWithTheme(<Harness />);

		expect(screen.getByRole("button", { name: "Game" })).toHaveAttribute("aria-haspopup", "listbox");
	});

	it("shows recently played games above all games when opened", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness />);

		await user.click(screen.getByRole("button", { name: "Game" }));

		const recent = screen.getByRole("group", { name: "form.game.recent" });
		const all = screen.getByRole("group", { name: "form.game.all" });
		expect(
			within(recent)
				.getAllByRole("option")
				.map((x) => x.getAttribute("aria-label")),
		).toEqual(["Catan", "Everdell"]);
		expect(within(all).getAllByRole("option")).toHaveLength(4);
		expect(screen.getByRole("combobox")).toHaveFocus();
	});

	it("hides the recent group and filters all games while typing", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness />);

		await user.click(screen.getByRole("button", { name: "Game" }));
		await user.type(screen.getByRole("combobox"), "ca");

		expect(screen.queryByRole("group", { name: "form.game.recent" })).not.toBeInTheDocument();
		expect(optionNames()).toEqual(["Carcassonne", "Catan"]);

		await user.clear(screen.getByRole("combobox"));

		expect(screen.getByRole("group", { name: "form.game.recent" })).toBeInTheDocument();
	});

	it("shows a message when no game matches", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness />);

		await user.click(screen.getByRole("button", { name: "Game" }));
		await user.type(screen.getByRole("combobox"), "zzz");

		expect(screen.queryAllByRole("option")).toHaveLength(0);
		expect(screen.getByText("form.game.no-results")).toBeInTheDocument();
	});

	it("selects a game with the keyboard and returns focus to the trigger", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		renderWithTheme(<Harness onChange={onChange} />);

		screen.getByRole("button", { name: "Game" }).focus();
		await user.keyboard("{ArrowDown}");
		await user.keyboard("{ArrowDown}{Enter}");

		expect(onChange).toHaveBeenCalledWith(4);
		expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Game" })).toHaveFocus();
		expect(screen.getByRole("button", { name: "Game" })).toHaveTextContent("Everdell");
	});

	it("closes on Escape without changing the selection", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		renderWithTheme(<Harness onChange={onChange} initial={1} />);

		await user.click(screen.getByRole("button", { name: "Game" }));
		await user.keyboard("{Escape}");

		expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
		expect(onChange).not.toHaveBeenCalled();
	});

	it("selects a game by clicking it", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		renderWithTheme(<Harness onChange={onChange} />);

		await user.click(screen.getByRole("button", { name: "Game" }));
		await user.type(screen.getByRole("combobox"), "azu");
		await user.click(screen.getByRole("option", { name: "Azul" }));

		expect(onChange).toHaveBeenCalledWith(1);
	});
});
