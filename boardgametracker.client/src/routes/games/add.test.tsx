import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	navigate: vi.fn(),
	settings: undefined as { bggStatus: { isConfigured: boolean } } | undefined,
	canManageSettings: true,
	getStatuses: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return config;
	},
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn(), canGoBack: () => false } }),
	Link: ({ children, to, search }: { children: React.ReactNode; to: string; search: { tab: string } }) => (
		<a href={`${to}?tab=${search.tab}`}>{children}</a>
	),
	Navigate: () => null,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true, canManageSettings: mocks.canManageSettings }),
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: mocks.settings }),
}));

vi.mock("@/services/integrationService", () => ({
	getIntegrationStatusesCall: mocks.getStatuses,
	testIntegrationCall: vi.fn(),
}));

await import("./add");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);
};

describe("Add game page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.settings = { bggStatus: { isConfigured: false } };
		mocks.canManageSettings = true;
		mocks.getStatuses.mockResolvedValue([]);
	});

	it("shows a page title and a back link to the games", async () => {
		const user = userEvent.setup();
		renderPage();

		expect(screen.getByRole("heading", { name: "game:new.title" })).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: /common:back/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games", replace: true });
	});

	it("points to Settings → Integrations when the BGG key is missing", () => {
		renderPage();

		expect(screen.getByText("settings:bgg.status.not-configured")).toBeInTheDocument();
		expect(screen.getByText(/game:new.bgg-missing.description/)).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "game:new.bgg-missing.link" })).toHaveAttribute(
			"href",
			"/settings?tab=integrations",
		);
		expect(screen.queryByText("settings:bgg.status.not-configured-description")).not.toBeInTheDocument();
	});

	it("hides the banner once the BGG key is configured", () => {
		mocks.settings = { bggStatus: { isConfigured: true } };
		renderPage();

		expect(screen.queryByText("settings:bgg.status.not-configured")).not.toBeInTheDocument();
	});

	it("hides the banner while the settings load", () => {
		mocks.settings = undefined;
		renderPage();

		expect(screen.queryByText("settings:bgg.status.not-configured")).not.toBeInTheDocument();
	});

	it("warns admins before a sync when BGG rejects the saved key", async () => {
		mocks.settings = { bggStatus: { isConfigured: true } };
		mocks.getStatuses.mockResolvedValue([
			{ integration: "bgg", status: "unauthorized", checkedAt: "2026-10-16T09:30:00Z", messageKey: "unauthorized" },
		]);
		renderPage();

		expect(await screen.findByText("settings:integrations.status.unauthorized")).toBeInTheDocument();
		expect(screen.getByText(/settings:integrations.reason.unauthorized/)).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "game:new.bgg-missing.link" })).toHaveAttribute(
			"href",
			"/settings?tab=integrations",
		);
	});

	it("stays quiet when the BGG connection works", async () => {
		mocks.settings = { bggStatus: { isConfigured: true } };
		mocks.getStatuses.mockResolvedValue([
			{ integration: "bgg", status: "ok", checkedAt: "2026-10-16T09:30:00Z", messageKey: "ok" },
		]);
		renderPage();

		await vi.waitFor(() => expect(mocks.getStatuses).toHaveBeenCalledTimes(1));
		expect(screen.queryByText(/settings:integrations.status/)).not.toBeInTheDocument();
	});

	it("does not test the connection for people who cannot manage settings", () => {
		mocks.settings = { bggStatus: { isConfigured: true } };
		mocks.canManageSettings = false;
		renderPage();

		expect(mocks.getStatuses).not.toHaveBeenCalled();
	});
});
