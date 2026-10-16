import type { FC, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Shame, ShameStatistics } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	beforeLoad: (args: { context: { queryClient: { query: (options: unknown) => Promise<unknown> } } }) => Promise<void>;
	loader: (args: { context: { queryClient: { query: (options: unknown) => Promise<unknown> } } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	canWrite: true,
	canManageSettings: true,
	shames: [] as Shame[],
	statistics: undefined as ShameStatistics | undefined,
	settings: {
		currency: "€",
		dateFormat: "dd-MM-yyyy",
		shelfOfShameMonthsLimit: 6,
		gameNightsEnabled: true,
		shelfOfShameEnabled: true,
	} as Record<string, unknown>,
	plan: vi.fn(),
	useShameData: vi.fn(),
	redirect: vi.fn((options: unknown) => options),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	redirect: mocks.redirect,
	Link: ({
		to,
		params,
		search,
		children,
		className,
		"aria-label": ariaLabel,
	}: {
		to: string;
		params?: Record<string, string | number>;
		search?: Record<string, string>;
		children: ReactNode;
		className?: string;
		"aria-label"?: string;
	}) => {
		const path = Object.entries(params ?? {}).reduce(
			(current, [name, value]) => current.replace(`$${name}`, String(value)),
			to,
		);
		const query = search ? `?${new URLSearchParams(search).toString()}` : "";
		return (
			<a aria-label={ariaLabel} className={className} href={`${path}${query}`}>
				{children}
			</a>
		);
	},
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: mocks.canWrite, canManageSettings: mocks.canManageSettings }),
}));

vi.mock("@/components/BgtForm/BgtSelect", () => ({
	BgtSelect: ({
		label,
		value,
		items,
		onValueChange,
	}: {
		label: string;
		value: string | number;
		items: { value: string | number; label: string }[];
		onValueChange: (value: string | number) => void;
	}) => (
		<label>
			{label}
			<select
				value={String(value)}
				onChange={(event) => {
					const item = items.find((option) => String(option.value) === event.target.value);
					if (item) onValueChange(item.value);
				}}
			>
				{items.map((item) => (
					<option key={item.value} value={String(item.value)}>
						{item.label}
					</option>
				))}
			</select>
		</label>
	),
}));

vi.mock("./-hooks/useShameData", () => ({
	useShameData: (months?: number) => {
		mocks.useShameData(months);
		return {
			shames: mocks.shames,
			statistics: mocks.statistics,
			settings: mocks.settings,
			isLoading: false,
			plan: mocks.plan,
			planningId: undefined,
		};
	},
}));

const night = { id: 4, title: "Friday", startDate: new Date(2026, 9, 23, 19) };

const shame = (overrides: Partial<Shame>): Shame => ({
	id: 1,
	title: "Game",
	image: null,
	additionDate: new Date(2023, 0, 1),
	price: null,
	lastSessionDate: null,
	playCount: 0,
	minPlayers: null,
	maxPlayers: null,
	minPlayTime: null,
	maxPlayTime: null,
	plannedGameNightId: null,
	plannedFor: null,
	...overrides,
});

const root = shame({ id: 1, title: "Root", additionDate: new Date(2022, 0, 1) });
const everdell = shame({ id: 2, title: "Everdell", price: 52, plannedGameNightId: 4, plannedFor: night.startDate });
const wingspan = shame({ id: 3, title: "Wingspan", price: 55, playCount: 6, lastSessionDate: new Date(2025, 10, 1) });

const baseStatistics: ShameStatistics = {
	count: 3,
	totalValue: 107,
	averageValue: 53.5,
	pricedGameCount: 2,
	months: 6,
	ownedCount: 41,
	collectionValue: 1240,
	neverPlayedCount: 2,
	neverPlayedValue: 52,
	plannedCount: 1,
	nextGameNight: night,
};

await import("./index");

const Page = () => {
	const Component = mocks.captured.config.component;
	return <Component />;
};

const rowTitles = () =>
	within(screen.getByRole("list", { name: "list.label" }))
		.getAllByRole("listitem")
		.map((item) => item.querySelector("a")?.textContent);

