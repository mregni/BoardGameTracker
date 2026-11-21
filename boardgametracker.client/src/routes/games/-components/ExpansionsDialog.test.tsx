import { describe, expect, it, vi } from "vitest";
import { BoxOrientation, type Expansion, GameState, StorageLayout } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { ExpansionsDialog } from "./ExpansionsDialog";

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({
		nodes: [
			{
				id: 5,
				name: "Kallax",
				locationId: 1,
				parentId: null,
				layout: StorageLayout.Free,
				rows: null,
				columns: null,
				row: null,
				column: null,
			},
		],
		locations: [{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null }],
	}),
	useStorageActions: () => ({ assignStorage: vi.fn(), isPending: false }),
}));

vi.mock("@/components/BgtStorage/StoragePicker", () => ({ StoragePicker: () => null }));

const expansion = (id: number, title: string, overrides: Partial<Expansion> = {}): Expansion => ({
	id,
	title,
	bggId: 100 + id,
	gameId: 9,
	image: null,
	state: GameState.Owned,
	buyingPrice: null,
	soldPrice: null,
	additionDate: null,
	storedAtLocationId: null,
	storageLocationId: null,
	storagePosition: 0,
	boxOrientation: BoxOrientation.Front,
	...overrides,
});

const renderDialog = (overrides: Partial<Parameters<typeof ExpansionsDialog>[0]> = {}) => {
	const props = {
		gameTitle: "Everdell",
		gamePlace: null,
		gamePrice: 45,
		currency: "EUR",
		onUpdateExpansion: vi.fn(),
		expansions: [
			expansion(1, "Pearlbrook", { buyingPrice: 28 }),
			expansion(2, "Homebrew", { bggId: null, state: GameState.Wanted, buyingPrice: 10 }),
		],
		open: true,
		close: vi.fn(),
		canWrite: true,
		bggEnabled: true,
		onAddExpansion: vi.fn(),
		onAddManualExpansion: vi.fn(() => Promise.resolve(true)),
		onDeleteExpansion: vi.fn(),
		...overrides,
	};
	renderWithTheme(<ExpansionsDialog {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
const footerLabels = () =>
	within(footer())
		.getAllByRole("button")
		.map((button) => button.textContent);

describe("ExpansionsDialog", () => {
	it("puts close on the left and add on the right when the user can write and bgg is enabled", () => {
		renderDialog();

		expect(footerLabels()).toEqual(["common:close", "expansions.add"]);
		expect(within(footer()).getByRole("button", { name: "expansions.add" })).toHaveClass("bg-primary/60");
	});

	it("only shows close when bgg is disabled", () => {
		renderDialog({ bggEnabled: false });

		expect(footerLabels()).toEqual(["common:close"]);
	});

	it("only shows close when the user cannot write", () => {
		renderDialog({ canWrite: false });

		expect(footerLabels()).toEqual(["common:close"]);
	});

	it("closes from the footer", async () => {
		const user = userEvent.setup();
		const { close } = renderDialog();

		await user.click(within(footer()).getByRole("button", { name: "common:close" }));

		expect(close).toHaveBeenCalledTimes(1);
	});

	it("starts adding an expansion from the footer", async () => {
		const user = userEvent.setup();
		const { onAddExpansion } = renderDialog();

		await user.click(within(footer()).getByRole("button", { name: "expansions.add" }));

		expect(onAddExpansion).toHaveBeenCalledTimes(1);
	});

	it("lists the expansions and marks the manual one", () => {
		renderDialog();

		expect(screen.getByText("expansions.title (2)")).toBeInTheDocument();
		expect(screen.getByText("Pearlbrook")).toBeInTheDocument();
		expect(screen.getByText("Homebrew")).toBeInTheDocument();
		expect(screen.getAllByText("expansions.manual.badge")).toHaveLength(1);
	});

	it("adds a manual expansion with the trimmed title", async () => {
		const user = userEvent.setup();
		const { onAddManualExpansion } = renderDialog();

		const input = screen.getByRole("textbox", { name: "expansions.manual.placeholder" });
		await user.type(input, "  Spirecrest  ");
		await user.click(screen.getByRole("button", { name: "expansions.manual.add" }));

		expect(onAddManualExpansion).toHaveBeenCalledWith("Spirecrest");
		await waitFor(() => expect(input).toHaveValue(""));
	});

	it("sums the base game and the owned expansions", () => {
		renderDialog();

		const prices = screen.getByText("expansions.prices.base").closest("dl") as HTMLElement;
		expect(within(prices).getByText(/45/)).toBeInTheDocument();
		expect(within(prices).getByText(/28/)).toBeInTheDocument();
		expect(within(prices).getByText(/73/)).toBeInTheDocument();
	});

	it("saves a new price when the field loses focus", async () => {
		const user = userEvent.setup();
		const { onUpdateExpansion } = renderDialog();

		const price = screen.getByRole("textbox", { name: "price.label: Pearlbrook" });
		await user.clear(price);
		await user.type(price, "31,5");
		await user.tab();

		expect(onUpdateExpansion).toHaveBeenCalledWith(expect.objectContaining({ id: 1, buyingPrice: 31.5 }));
	});

	it("does not save when the price did not change", async () => {
		const user = userEvent.setup();
		const { onUpdateExpansion } = renderDialog();

		await user.click(screen.getByRole("textbox", { name: "price.label: Pearlbrook" }));
		await user.tab();

		expect(onUpdateExpansion).not.toHaveBeenCalled();
	});

	it("asks for the sold price only for a sold expansion", () => {
		renderDialog({ expansions: [expansion(1, "Pearlbrook", { state: GameState.Sold, soldPrice: 12 })] });

		expect(screen.getByRole("textbox", { name: "update.sold-price.label: Pearlbrook" })).toHaveValue("12");
	});

	it("says where the base game is stored", () => {
		renderDialog({ gamePlace: { locationId: 1, storageLocationId: 5 } });

		expect(screen.getByText("expansions.description expansions.base-stored")).toBeInTheDocument();
	});

	it("does not mention the base game's place when it has none", () => {
		renderDialog();

		expect(screen.queryByText(/expansions\.base-stored/)).not.toBeInTheDocument();
	});

	it("gives every expansion that can be stored its own storage switch", () => {
		renderDialog();

		const pearlbrook = screen.getByRole("group", { name: "storage:picker.label: Pearlbrook" });
		expect(within(pearlbrook).getByRole("switch", { name: "storage.in-base-box" })).toBeChecked();
		expect(screen.getByText("storage.only-owned")).toBeInTheDocument();
	});

	it("shows a reader the state and price as text, without fields or delete buttons", () => {
		renderDialog({
			canWrite: false,
			expansions: [expansion(1, "Pearlbrook", { state: GameState.Sold, buyingPrice: 28, soldPrice: 12 })],
		});

		expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: /common:delete.button/ })).not.toBeInTheDocument();
		expect(screen.getByText("game:state.sold")).toBeInTheDocument();
		expect(screen.getByText("€28")).toBeInTheDocument();
		expect(screen.getByText("€12")).toBeInTheDocument();
	});

	it("shows a reader where each expansion is stored", () => {
		renderDialog({
			canWrite: false,
			expansions: [
				expansion(1, "Pearlbrook"),
				expansion(2, "Spirecrest", { storedAtLocationId: 1, storageLocationId: 5 }),
			],
		});

		expect(screen.queryByRole("switch")).not.toBeInTheDocument();
		expect(screen.getByText("storage:place.with-base")).toBeInTheDocument();
		expect(screen.getByText("Home › Kallax")).toBeInTheDocument();
	});
});
