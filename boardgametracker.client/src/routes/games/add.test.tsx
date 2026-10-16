import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	navigate: vi.fn(),
	settings: undefined as { bggStatus: { isConfigured: boolean } } | undefined,
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
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: mocks.settings }),
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
			"/settings?tab=bgg",
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
});