describe("Shelf of shame page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.canWrite = true;
		mocks.canManageSettings = true;
		mocks.shames = [root, everdell, wingspan];
		mocks.statistics = baseStatistics;
		mocks.settings = {
			currency: "€",
			dateFormat: "dd-MM-yyyy",
			shelfOfShameMonthsLimit: 6,
			gameNightsEnabled: true,
			shelfOfShameEnabled: true,
		};
	});

	it("shows the tiles, the rows and the money bar", () => {
		renderWithProviders(<Page />);

		expect(screen.getByText("tiles.on-shelf")).toBeInTheDocument();
		expect(rowTitles()).toEqual(["Root", "Everdell", "Wingspan"]);
		expect(screen.getByRole("heading", { name: "money.title" })).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "change-in-settings" })).toHaveAttribute(
			"href",
			"/settings?tab=shelf-of-shame",
		);
	});

	it("hides the settings link from users who cannot change settings", () => {
		mocks.canManageSettings = false;
		renderWithProviders(<Page />);

		expect(screen.queryByRole("link", { name: "change-in-settings" })).not.toBeInTheDocument();
	});

	it("filters on never played and played before", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Page />);

		await user.click(screen.getByRole("button", { name: "filters.played 1" }));
		expect(rowTitles()).toEqual(["Wingspan"]);

		await user.click(screen.getByRole("button", { name: "filters.never 2" }));
		expect(rowTitles()).toEqual(["Root", "Everdell"]);
	});

	it("sorts by title and by price paid, also from the money card", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Page />);

		await user.selectOptions(screen.getByRole("combobox", { name: "filters.sort" }), "title");
		expect(rowTitles()).toEqual(["Everdell", "Root", "Wingspan"]);

		await user.click(screen.getByRole("button", { name: "money.sort-by-price" }));
		expect(rowTitles()).toEqual(["Wingspan", "Everdell", "Root"]);
	});

	it("asks the shelf for a longer period when Not played in changes", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Page />);

		expect(mocks.useShameData).toHaveBeenLastCalledWith(undefined);
		await user.selectOptions(screen.getByRole("combobox", { name: "filters.not-played-in" }), "12");

		expect(mocks.useShameData).toHaveBeenLastCalledWith(12);
	});

	it("never offers a period shorter than the setting", () => {
		mocks.settings = { ...mocks.settings, shelfOfShameMonthsLimit: 12 };
		mocks.statistics = { ...baseStatistics, months: 12 };
		renderWithProviders(<Page />);

		const select = screen.getByRole("combobox", { name: "filters.not-played-in" }) as HTMLSelectElement;
		expect(Array.from(select.options).map((option) => option.value)).toEqual(["12", "24"]);
	});

	it("plans a game for the next night", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Page />);

		const rows = within(screen.getByRole("list", { name: "list.label" })).getAllByRole("listitem");
		await user.click(within(rows[0]).getByRole("button", { name: "row.plan-label" }));

		expect(mocks.plan).toHaveBeenCalledWith(root, night);
		expect(within(rows[1]).getByText("row.planned")).toBeInTheDocument();
	});

	it("opens Log session for the game", () => {
		renderWithProviders(<Page />);

		const rows = within(screen.getByRole("list", { name: "list.label" })).getAllByRole("listitem");
		expect(within(rows[0]).getByRole("link", { name: "row.log-play-label" })).toHaveAttribute(
			"href",
			"/sessions/new/1",
		);
	});

	it("links to the game nights instead of offering Plan when no night is coming up", () => {
		mocks.statistics = { ...baseStatistics, nextGameNight: null };
		renderWithProviders(<Page />);

		expect(screen.queryByRole("button", { name: "row.plan-label" })).not.toBeInTheDocument();
		expect(screen.getByRole("link", { name: "plan.plan-night" })).toHaveAttribute("href", "/game-nights");
	});

	it("does not point to game nights when they are turned off or the user cannot plan", () => {
		mocks.statistics = { ...baseStatistics, nextGameNight: null };
		mocks.settings = { ...mocks.settings, gameNightsEnabled: false };
		const { unmount } = renderWithProviders(<Page />);
		expect(screen.queryByRole("link", { name: "plan.plan-night" })).not.toBeInTheDocument();
		unmount();

		mocks.settings = { ...mocks.settings, gameNightsEnabled: true };
		mocks.canWrite = false;
		renderWithProviders(<Page />);
		expect(screen.queryByRole("link", { name: "plan.plan-night" })).not.toBeInTheDocument();
		expect(screen.queryByRole("link", { name: "row.log-play-label" })).not.toBeInTheDocument();
	});

	it("shows the empty state when the shelf is empty", () => {
		mocks.shames = [];
		mocks.statistics = { ...baseStatistics, count: 0, neverPlayedCount: 0, plannedCount: 0, totalValue: null };
		renderWithProviders(<Page />);

		expect(screen.getByText("none.title")).toBeInTheDocument();
		expect(screen.queryByRole("list", { name: "list.label" })).not.toBeInTheDocument();
		expect(screen.queryByRole("link", { name: "plan.plan-night" })).not.toBeInTheDocument();
	});

	it("says so when the filters leave nothing", async () => {
		const user = userEvent.setup();
		mocks.shames = [root];
		renderWithProviders(<Page />);

		await user.click(screen.getByRole("button", { name: "filters.played 0" }));

		expect(screen.getByText("no-matches")).toBeInTheDocument();
		expect(screen.queryByRole("list", { name: "list.label" })).not.toBeInTheDocument();
	});

	it("redirects home when the shelf of shame is turned off and preloads the shelf otherwise", async () => {
		const query = vi.fn((options: unknown) => Promise.resolve({ shelfOfShameEnabled: false, options }));
		await expect(mocks.captured.config.beforeLoad({ context: { queryClient: { query } } })).rejects.toEqual({
			to: "/",
		});

		const enabled = vi.fn(() => Promise.resolve({ shelfOfShameEnabled: true }));
		await mocks.captured.config.beforeLoad({ context: { queryClient: { query: enabled } } });

		const loaderQuery = vi.fn(() => Promise.resolve());
		mocks.captured.config.loader({ context: { queryClient: { query: loaderQuery } } });
		expect(loaderQuery).toHaveBeenCalledTimes(2);
	});
});
