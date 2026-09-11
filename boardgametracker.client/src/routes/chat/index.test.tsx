import type { FC } from "react";
import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import type { GameManual, RagAnswer } from "@/models";
import { act, renderWithProviders, screen, userEvent } from "@/test/test-utils";

interface ChatSearch {
	gameId?: number;
	manualId?: number;
}

interface CapturedRoute {
	component: FC;
	validateSearch: { parse: (input: unknown) => ChatSearch };
	beforeLoad: (args: { context: { queryClient: unknown } }) => Promise<void>;
	loader: (args: { context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as ChatSearch,
	getGamesCall: vi.fn(),
	getManualsCall: vi.fn(),
	askRagCall: vi.fn(),
	redirect: vi.fn((options: unknown) => ({ redirectTo: options })),
	captured: {} as { config: CapturedRoute },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search };
	},
	redirect: (options: unknown) => mocks.redirect(options),
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({ queryKey: ["games"], queryFn: () => mocks.getGamesCall() }),
}));

vi.mock("@/services/queries/manuals", () => ({
	getGameManuals: (gameId: number) => ({
		queryKey: ["game", gameId, "manuals"],
		queryFn: () => mocks.getManualsCall(gameId),
	}),
}));

vi.mock("@/services/ragService", () => ({
	askRagCall: (...args: unknown[]) => mocks.askRagCall(...args),
}));

vi.mock("@/components/BgtForm", () => ({
	BgtSimpleSelect: ({
		items,
		placeholder,
		value,
		onValueChange,
	}: {
		items: { value: string | number; label: string }[];
		placeholder?: string;
		value?: string | number | null;
		onValueChange?: (value: string | number) => void;
	}) => (
		<select
			aria-label={placeholder}
			value={value?.toString() ?? ""}
			onChange={(event) => {
				const item = items.find((candidate) => candidate.value.toString() === event.target.value);
				if (item) {
					onValueChange?.(item.value);
				}
			}}
		>
			<option value="" />
			{items.map((item) => (
				<option key={item.value} value={item.value.toString()}>
					{item.label}
				</option>
			))}
		</select>
	),
}));

import "./index";

const games = [
	{ id: 1, title: "Catan", image: null },
	{ id: 2, title: "Azul", image: null },
];

const buildManual = (overrides: Partial<GameManual> = {}): GameManual => ({
	id: 5,
	gameId: 1,
	title: "Rulebook",
	fileSizeBytes: 100,
	uploadDate: new Date(),
	contentType: "application/pdf",
	indexStatus: "indexed",
	indexedChunkCount: 3,
	indexError: null,
	indexedDate: null,
	...overrides,
});

const buildAnswer = (overrides: Partial<RagAnswer> = {}): RagAnswer => ({
	answer: "Collect ten points",
	hasContext: true,
	durationMs: 1234,
	citations: [],
	...overrides,
});

const renderRoute = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

const sendQuestion = async (question: string) => {
	const input = await screen.findByPlaceholderText("composer.placeholder");
	await userEvent.type(input, question);
	await userEvent.click(screen.getByRole("button", { name: "composer.send" }));
	return input;
};

beforeAll(() => {
	Element.prototype.scrollIntoView = vi.fn();
});

beforeEach(() => {
	vi.clearAllMocks();
	mocks.search = {};
	mocks.getGamesCall.mockResolvedValue(games);
	mocks.getManualsCall.mockResolvedValue([]);
});

describe("chat route config", () => {
	it("should redirect to home when rag is disabled", async () => {
		const queryClient = { ensureQueryData: vi.fn().mockResolvedValue({ ragEnabled: false }) };

		await expect(mocks.captured.config.beforeLoad({ context: { queryClient } })).rejects.toEqual({
			redirectTo: { to: "/" },
		});
	});

	it("should allow the route when rag is enabled", async () => {
		const queryClient = { ensureQueryData: vi.fn().mockResolvedValue({ ragEnabled: true }) };

		await expect(mocks.captured.config.beforeLoad({ context: { queryClient } })).resolves.toBeUndefined();
		expect(mocks.redirect).not.toHaveBeenCalled();
	});

	it("should prefetch games in the loader", () => {
		const queryClient = { prefetchQuery: vi.fn() };

		mocks.captured.config.loader({ context: { queryClient } });

		expect(queryClient.prefetchQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ["games"] }));
	});

	it("should keep valid search ids", () => {
		expect(mocks.captured.config.validateSearch.parse({ gameId: 3, manualId: 7 })).toEqual({
			gameId: 3,
			manualId: 7,
		});
	});

	it("should drop invalid search ids", () => {
		expect(mocks.captured.config.validateSearch.parse({ gameId: -1, manualId: "abc" })).toEqual({});
	});
});

describe("empty states", () => {
	it("should show the no-game state with a disabled composer when no game is selected", async () => {
		renderRoute();

		expect(await screen.findByText("empty.no-game.title")).toBeInTheDocument();
		expect(screen.getByText("empty.no-game.description")).toBeInTheDocument();
		expect(screen.getByPlaceholderText("composer.disabled-placeholder")).toBeDisabled();
		expect(screen.queryByRole("combobox", { name: "all-manuals" })).not.toBeInTheDocument();
	});

	it("should ignore a gameId that does not match a known game", async () => {
		mocks.search = { gameId: 999 };
		renderRoute();

		await screen.findByRole("option", { name: "Catan" });
		expect(screen.getByText("empty.no-game.title")).toBeInTheDocument();
		expect(mocks.getManualsCall).not.toHaveBeenCalled();
	});

	it("should show the no-manuals state when the selected game has no manuals", async () => {
		mocks.search = { gameId: 1 };
		renderRoute();

		expect(await screen.findByText("empty.no-manuals.title")).toBeInTheDocument();
		expect(screen.getByPlaceholderText("composer.disabled-placeholder")).toBeDisabled();
		expect(screen.queryByRole("combobox", { name: "all-manuals" })).not.toBeInTheDocument();
	});

	it("should show the not-indexed state when no manual is indexed", async () => {
		mocks.search = { gameId: 1 };
		mocks.getManualsCall.mockResolvedValue([buildManual({ indexStatus: "pending" })]);
		renderRoute();

		expect(await screen.findByText("empty.not-indexed.title")).toBeInTheDocument();
		expect(screen.getByPlaceholderText("composer.disabled-placeholder")).toBeDisabled();
		expect(screen.getByRole("combobox", { name: "all-manuals" })).toBeInTheDocument();
	});
});

