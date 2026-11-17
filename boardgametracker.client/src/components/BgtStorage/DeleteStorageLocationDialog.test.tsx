import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState, StorageLayout, type StorageLocation } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { DeleteStorageLocationDialog } from "./DeleteStorageLocationDialog";

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

const room = node({ id: 1, name: "Attic", layout: StorageLayout.Free });
const kallax = node({ id: 5, name: "Kallax", parentId: 1, layout: StorageLayout.Shelves, rows: 2, columns: 1 });
const shelf = node({ id: 6, parentId: 5, row: 1 });
const crate = node({ id: 7, name: "Crate", parentId: 1, layout: StorageLayout.Free });
const nodes = [room, kallax, shelf, node({ id: 8, parentId: 5, row: 2 }), crate];

const mocks = vi.hoisted(() => ({ deleteStorageLocation: vi.fn(), games: [] as Game[] }));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({
		nodes,
		locations: [{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null }],
	}),
	useStorageActions: () => ({ deleteStorageLocation: mocks.deleteStorageLocation, isPending: false }),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve(mocks.games) }),
}));

const game = (id: number, storageLocationId: number) =>
	({
		id,
		title: `Game ${id}`,
		image: null,
		state: GameState.Owned,
		storedAtLocationId: 1,
		storageLocationId,
		storagePosition: id,
		boxOrientation: "front",
		isLoaned: false,
		expansions: [],
	}) as unknown as Game;

describe("DeleteStorageLocationDialog", () => {
	beforeEach(() => {
		mocks.deleteStorageLocation.mockReset().mockResolvedValue(undefined);
		mocks.games = [];
	});

	it("says an empty cabinet only loses its shelves", () => {
		renderWithProviders(<DeleteStorageLocationDialog node={kallax} close={vi.fn()} />);

		expect(screen.getByText("delete.slots")).toBeInTheDocument();
		expect(screen.getByText("delete.empty")).toBeInTheDocument();
	});

	it("lists the boxes that move up to the parent, a dozen at most", async () => {
		mocks.games = Array.from({ length: 14 }, (_, index) => game(index + 1, index % 2 ? 6 : 7));
		renderWithProviders(<DeleteStorageLocationDialog node={room} close={vi.fn()} />);

		expect(screen.getByText("delete.inside")).toBeInTheDocument();
		expect(await screen.findByText("14")).toBeInTheDocument();
		expect(screen.getByText("Home")).toBeInTheDocument();
		expect(screen.getAllByRole("listitem")).toHaveLength(13);
		expect(screen.getByText("delete.more")).toBeInTheDocument();
		expect(screen.getByText("delete.keep-order")).toBeInTheDocument();
	});

	it("says nothing about what is inside a space without children", () => {
		renderWithProviders(<DeleteStorageLocationDialog node={crate} close={vi.fn()} />);

		expect(screen.queryByText("delete.inside")).not.toBeInTheDocument();
		expect(screen.queryByText("delete.slots")).not.toBeInTheDocument();
	});

	it("closes and reports back once the delete went through", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		const onDeleted = vi.fn();
		renderWithProviders(<DeleteStorageLocationDialog node={crate} close={close} onDeleted={onDeleted} />);

		await user.click(screen.getByRole("button", { name: "delete.confirm" }));

		expect(mocks.deleteStorageLocation).toHaveBeenCalledWith(7);
		await waitFor(() => expect(close).toHaveBeenCalled());
		expect(onDeleted).toHaveBeenCalled();
	});

	it("stays open when the delete fails", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.deleteStorageLocation.mockRejectedValue(new Error("nope"));
		renderWithProviders(<DeleteStorageLocationDialog node={crate} close={close} />);

		await user.click(screen.getByRole("button", { name: "delete.confirm" }));

		await waitFor(() => expect(mocks.deleteStorageLocation).toHaveBeenCalled());
		expect(close).not.toHaveBeenCalled();
	});
});
