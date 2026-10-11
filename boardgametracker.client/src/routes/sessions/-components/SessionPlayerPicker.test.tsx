import { describe, expect, it, vi } from "vitest";
import type { Player } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor } from "@/test/test-utils";
import { SessionPlayerPicker } from "./SessionPlayerPicker";

const player = (id: number, name: string) => ({ id, name, image: null }) as unknown as Player;

const players = [
	player(1, "Mikhael"),
	player(2, "Kathleen"),
	player(3, "Reggi"),
	player(4, "Karel"),
	player(5, "Nova"),
];

const renderPicker = (overrides: Partial<Parameters<typeof SessionPlayerPicker>[0]> = {}) => {
	const props = {
		inputId: "players",
		players,
		selectedPlayerIds: [],
		frequentPlayerIds: [3, 1, 2],
		disabled: false,
		hasErrors: false,
		onAddPlayer: vi.fn(),
		onCreatePlayer: vi.fn(),
		...overrides,
	};
	renderWithTheme(<SessionPlayerPicker {...props} />);
	return props;
};

const optionNames = () => screen.getAllByRole("option").map((x) => x.getAttribute("aria-label"));

describe("SessionPlayerPicker", () => {
	it("suggests the most frequent players that are not chosen yet", async () => {
		const user = userEvent.setup();
		renderPicker({ selectedPlayerIds: [1] });

		await user.click(screen.getByRole("combobox"));

		expect(optionNames()).toEqual(["Reggi", "Kathleen"]);
	});

	it("falls back to the first players alphabetically without frequent players", async () => {
		const user = userEvent.setup();
		renderPicker({ frequentPlayerIds: [], selectedPlayerIds: [4], players: [...players, player(6, "Anna")] });

		await user.click(screen.getByRole("combobox"));

		expect(optionNames()).toEqual(["Anna", "Kathleen", "Mikhael", "Nova", "Reggi"]);
	});

	it("searches all players while typing and offers to create a new one", async () => {
		const user = userEvent.setup();
		renderPicker();

		await user.type(screen.getByRole("combobox"), "ka");

		expect(optionNames()).toEqual(["Kathleen", "Karel", "form.players.create"]);
	});

	it("does not offer to create a player whose name already exists", async () => {
		const user = userEvent.setup();
		renderPicker();

		await user.type(screen.getByRole("combobox"), "nova");

		expect(optionNames()).toEqual(["Nova"]);
	});

	it("adds the highlighted player with Enter and clears the search", async () => {
		const user = userEvent.setup();
		const props = renderPicker();

		await user.type(screen.getByRole("combobox"), "kar{Enter}");

		expect(props.onAddPlayer).toHaveBeenCalledWith(4);
		expect(screen.getByRole("combobox")).toHaveValue("");
	});

	it("creates a new player inline and adds it", async () => {
		const user = userEvent.setup();
		const onCreatePlayer = vi.fn().mockResolvedValue(player(9, "Zoe"));
		const props = renderPicker({ onCreatePlayer });

		await user.type(screen.getByRole("combobox"), "Zoe");
		await user.click(screen.getByRole("option", { name: "form.players.create" }));

		expect(onCreatePlayer).toHaveBeenCalledWith("Zoe");
		await waitFor(() => expect(props.onAddPlayer).toHaveBeenCalledWith(9));
	});
});
