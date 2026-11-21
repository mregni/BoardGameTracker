import { fireEvent } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	BoxOrientation,
	type Expansion,
	type Game,
	GameState,
	StorageItemType,
	StorageLayout,
	type StorageLocation,
} from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import type { StorageItem, StoragePlace } from "@/utils/storage";
import { BoxInspector } from "./BoxInspector";

const mocks = vi.hoisted(() => ({ wide: [false, false], reduced: false }));

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, className }: { children: ReactNode; className?: string }) => (
		<a href="/games" className={className}>
			{children}
		</a>
	),
}));

vi.mock("@/hooks/useMediaQuery", () => ({
	useMediaQuery: (query: string) => (query.includes("80rem") ? mocks.wide[1] : mocks.wide[0]),
}));

vi.mock("@/components/BgtStorage/StoragePicker", () => ({
	StoragePicker: (props: { "aria-label": string; onChange: (place: StoragePlace | null) => void }) => (
		<>
			<button type="button" onClick={() => props.onChange({ locationId: 1, storageLocationId: 6 })}>
				{props["aria-label"]}
			</button>
			<button type="button" onClick={() => props.onChange(null)}>
				clear
			</button>
		</>
	),
}));

const nodes: StorageLocation[] = [
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
];
const locations = [{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null }];

const expansion = (id: number, storedAtLocationId: number | null) =>
	({
		id,
		title: `Exp ${id}`,
		image: null,
		state: GameState.Owned,
		storedAtLocationId,
		storageLocationId: null,
	}) as Expansion;

const game = {
	id: 3,
	title: "Everdell",
	image: null,
	state: GameState.Owned,
	expansions: [expansion(7, null), expansion(8, 1)],
} as unknown as Game;

const box = (overrides: Partial<StorageItem> = {}): StorageItem => ({
	key: "game-3",
	type: StorageItemType.Game,
	id: 3,
	title: "Everdell",
	image: null,
	game,
	expansion: null,
	place: { locationId: 1, storageLocationId: 5 },
	position: 0,
	orientation: BoxOrientation.Front,
	isLoaned: false,
	...overrides,
});

const props = () => ({
	nodes,
	locations,
	canWrite: true,
	canShift: { earlier: false, later: true },
	onClose: vi.fn(),
	onOrientation: vi.fn(),
	onShift: vi.fn(),
	onMove: vi.fn(),
});

const scrolled = vi.fn();

describe("BoxInspector", () => {
	beforeEach(() => {
		mocks.wide = [true, true];
		mocks.reduced = false;
		scrolled.mockReset();
		Element.prototype.scrollIntoView = scrolled;
		vi.spyOn(window, "matchMedia").mockImplementation(
			(query) => ({ matches: query.includes("reduce") && mocks.reduced }) as MediaQueryList,
		);
	});

	afterEach(() => vi.restoreAllMocks());

	it("shows where a game box is and what is still in it", () => {
		renderWithTheme(<BoxInspector item={box({ isLoaned: true })} {...props()} />);

		expect(screen.getByText("Everdell")).toBeInTheDocument();
		expect(screen.getByText("Home › Kallax")).toBeInTheDocument();
		expect(screen.getByText("box.in-box, box.elsewhere")).toBeInTheDocument();
		expect(screen.getByText("box.loaned")).toBeInTheDocument();
		expect(screen.queryByText("box.expansion-of")).not.toBeInTheDocument();
		expect(screen.getByText("box.open")).toBeInTheDocument();
	});

	it("leaves out the elsewhere count when every expansion is in the box", () => {
		const together = { ...game, expansions: [expansion(7, null)] } as Game;
		renderWithTheme(<BoxInspector item={box({ game: together })} {...props()} />);

		expect(screen.getByText("box.in-box")).toBeInTheDocument();
	});

	it("names the base game for an expansion box and skips the expansions row", () => {
		const own = expansion(8, 1);
		renderWithTheme(
			<BoxInspector
				item={box({ key: "expansion-8", type: StorageItemType.Expansion, expansion: own, title: "Exp 8" })}
				{...props()}
			/>,
		);

		expect(screen.getByText("box.expansion-of")).toBeInTheDocument();
		expect(screen.queryByText("box.expansions")).not.toBeInTheDocument();
	});

	it("turns, shifts, moves and closes the box", async () => {
		const user = userEvent.setup();
		const handlers = props();
		renderWithTheme(<BoxInspector item={box()} {...handlers} />);

		await user.click(screen.getByRole("button", { name: "orientation.flat" }));
		expect(handlers.onOrientation).toHaveBeenCalledWith(BoxOrientation.Flat);

		expect(screen.getByRole("button", { name: "box.earlier" })).toBeDisabled();
		await user.click(screen.getByRole("button", { name: "box.later" }));
		expect(handlers.onShift).toHaveBeenCalledWith(1);

		await user.click(screen.getByRole("button", { name: "clear" }));
		expect(handlers.onMove).not.toHaveBeenCalled();
		await user.click(screen.getByRole("button", { name: "box.move-item" }));
		expect(handlers.onMove).toHaveBeenCalledWith({ locationId: 1, storageLocationId: 6 });

		await user.click(screen.getByRole("button", { name: "box.close" }));
		fireEvent.keyDown(screen.getByRole("heading"), { key: "Enter" });
		fireEvent.keyDown(screen.getByRole("heading"), { key: "Escape" });
		expect(handlers.onClose).toHaveBeenCalledTimes(2);
	});

	it("moves earlier when the box is not first", async () => {
		const user = userEvent.setup();
		const handlers = { ...props(), canShift: { earlier: true, later: false } };
		renderWithTheme(<BoxInspector item={box()} {...handlers} />);

		await user.click(screen.getByRole("button", { name: "box.earlier" }));
		expect(handlers.onShift).toHaveBeenCalledWith(-1);
	});

	it("only shows a reader where the box is", () => {
		renderWithTheme(<BoxInspector item={box()} {...props()} canWrite={false} />);

		expect(screen.queryByRole("button", { name: "orientation.flat" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "box.later" })).not.toBeInTheDocument();
	});

	it("hides the shift buttons when the box cannot move either way", () => {
		renderWithTheme(<BoxInspector item={box()} {...props()} canShift={{ earlier: false, later: false }} />);

		expect(screen.queryByRole("button", { name: "box.earlier" })).not.toBeInTheDocument();
	});

	it("on a phone scrolls the box to the top and focuses the sheet", () => {
		mocks.wide = [false, false];
		document.body.insertAdjacentHTML("beforeend", '<div data-box="game-3"></div>');
		renderWithTheme(<BoxInspector item={box()} {...props()} />);

		expect(scrolled).toHaveBeenCalledWith({ block: "start", behavior: "smooth" });
		expect(screen.getByRole("heading")).toHaveFocus();
		document.querySelector('[data-box="game-3"]')?.remove();
	});

	it("on a tablet brings the panel into view, without motion when that is asked for", () => {
		mocks.wide = [true, false];
		mocks.reduced = true;
		renderWithTheme(<BoxInspector item={box()} {...props()} />);

		expect(scrolled).toHaveBeenCalledWith({ block: "nearest", behavior: "auto" });
	});

	it("leaves the page alone on a wide screen", () => {
		renderWithTheme(<BoxInspector item={box()} {...props()} />);

		expect(scrolled).not.toHaveBeenCalled();
	});
});
