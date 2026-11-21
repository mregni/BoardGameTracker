import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	beforeLoad: (args: { context: { queryClient: { getQueryData: (key: unknown) => unknown } } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	redirect: vi.fn((options: unknown) => options),
	rejected: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	redirect: mocks.redirect,
	useNavigate: () => vi.fn(),
}));

vi.mock("react-i18next", () => {
	const t = (key: string, options?: Record<string, unknown>) => (options ? `${key} ${JSON.stringify(options)}` : key);
	return { useTranslation: () => ({ t }) };
});

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateGameCreated: vi.fn() }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/services/gameService", () => ({
	importGamesCall: vi.fn(),
}));

vi.mock("@/services/queries/games", () => ({
	csvImportGamesKey: ["csv-games"],
	getCsvImportGames: () => ({
		queryKey: ["csv-games"],
		queryFn: () =>
			Promise.resolve([
				{ bggId: 13, title: "Catan", state: "owned", imageUrl: "", price: 45.5, lastModified: "2020-03-14T00:00:00Z" },
			]),
	}),
	getCsvImportRejected: () => ({ queryKey: ["csv-rejected"], queryFn: mocks.rejected }),
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({ currency: "€" }) }),
}));

await import("./csv");

describe("CSV import list", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.rejected.mockResolvedValue([]);
	});

	it("sends the user back to the start page when no CSV file was uploaded", () => {
		const queryClient = { getQueryData: () => undefined };

		expect(() => mocks.captured.config.beforeLoad({ context: { queryClient } })).toThrow();
		expect(mocks.redirect).toHaveBeenCalledWith({ to: "/games/import/start" });
	});

	it("opens the review screen when a CSV file was uploaded", () => {
		const queryClient = { getQueryData: () => [] };

		expect(() => mocks.captured.config.beforeLoad({ context: { queryClient } })).not.toThrow();
		expect(mocks.redirect).not.toHaveBeenCalled();
	});

	it("names a rejected row by its BGG id, or a dash, when it has no title", async () => {
		mocks.rejected.mockResolvedValue([
			{ line: 2, title: "", bggId: 404, reason: "not-found" },
			{ line: 3, title: "", bggId: null, reason: "invalid-id" },
		]);
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		expect(await screen.findByText(/"line":2,"title":404,/)).toBeInTheDocument();
		expect(screen.getByText(/"line":3,"title":"-",/)).toBeInTheDocument();
	});

	it("lists the games from the file with the price from the file", async () => {
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		expect(await screen.findByText("Catan")).toBeInTheDocument();
		expect(screen.getByRole("textbox", { name: /Catan/ })).toHaveDisplayValue(/^45[.,]5$/);
		expect(screen.queryByText(/import\.csv\.rejected-title/)).not.toBeInTheDocument();
	});

	it("shows the rows that were not imported with their line and reason", async () => {
		mocks.rejected.mockResolvedValue([{ line: 5, title: "Lost Promo", bggId: 999, reason: "no-base-game" }]);
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		expect(await screen.findByText(/import\.csv\.rejected-title/)).toBeInTheDocument();
		expect(
			screen.getByText(/"line":5,"title":"Lost Promo","reason":"import\.csv\.reason\.no-base-game"/),
		).toBeInTheDocument();
	});
});
