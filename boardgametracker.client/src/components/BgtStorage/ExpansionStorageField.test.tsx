import { beforeEach, describe, expect, it, vi } from "vitest";
import { BoxOrientation, type Expansion, GameState, StorageItemType, StorageLayout } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import type { StoragePlace } from "@/utils/storage";
import { ExpansionStorageField } from "./ExpansionStorageField";

const mocks = vi.hoisted(() => ({ assignStorage: vi.fn() }));

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
	useStorageActions: () => ({ assignStorage: mocks.assignStorage, isPending: false }),
}));

vi.mock("./StoragePicker", () => ({
	StoragePicker: (props: {
		value: StoragePlace | null;
		onChange: (place: StoragePlace) => void;
		"aria-label": string;
	}) => (
		<button
			type="button"
			aria-label={props["aria-label"]}
			onClick={() => props.onChange({ locationId: 1, storageLocationId: 5 })}
		>
			{props.value ? `${props.value.locationId}:${props.value.storageLocationId}` : "picker.placeholder"}
		</button>
	),
}));

const expansion = (overrides: Partial<Expansion> = {}): Expansion => ({
	id: 7,
	title: "Pearlbrook",
	bggId: 101,
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

const group = () => screen.getByRole("group", { name: "storage:picker.label: Pearlbrook" });

describe("ExpansionStorageField", () => {
	beforeEach(() => {
		mocks.assignStorage.mockReset().mockResolvedValue(undefined);
	});

	it("says only owned and for trade expansions get a place", () => {
		renderWithTheme(
			<ExpansionStorageField expansion={expansion({ state: GameState.Wanted })} baseTitle="Everdell" canWrite />,
		);

		expect(screen.getByText("storage.only-owned")).toBeInTheDocument();
		expect(screen.queryByRole("switch")).not.toBeInTheDocument();
	});

	it("shows a reader the place or the base game's box as text", () => {
		const { unmount } = renderWithTheme(
			<ExpansionStorageField expansion={expansion()} baseTitle="Everdell" canWrite={false} />,
		);
		expect(screen.getByText("storage:place.with-base")).toBeInTheDocument();
		unmount();

		renderWithTheme(
			<ExpansionStorageField
				expansion={expansion({ storedAtLocationId: 1, storageLocationId: 5 })}
				baseTitle="Everdell"
				canWrite={false}
			/>,
		);
		expect(screen.getByText("Home › Kallax")).toBeInTheDocument();
		expect(screen.queryByRole("switch")).not.toBeInTheDocument();
	});

	it("keeps an expansion without a place in the base game's box", () => {
		renderWithTheme(<ExpansionStorageField expansion={expansion()} baseTitle="Everdell" canWrite />);

		expect(within(group()).getByRole("switch", { name: "storage.in-base-box" })).toBeChecked();
		expect(screen.queryByRole("button", { name: "storage:picker.label: Pearlbrook" })).not.toBeInTheDocument();
	});

	it("only saves once a place is picked after taking it out of the box", async () => {
		const user = userEvent.setup();
		renderWithTheme(<ExpansionStorageField expansion={expansion()} baseTitle="Everdell" canWrite />);

		await user.click(screen.getByRole("switch", { name: "storage.in-base-box" }));

		expect(screen.getByRole("switch")).not.toBeChecked();
		expect(mocks.assignStorage).not.toHaveBeenCalled();

		await user.click(screen.getByRole("button", { name: "storage:picker.label: Pearlbrook" }));

		expect(mocks.assignStorage).toHaveBeenCalledWith({
			items: [{ type: StorageItemType.Expansion, id: 7 }],
			locationId: 1,
			storageLocationId: 5,
		});
	});

	it("puts a stored expansion back in the base game's box", async () => {
		const user = userEvent.setup();
		renderWithTheme(
			<ExpansionStorageField
				expansion={expansion({ storedAtLocationId: 1, storageLocationId: 5 })}
				baseTitle="Everdell"
				canWrite
			/>,
		);

		expect(screen.getByRole("switch")).not.toBeChecked();
		expect(screen.getByRole("button", { name: "storage:picker.label: Pearlbrook" })).toHaveTextContent("1:5");

		await user.click(screen.getByRole("switch"));

		await waitFor(() =>
			expect(mocks.assignStorage).toHaveBeenCalledWith({
				items: [{ type: StorageItemType.Expansion, id: 7 }],
				locationId: null,
				storageLocationId: null,
			}),
		);
	});

	it("does not save when switched back before a place was picked", async () => {
		const user = userEvent.setup();
		renderWithTheme(<ExpansionStorageField expansion={expansion()} baseTitle="Everdell" canWrite />);

		await user.click(screen.getByRole("switch"));
		await user.click(screen.getByRole("switch"));

		expect(screen.getByRole("switch")).toBeChecked();
		expect(mocks.assignStorage).not.toHaveBeenCalled();
	});
});
