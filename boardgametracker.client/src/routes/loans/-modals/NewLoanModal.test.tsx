import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState, type Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import NewLoanModal from "./NewLoanModal";

const { saveLoan } = vi.hoisted(() => ({ saveLoan: vi.fn() }));

const games = [
	{ id: 1, title: "Everdell", image: null, state: GameState.Owned, isLoaned: true },
	{ id: 2, title: "Everdell Duo", image: null, state: GameState.Owned, isLoaned: false },
] as unknown as Game[];

const players = [{ id: 7, name: "Kathleen", image: null }] as unknown as Player[];

vi.mock("../-hooks/useNewLoanModal", () => ({
	useNewLoanModal: () => ({ games, players, isLoading: false, saveLoan }),
}));

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: () => null,
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
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 12, 0));
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	const dueDateField = () => screen.getByRole("group", { name: "new.due.custom-label" });

	const segmentValues = (group: HTMLElement) =>
		within(group)
			.getAllByRole("spinbutton")
			.map((segment) => segment.getAttribute("aria-valuenow"));

	it("defaults to a two week due date and switches between presets", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(screen.getByRole("button", { name: /^new\.due\.two-weeks · / })).toHaveAttribute("aria-pressed", "true");

		await userEvent.click(screen.getByRole("button", { name: "new.due.none" }));

		expect(screen.getByRole("button", { name: "new.due.none" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "new.due.two-weeks" })).toHaveAttribute("aria-pressed", "false");
	});

	it("shows the app date field for pick date, prefilled with the selected preset", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(screen.queryByRole("group", { name: "new.due.custom-label" })).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "new.due.custom" }));

		const field = await waitFor(dueDateField);
		expect(within(field).getByLabelText("common:open-calendar")).toBeInTheDocument();
		expect(segmentValues(field)).toEqual(["2026", "10", "25"]);
	});

	it("prefills pick date from the preset that was selected before", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(screen.getByRole("button", { name: "new.due.one-month" }));
		await userEvent.click(screen.getByRole("button", { name: "new.due.custom" }));

		expect(segmentValues(await waitFor(dueDateField))).toEqual(["2026", "11", "11"]);
	});

	it("prefills pick date two weeks out after no due date was selected", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(screen.getByRole("button", { name: "new.due.none" }));
		await userEvent.click(screen.getByRole("button", { name: "new.due.custom" }));

		expect(segmentValues(await waitFor(dueDateField))).toEqual(["2026", "10", "25"]);
	});

	it("shows an error under the picked date when it is before the loan start", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);
		await userEvent.click(screen.getByRole("button", { name: "new.due.custom" }));
		const field = await waitFor(dueDateField);

		const day = within(field).getAllByRole("spinbutton")[2];
		await userEvent.click(day);
		await userEvent.keyboard("01");

		expect(await screen.findByText("loans:new.end.before-start")).toBeInTheDocument();
	});

	it("does not save without a game and a borrower", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(screen.getByRole("button", { name: "new.save" }));

		await waitFor(() => expect(saveLoan).not.toHaveBeenCalled());
	});
});
