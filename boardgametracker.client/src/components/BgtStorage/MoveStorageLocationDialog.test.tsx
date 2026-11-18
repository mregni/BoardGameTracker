import { beforeEach, describe, expect, it, vi } from "vitest";
import { StorageLayout, type StorageLocation } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { MoveStorageLocationDialog } from "./MoveStorageLocationDialog";

const node = (overrides: Partial<StorageLocation> & { id: number }): StorageLocation => ({
	name: null,
	locationId: 1,
	parentId: null,
	layout: StorageLayout.Free,
	rows: null,
	columns: null,
	row: null,
	column: null,
	...overrides,
});

const attic = node({ id: 1, name: "Attic" });
const crate = node({ id: 2, name: "Crate", parentId: 1 });
const inner = node({ id: 3, name: "Inner", parentId: 2 });
const kallax = node({ id: 4, name: "Kallax", layout: StorageLayout.Shelves, rows: 1, columns: 1 });
const nodes = [attic, crate, inner, kallax, node({ id: 5, parentId: 4, layout: StorageLayout.Slot, row: 1 })];

const mocks = vi.hoisted(() => ({ moveStorageLocation: vi.fn() }));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({
		nodes,
		locations: [
			{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null },
			{ id: 2, name: "Cafe", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null },
		],
	}),
	useStorageActions: () => ({ moveStorageLocation: mocks.moveStorageLocation, isPending: false }),
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
			<option value="broken">broken</option>
		</select>
	),
}));

const target = () => screen.getByRole("combobox", { name: "move.to" });
const save = () => screen.getByRole("button", { name: "move.save" });

describe("MoveStorageLocationDialog", () => {
	beforeEach(() => {
		mocks.moveStorageLocation.mockReset().mockResolvedValue(undefined);
	});

	it("only offers free spaces outside the one being moved", () => {
		renderWithProviders(<MoveStorageLocationDialog node={crate} close={vi.fn()} />);

		const offered = screen.getAllByRole("option").map((option) => option.getAttribute("value"));
		expect(offered).toEqual(["2:", "1:", "1:1", "broken"]);
		expect(target()).toHaveValue("1:1");
		expect(save()).toBeDisabled();
	});

	it("moves to another room and closes", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithProviders(<MoveStorageLocationDialog node={crate} close={close} />);

		await user.selectOptions(target(), "2:");
		await user.click(save());

		expect(mocks.moveStorageLocation).toHaveBeenCalledWith({ id: 2, locationId: 2, parentId: null });
		await waitFor(() => expect(close).toHaveBeenCalled());
	});

	it("goes back to the current place when the choice cannot be read", async () => {
		const user = userEvent.setup();
		renderWithProviders(<MoveStorageLocationDialog node={crate} close={vi.fn()} />);

		await user.selectOptions(target(), "2:");
		await user.selectOptions(target(), "broken");

		expect(target()).toHaveValue("1:1");
		expect(save()).toBeDisabled();
	});

	it("stays open when the move fails", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.moveStorageLocation.mockRejectedValue(new Error("nope"));
		renderWithProviders(<MoveStorageLocationDialog node={crate} close={close} />);

		await user.selectOptions(target(), "1:");
		await user.click(save());

		await waitFor(() => expect(mocks.moveStorageLocation).toHaveBeenCalled());
		expect(close).not.toHaveBeenCalled();
	});
});
