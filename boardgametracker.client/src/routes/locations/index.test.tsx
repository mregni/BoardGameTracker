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
	getLocations: () => ({ queryKey: ["locations"], queryFn: () => Promise.resolve(mocks.locations) }),
}));

await import("./index");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Locations overview", () => {
	beforeEach(() => {
		mocks.locations = [
			{ id: 1, name: "Bij Kathleen", playCount: 0 },
			{ id: 2, name: "Thuis", playCount: 9 },
		];
	});

	it("prefetches the locations in the loader", () => {
		const queryClient = { query: vi.fn().mockResolvedValue(undefined) };

		mocks.captured.config.loader({ context: { queryClient } });

		expect(queryClient.query).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ["locations"] }));
	});

	it("shows a tile per location, most used first, linking to the detail page", async () => {
		renderPage();

		const links = await screen.findAllByRole("link");
		expect(links.map((x) => x.getAttribute("href"))).toEqual(["/locations/2", "/locations/1"]);
		expect(screen.getByText("tile.no-sessions")).toBeInTheDocument();
		expect(screen.getByText("location:count")).toBeInTheDocument();
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
});
