import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { GameForm } from "./GameForm";

const mocks = vi.hoisted(() => ({
	language: "nl-BE",
	back: vi.fn(),
}));

vi.mock("react-i18next", () => ({
	useTranslation: () => ({
		t: (key: string) => key,
		i18n: { language: mocks.language },
	}),
}));

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: mocks.back } }),
}));

vi.mock("../-hooks/useGameForm", () => ({
	useGameForm: () => ({ settings: { currency: "€" }, uploadImage: vi.fn(), isLoading: false }),
}));

const createGame = (overrides: Partial<Game> = {}): Game => ({
	id: 3,
	title: "Catan",
	description: "Trade and build settlements",
	yearPublished: 1995,
	image: "catan.jpg",
	shopUrl: null,
	changeDetectionWatchId: null,
	language: null,
	minPlayers: 2,
	maxPlayers: 4,
	minPlayTime: 30,
	maxPlayTime: 60,
	minAge: 10,
	rating: 8.1,
	weight: 2.4,
	bggId: 13,
	state: GameState.Owned,
	isLoaned: false,
	expansions: [],
	categories: [],
	mechanics: [],
	people: [],
	hasScoring: true,
	buyingPrice: 45.5,
	soldPrice: null,
	additionDate: null,
	...overrides,
});

const renderForm = (game: Game = createGame()) => {
	const onClick = vi.fn().mockResolvedValue(undefined);
	renderWithProviders(<GameForm onClick={onClick} buttonText="Save" disabled={false} game={game} title="Edit" />);
	return { onClick };
};

const replace = async (input: HTMLElement, value: string) => {
	const user = userEvent.setup();
	await user.clear(input);
	if (value) {
		await user.type(input, value);
	}
	await user.tab();
};

describe("GameForm culture decimals", () => {
	beforeEach(() => {
		mocks.language = "nl-BE";
		vi.clearAllMocks();
	});

	it("shows rating, weight and price with the nl-BE decimal comma", () => {
		renderForm();

		expect(screen.getByLabelText("update.rating.label")).toHaveValue("8,1");
		expect(screen.getByLabelText("update.weight.label")).toHaveValue("2,4");
		expect(screen.getByLabelText("price.label")).toHaveValue("45,5");
	});

	it("shows the same values with a decimal point for en-US", () => {
		mocks.language = "en-US";
		renderForm();

		expect(screen.getByLabelText("update.rating.label")).toHaveValue("8.1");
		expect(screen.getByLabelText("update.weight.label")).toHaveValue("2.4");
		expect(screen.getByLabelText("price.label")).toHaveValue("45.5");
	});

	it("uses a decimal keyboard on text inputs", () => {
		renderForm();

		const rating = screen.getByLabelText("update.rating.label");
		expect(rating).toHaveAttribute("type", "text");
		expect(rating).toHaveAttribute("inputmode", "decimal");
	});

	it("saves values typed with a comma or a point as numbers", async () => {
		const user = userEvent.setup();
		const { onClick } = renderForm();

		await replace(screen.getByLabelText("update.rating.label"), "7,5");
		await replace(screen.getByLabelText("update.weight.label"), "3.25");
		await replace(screen.getByLabelText("price.label"), "39,95");

		expect(screen.getByLabelText("update.weight.label")).toHaveValue("3,25");

		await user.click(screen.getByRole("button", { name: "Save" }));

		await waitFor(() => expect(onClick).toHaveBeenCalledTimes(1));
		expect(onClick).toHaveBeenCalledWith(expect.objectContaining({ rating: 7.5, weight: 3.25, buyingPrice: 39.95 }));
	});

	it("ignores letters while typing and clears an emptied value", async () => {
		const user = userEvent.setup();
		const { onClick } = renderForm();

		const rating = screen.getByLabelText("update.rating.label");
		await user.type(rating, "x");
		expect(rating).toHaveValue("8,1");

		await replace(screen.getByLabelText("update.weight.label"), "");
		await user.click(screen.getByRole("button", { name: "Save" }));

		await waitFor(() => expect(onClick).toHaveBeenCalledTimes(1));
		expect(onClick.mock.calls[0][0].weight ?? null).toBeNull();
		expect(onClick.mock.calls[0][0].rating).toBe(8.1);
	});
});