describe("asking questions", () => {
	beforeEach(() => {
		mocks.search = { gameId: 1 };
		mocks.getManualsCall.mockResolvedValue([buildManual()]);
	});

	it("should enable the composer and show the ask hint when an indexed manual exists", async () => {
		renderRoute();

		expect(await screen.findByText("empty.ask-something")).toBeInTheDocument();
		expect(screen.getByPlaceholderText("composer.placeholder")).toBeEnabled();
		expect(screen.getByRole("button", { name: "composer.send" })).toBeDisabled();
	});

	it("should show the question and pending state, then the answer without sources", async () => {
		let resolveAsk!: (value: RagAnswer) => void;
		mocks.askRagCall.mockImplementation(
			() =>
				new Promise((resolve) => {
					resolveAsk = resolve;
				}),
		);
		renderRoute();

		const input = await sendQuestion("How do I win?");

		expect(mocks.askRagCall).toHaveBeenCalledWith(1, "How do I win?", undefined);
		expect(await screen.findByText("How do I win?")).toBeInTheDocument();
		expect(screen.getByText("thinking")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "composer.send" })).toBeDisabled();
		expect(input).toHaveValue("");

		await act(async () => {
			resolveAsk(buildAnswer());
		});

		expect(await screen.findByText("Collect ten points")).toBeInTheDocument();
		expect(screen.queryByText("thinking")).not.toBeInTheDocument();
		expect(screen.queryByText("sources")).not.toBeInTheDocument();
	});

	it("should render citations when the answer has sources", async () => {
		mocks.askRagCall.mockResolvedValue(
			buildAnswer({
				citations: [{ manualId: 5, title: "Rulebook", page: 3, snippet: "snippet", score: 0.8, imageUrl: null }],
			}),
		);
		renderRoute();

		await sendQuestion("How many players?");

		expect(await screen.findByText("Collect ten points")).toBeInTheDocument();
		expect(screen.getAllByText("sources").length).toBeGreaterThan(0);
		expect(screen.getByText("top-match")).toBeInTheDocument();
	});

	it("should show an error with retry and recover after retrying", async () => {
		mocks.askRagCall.mockRejectedValueOnce(new Error("boom")).mockResolvedValueOnce(buildAnswer());
		renderRoute();

		await sendQuestion("What now?");

		expect(await screen.findByText("error:something-went-wrong")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "retry" }));

		expect(mocks.askRagCall).toHaveBeenLastCalledWith(1, "What now?", undefined);
		expect(await screen.findByText("Collect ten points")).toBeInTheDocument();
		expect(screen.queryByText("error:something-went-wrong")).not.toBeInTheDocument();
	});

	it("should pass the selected manual to the question", async () => {
		mocks.search = { gameId: 1, manualId: 5 };
		mocks.askRagCall.mockResolvedValue(buildAnswer());
		renderRoute();

		await sendQuestion("Setup rules?");

		expect(mocks.askRagCall).toHaveBeenCalledWith(1, "Setup rules?", 5);
	});

	it("should ignore a manualId that does not match a manual of the game", async () => {
		mocks.search = { gameId: 1, manualId: 99 };
		mocks.askRagCall.mockResolvedValue(buildAnswer());
		renderRoute();

		expect(await screen.findByRole("combobox", { name: "all-manuals" })).toHaveValue("all");
		await sendQuestion("Setup rules?");

		expect(mocks.askRagCall).toHaveBeenCalledWith(1, "Setup rules?", undefined);
	});
});

describe("selectors", () => {
	it("should navigate with the gameId when a game is selected", async () => {
		renderRoute();

		const gameSelect = await screen.findByRole("combobox", { name: "select-game" });
		await screen.findByRole("option", { name: "Catan" });
		await userEvent.selectOptions(gameSelect, "1");

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/chat", search: { gameId: 1 }, replace: true });
	});

	it("should navigate with the manualId when a manual is selected", async () => {
		mocks.search = { gameId: 1 };
		mocks.getManualsCall.mockResolvedValue([buildManual()]);
		renderRoute();

		const manualSelect = await screen.findByRole("combobox", { name: "all-manuals" });
		await userEvent.selectOptions(manualSelect, "5");

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/chat", search: { gameId: 1, manualId: 5 }, replace: true });
	});

	it("should drop the manualId when all manuals is selected", async () => {
		mocks.search = { gameId: 1, manualId: 5 };
		mocks.getManualsCall.mockResolvedValue([buildManual()]);
		renderRoute();

		const manualSelect = await screen.findByRole("combobox", { name: "all-manuals" });
		await screen.findByRole("option", { name: "Rulebook" });
		await userEvent.selectOptions(manualSelect, "all");

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/chat", search: { gameId: 1 }, replace: true });
	});
});
