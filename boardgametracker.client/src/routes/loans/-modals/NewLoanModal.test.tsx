import type { AnyFieldApi } from "@tanstack/react-form";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState, type Player } from "@/models";
import { act, fireEvent, renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import NewLoanModal from "./NewLoanModal";

const mocks = vi.hoisted(() => ({
	saveLoan: vi.fn(),
	players: [] as Player[],
}));
const { saveLoan } = mocks;

const games = [
	{ id: 1, title: "Everdell", image: null, state: GameState.Owned, isLoaned: true },
	{ id: 2, title: "Everdell Duo", image: null, state: GameState.Owned, isLoaned: false },
] as unknown as Game[];

const kathleen = { id: 7, name: "Kathleen", image: null } as unknown as Player;
const nova = { id: 8, name: "Nova", image: null } as unknown as Player;

vi.mock("../-hooks/useNewLoanModal", () => ({
	useNewLoanModal: ({ onSuccess }: { onSuccess: () => void }) => ({
		games,
		players: mocks.players,
		isLoading: false,
		saveLoan: async (data: unknown) => {
			mocks.saveLoan(data);
			onSuccess();
		},
	}),
}));

vi.mock("@/components/BgtForm", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/components/BgtForm")>()),
	BgtSelect: ({
		field,
		label,
		items,
	}: {
		field: AnyFieldApi;
		label: string;
		items: { value: number; label: string }[];
	}) => (
		<label>
			{label}
			<select
				value={String(field.state.value ?? "")}
				onChange={(event) => field.handleChange(Number(event.target.value))}
			>
				<option value="">-</option>
				{items.map((item) => (
					<option key={item.value} value={item.value}>
						{item.label}
					</option>
				))}
			</select>
		</label>
	),
}));

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: ({
		open,
		close,
		onPlayerCreated,
	}: {
		open: boolean;
		close: () => void;
		onPlayerCreated: (player: Player) => void;
	}) =>
		open ? (
			<div>
				<button type="button" onClick={() => onPlayerCreated(nova)}>
					save-new-player
				</button>
				<button type="button" onClick={close}>
					close-new-player
				</button>
			</div>
		) : null,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

describe("NewLoanModal", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.players = [kathleen];
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 12, 0));
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	const dueDateField = () => screen.getByRole("group", { name: "new.due.custom-label" });

	const preset = (name: string | RegExp) => screen.getByRole("radio", { name });

	const segmentValues = (group: HTMLElement) =>
		within(group)
			.getAllByRole("spinbutton")
			.map((segment) => segment.getAttribute("aria-valuenow"));

	it("defaults to a two week due date and switches between presets", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(preset(/^new\.due\.two-weeks · /)).toBeChecked();

		await userEvent.click(preset("new.due.none"));

		expect(preset("new.due.none")).toBeChecked();
		expect(preset("new.due.two-weeks")).not.toBeChecked();
	});

	it("shows the app date field for pick date with the due date of the selected preset", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(screen.queryByRole("group", { name: "new.due.custom-label" })).not.toBeInTheDocument();
		await userEvent.click(preset("new.due.custom"));

		const field = await waitFor(dueDateField);
		expect(within(field).getByLabelText("common:open-calendar")).toBeInTheDocument();
		expect(segmentValues(field)).toEqual(["2026", "10", "25"]);
	});

	it("keeps the due date of the preset that was selected before", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(preset("new.due.one-month"));
		await userEvent.click(preset("new.due.custom"));

		expect(segmentValues(await waitFor(dueDateField))).toEqual(["2026", "11", "11"]);
	});

	it("leaves pick date empty after no due date was selected", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(preset("new.due.none"));
		await userEvent.click(preset("new.due.custom"));

		expect(segmentValues(await waitFor(dueDateField))).toEqual([null, null, null]);
	});

	it("shows an error under the picked date when it is before the loan start", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);
		await userEvent.click(preset("new.due.custom"));
		const field = await waitFor(dueDateField);

		const day = within(field).getAllByRole("spinbutton")[2];
		await userEvent.click(day);
		await userEvent.keyboard("01");

		expect(await screen.findByText("loans:new.end.before-start")).toBeInTheDocument();
	});

	it("moves the preset due date along with the loan start", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);
		await waitFor(() => expect(preset(/^new\.due\.two-weeks · /)).toHaveTextContent("2026-10-25"));

		const day = within(screen.getByRole("group", { name: "new.start.label" })).getAllByRole("spinbutton")[2];
		await userEvent.click(day);
		await userEvent.keyboard("15");

		await waitFor(() => expect(preset(/^new\.due\.two-weeks · /)).toHaveTextContent("2026-10-29"));
	});

	it("puts cancel on the left and the save submit on the right", () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		const footer = document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
		const buttons = within(footer).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "new.save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from cancel without saving", async () => {
		const close = vi.fn();
		renderWithProviders(<NewLoanModal open close={close} />);

		const footer = document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
		await userEvent.click(within(footer).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalled();
		expect(saveLoan).not.toHaveBeenCalled();
	});

	it("does not save without a game and a borrower", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(screen.getByRole("button", { name: "new.save" }));

		await waitFor(() => expect(saveLoan).not.toHaveBeenCalled());
	});

	it("lends the picked game to the picked borrower with the preset due date", async () => {
		const close = vi.fn();
		renderWithProviders(<NewLoanModal open close={close} />);

		await userEvent.selectOptions(screen.getByRole("combobox", { name: "player-session:new.game.label" }), "2");
		await userEvent.selectOptions(screen.getByRole("combobox", { name: "new.player.label" }), "7");
		await userEvent.click(preset("new.due.one-week"));
		await userEvent.click(screen.getByRole("button", { name: "new.save" }));

		await waitFor(() =>
			expect(saveLoan).toHaveBeenCalledWith({
				gameId: 2,
				playerId: 7,
				loanDate: new Date(2026, 9, 11),
				dueDate: new Date(2026, 9, 18),
			}),
		);
		expect(close).toHaveBeenCalled();
	});

	it("selects a player created from the dialog once the player list has it", async () => {
		const { rerender } = renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(screen.getByRole("button", { name: "player-session:new.create-player" }));
		act(() => fireEvent.click(screen.getByRole("button", { name: "save-new-player", hidden: true })));
		act(() => fireEvent.click(screen.getByRole("button", { name: "close-new-player", hidden: true })));
		expect(screen.queryByRole("button", { name: "save-new-player", hidden: true })).not.toBeInTheDocument();

		mocks.players = [kathleen, nova];
		rerender(<NewLoanModal open close={vi.fn()} />);

		await waitFor(() => expect(screen.getByRole("combobox", { name: "new.player.label" })).toHaveValue("8"));
	});

	it("starts over when the dialog is opened again", async () => {
		const { rerender } = renderWithProviders(<NewLoanModal open close={vi.fn()} />);
		await userEvent.click(preset("new.due.none"));

		rerender(<NewLoanModal open={false} close={vi.fn()} />);
		rerender(<NewLoanModal open close={vi.fn()} />);

		await waitFor(() => expect(preset(/^new\.due\.two-weeks · /)).toBeChecked());
	});
});
