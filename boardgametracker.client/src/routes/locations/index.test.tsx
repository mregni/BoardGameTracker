import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Location } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	locations: [] as Location[],
	failing: false,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	useNavigate: () => vi.fn(),
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/services/queries/locations", () => ({
	getLocations: () => ({
		queryKey: ["locations"],
		queryFn: () => (mocks.failing ? Promise.reject(new Error("offline")) : Promise.resolve(mocks.locations)),
	}),
}));

await import("./index");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Locations overview", () => {
	beforeEach(() => {
		mocks.locations = [
			{ id: 1, name: "Bij Kathleen", playCount: 0, lastPlayed: null },
			{ id: 2, name: "Thuis", playCount: 9, lastPlayed: "2026-10-16T20:00:00Z" },
		];
		mocks.failing = false;
	});

	it("prefetches the locations in the loader", () => {
		const queryClient = { query: vi.fn().mockResolvedValue(undefined) };

		mocks.captured.config.loader({ context: { queryClient } });

		expect(queryClient.query).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ["locations"] }));
	});

	it("shows a tile per location, most recently played first, linking to the detail page", async () => {
		renderPage();

		const links = await screen.findAllByRole("link");
		expect(links.map((x) => x.getAttribute("href"))).toEqual(["/locations/2", "/locations/1"]);
		expect(screen.getByText("tile.no-sessions")).toBeInTheDocument();
	});

	it("sorts by name when asked", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "location:sort.name" }));

		const links = screen.getAllByRole("link");
		expect(links.map((x) => x.getAttribute("href"))).toEqual(["/locations/1", "/locations/2"]);
	});

	it("opens the create dialog from the header", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "location:new.button" }));

		expect(await screen.findByRole("dialog")).toBeInTheDocument();
	});

	it("shows the empty state when there are no locations", async () => {
		mocks.locations = [];
		renderPage();

		expect(await screen.findByText("location:empty.title")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("shows an error with a retry instead of the empty state when loading fails", async () => {
		mocks.failing = true;
		renderPage();

		expect(await screen.findByText("location:error.title")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "common:retry" })).toBeInTheDocument();
		expect(screen.queryByText("location:empty.title")).not.toBeInTheDocument();
	});

	it("offers to clear the filter when nothing matches", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.type(await screen.findByRole("textbox"), "zzz");
		expect(await screen.findByText("location:filter.no-matches")).toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "location:filter.clear" }));
		expect(await screen.findAllByRole("link")).toHaveLength(2);
	});
});
