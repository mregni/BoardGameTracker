import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	navigate: vi.fn(),
	collection: vi.fn(),
}));

const collection = [
	{ bggId: 7, title: "Dwar7s Winter", state: "owned", imageUrl: "", lastModified: "2022-08-30T00:00:00Z" },
	{ bggId: 8, title: "Earth", state: "owned", imageUrl: "", lastModified: "2026-05-20T00:00:00Z" },
];

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ username: "tester" }) };
	},
	redirect: vi.fn(),
	useNavigate: () => mocks.navigate,
}));

vi.mock("react-i18next", () => {
	const t = (key: string) => key;
	return { useTranslation: () => ({ t }) };
});

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({
		invalidateGames: vi.fn(),
		invalidateCounts: vi.fn(),
		invalidateDashboard: vi.fn(),
	}),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/services/gameService", () => ({
	importGamesCall: vi.fn(),
}));

vi.mock("@/services/queries/games", () => ({
	getBggCollection: (username: string) => ({
		queryKey: ["bgg-collection", username],
		queryFn: mocks.collection,
	}),
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({ currency: "€" }) }),
}));

await import("./list_.$username");

describe("BGG import list", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.collection.mockResolvedValue(collection);
	});

	it.each([401, 503])(
		"offers to open the integration settings next to try again when BGG rejects the request (%i)",
		async (status) => {
			mocks.collection.mockRejectedValue({ kind: "client", status, message: "x", url: "game/bgg/collection" });
			const Component = mocks.captured.config.component;
			const user = userEvent.setup();
			renderWithProviders(<Component />);

			expect(await screen.findByText("games:import.error-unauthorized")).toBeInTheDocument();
			expect(screen.getByRole("button", { name: "common:retry" })).toBeInTheDocument();
			await user.click(screen.getByRole("button", { name: "common:open-integration-settings" }));

			expect(mocks.navigate).toHaveBeenCalledWith({ to: "/settings", search: { tab: "bgg" } });
		},
	);

	it("only offers try again for a generic network error", async () => {
		mocks.collection.mockRejectedValue(new Error("Network Error"));
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		expect(await screen.findByText("games:import.error-unknown")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "common:retry" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "common:open-integration-settings" })).not.toBeInTheDocument();
	});

	it("keeps the price field focused while typing a multi-digit price", async () => {
		const Component = mocks.captured.config.component;
		const user = userEvent.setup();
		renderWithProviders(<Component />);

		const [price] = await screen.findAllByPlaceholderText("game:price.placeholder");
		expect(price).toHaveDisplayValue("");
		await user.type(price, "100");

		expect(price).toHaveFocus();
		expect(price).toHaveDisplayValue("100");
		expect(price).toHaveValue("100");
	});

	it("selects every importable game from the header checkbox", async () => {
		const Component = mocks.captured.config.component;
		const user = userEvent.setup();
		renderWithProviders(<Component />);

		await screen.findByText("Dwar7s Winter");
		await user.click(screen.getAllByRole("checkbox")[0]);

		const [selectAll, first, second] = screen.getAllByRole("checkbox");
		expect(selectAll).toBeChecked();
		expect(first).toBeChecked();
		expect(second).toBeChecked();
	});
});
