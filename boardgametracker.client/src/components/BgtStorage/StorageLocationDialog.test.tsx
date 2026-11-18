import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState, StorageLayout, type StorageLocation } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { StorageLocationDialog } from "./StorageLocationDialog";

const node = (overrides: Partial<StorageLocation> & { id: number }): StorageLocation => ({
	name: null,
	locationId: 1,
	parentId: null,
	layout: StorageLayout.Slot,
	rows: null,
	columns: null,
	row: null,
	column: null,
	...overrides,
});

const kallax = node({ id: 5, name: "Kallax", layout: StorageLayout.Grid, rows: 2, columns: 2 });
const cupboard = node({ id: 10, name: "Cupboard", layout: StorageLayout.Free });
const cell = node({ id: 9, parentId: 5, row: 2, column: 2 });
const nodes = [kallax, node({ id: 6, parentId: 5, row: 1, column: 1 }), cell, cupboard];

const mocks = vi.hoisted(() => ({
	createStorageLocation: vi.fn(),
	updateStorageLocation: vi.fn(),
	games: [] as Game[],
}));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({
		nodes,
		locations: [{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null }],
	}),
	useStorageActions: () => ({
		createStorageLocation: mocks.createStorageLocation,
		updateStorageLocation: mocks.updateStorageLocation,
		isPending: false,
	}),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve(mocks.games) }),
}));

vi.mock("@/components/BgtForm", async (original) => ({
	...(await original<typeof import("@/components/BgtForm")>()),
	BgtSimpleSelect: (props: {
		label: string;
		items: { value: string; label: string }[];
		value?: string;
		onValueChange: (value: string) => void;
	}) => (
		<select aria-label={props.label} value={props.value} onChange={(event) => props.onValueChange(event.target.value)}>
			{props.items.map((item) => (
				<option key={item.value} value={item.value}>
					{item.label}
				</option>
			))}
		</select>
	),
}));

const storedGame = {
	id: 3,
	title: "Brass",
	image: null,
	state: GameState.Owned,
	storedAtLocationId: 1,
	storageLocationId: 9,
	storagePosition: 0,
	boxOrientation: "front",
	isLoaned: false,
	expansions: [],
} as unknown as Game;

const field = (name: string) => screen.getByRole("spinbutton", { name });
const create = () => screen.getByRole("button", { name: "form.create" });

describe("StorageLocationDialog", () => {
	beforeEach(() => {
		mocks.createStorageLocation.mockReset().mockResolvedValue(node({ id: 11, name: "Shelf" }));
		mocks.updateStorageLocation.mockReset().mockResolvedValue(undefined);
		mocks.games = [];
	});

	it("creates a grid in the first place and hands back what it made", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		const onCreated = vi.fn();
		renderWithProviders(
			<StorageLocationDialog open close={close} mode={{ kind: "create", parent: null }} onCreated={onCreated} />,
		);

		expect(screen.getByRole("combobox", { name: "form.inside" })).toHaveValue("1:");
		expect(screen.queryByRole("option", { name: "Home › Kallax" })).not.toBeInTheDocument();
		expect(create()).toBeDisabled();

		await user.type(screen.getByRole("textbox", { name: "form.name" }), "  Billy  ");
		await user.click(screen.getByRole("button", { name: "layout.grid" }));
		await user.clear(field("form.rows"));
		await user.type(field("form.rows"), "3");
		await user.clear(field("form.columns"));
		await user.type(field("form.columns"), "2");
		expect(screen.getByText("form.creates-cells")).toBeInTheDocument();
		await user.click(create());

		expect(mocks.createStorageLocation).toHaveBeenCalledWith({
			name: "Billy",
			locationId: 1,
			parentId: null,
			layout: StorageLayout.Grid,
			rows: 3,
			columns: 2,
		});
		await waitFor(() => expect(close).toHaveBeenCalled());
		expect(onCreated).toHaveBeenCalledWith(expect.objectContaining({ id: 11 }));
	});

	it("starts inside the place it was opened from and makes shelves one column wide", async () => {
		const user = userEvent.setup();
		renderWithProviders(
			<StorageLocationDialog
				open
				close={vi.fn()}
				mode={{ kind: "create", parent: { locationId: 1, storageLocationId: 10 } }}
			/>,
		);

		expect(screen.getByRole("combobox", { name: "form.inside" })).toHaveValue("1:10");
		await user.type(screen.getByRole("textbox", { name: "form.name" }), "Hallway");
		await user.click(screen.getByRole("button", { name: "layout.shelves" }));
		expect(screen.queryByRole("spinbutton", { name: "form.columns" })).not.toBeInTheDocument();
		expect(screen.getByText("form.creates-shelves")).toBeInTheDocument();
		await user.click(create());

		expect(mocks.createStorageLocation).toHaveBeenCalledWith(
			expect.objectContaining({ parentId: 10, layout: StorageLayout.Shelves, rows: 4, columns: 1 }),
		);
	});

	it("falls back to the first place when the given parent cannot hold storage", () => {
		renderWithProviders(
			<StorageLocationDialog
				open
				close={vi.fn()}
				mode={{ kind: "create", parent: { locationId: 1, storageLocationId: 5 } }}
			/>,
		);

		expect(screen.getByRole("combobox", { name: "form.inside" })).toHaveValue("1:");
	});

	it("saves a free space without a size into the picked place", async () => {
		const user = userEvent.setup();
		renderWithProviders(<StorageLocationDialog open close={vi.fn()} mode={{ kind: "create", parent: null }} />);

		await user.selectOptions(screen.getByRole("combobox", { name: "form.inside" }), "1:10");
		await user.type(screen.getByRole("textbox", { name: "form.name" }), "Drawer");
		await user.click(create());

		expect(mocks.createStorageLocation).toHaveBeenCalledWith(
			expect.objectContaining({ parentId: 10, layout: StorageLayout.Free, rows: null, columns: null }),
		);
	});

	it.each([
		["0", "2"],
		["21", "2"],
		["", "2"],
		["2", "0"],
	])("does not create a grid of %s by %s", async (rows, columns) => {
		const user = userEvent.setup();
		renderWithProviders(<StorageLocationDialog open close={vi.fn()} mode={{ kind: "create", parent: null }} />);

		await user.type(screen.getByRole("textbox", { name: "form.name" }), "Billy");
		await user.click(screen.getByRole("button", { name: "layout.grid" }));
		await user.clear(field("form.rows"));
		if (rows) await user.type(field("form.rows"), rows);
		await user.clear(field("form.columns"));
		await user.type(field("form.columns"), columns);

		expect(screen.queryByText("form.creates-cells")).not.toBeInTheDocument();
		expect(create()).toBeDisabled();
		await user.click(create());
		expect(mocks.createStorageLocation).not.toHaveBeenCalled();
	});

	it("stays open when creating fails", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.createStorageLocation.mockRejectedValue(new Error("nope"));
		renderWithProviders(<StorageLocationDialog open close={close} mode={{ kind: "create", parent: null }} />);

		await user.type(screen.getByRole("textbox", { name: "form.name" }), "Billy");
		await user.click(create());

		await waitFor(() => expect(mocks.createStorageLocation).toHaveBeenCalled());
		expect(close).not.toHaveBeenCalled();
	});

	it("warns how many boxes a smaller cabinet moves out and saves the new size", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.games = [storedGame];
		renderWithProviders(<StorageLocationDialog open close={close} mode={{ kind: "edit", node: kallax }} />);

		expect(screen.getByText("form.edit-title")).toBeInTheDocument();
		expect(screen.queryByRole("combobox", { name: "form.inside" })).not.toBeInTheDocument();
		expect(screen.getByRole("textbox", { name: "form.name" })).toHaveValue("Kallax");

		await user.clear(field("form.rows"));
		await user.type(field("form.rows"), "1");
		expect(await screen.findByRole("status")).toHaveTextContent("form.shrink");
		expect(screen.queryByText("form.creates-cells")).not.toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "form.save" }));

		expect(mocks.updateStorageLocation).toHaveBeenCalledWith({ id: 5, name: "Kallax", rows: 1, columns: 2 });
		await waitFor(() => expect(close).toHaveBeenCalled());
	});

	it("lets a slot be saved without a name and without a size", async () => {
		const user = userEvent.setup();
		renderWithProviders(<StorageLocationDialog open close={vi.fn()} mode={{ kind: "edit", node: cell }} />);

		expect(screen.getByText("form.slot-name")).toBeInTheDocument();
		expect(screen.queryByRole("spinbutton")).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "form.save" }));

		expect(mocks.updateStorageLocation).toHaveBeenCalledWith({ id: 9, name: null, rows: null, columns: null });
	});

	it("needs a name to rename a free space", async () => {
		const user = userEvent.setup();
		renderWithProviders(<StorageLocationDialog open close={vi.fn()} mode={{ kind: "edit", node: cupboard }} />);

		await user.clear(screen.getByRole("textbox", { name: "form.name" }));

		expect(screen.getByRole("button", { name: "form.save" })).toBeDisabled();
	});
});
