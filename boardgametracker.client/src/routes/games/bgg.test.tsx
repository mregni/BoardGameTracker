import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	navigate: vi.fn(),
	redirectedTo: null as string | null,
	settings: undefined as { currency: string; bggStatus: { isConfigured: boolean } } | undefined,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return config;
	},
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn(), canGoBack: () => false } }),
	Navigate: ({ to }: { to: string }) => {
		mocks.redirectedTo = to;
		return null;
	},
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("./-hooks/useBggGameModal", () => ({
	useBggGameModal: () => ({ save: vi.fn(), isPending: false, settings: mocks.settings }),
}));

await import("./bgg");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);
};

describe("Add game from BGG page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.redirectedTo = null;
	});

	it("sends the user back to the add page when no BGG key is configured", () => {
		mocks.settings = { currency: "€", bggStatus: { isConfigured: false } };
		renderPage();

		expect(mocks.redirectedTo).toBe("/games/add");
		expect(screen.queryByLabelText("bgg.label")).not.toBeInTheDocument();
	});

	it("shows the form with a page title and back link when the key is configured", async () => {
		const user = userEvent.setup();
		mocks.settings = { currency: "€", bggStatus: { isConfigured: true } };
		renderPage();

		expect(mocks.redirectedTo).toBeNull();
		expect(screen.getByRole("heading", { name: "new.title" })).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: /common:back/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/add", replace: true });
	});

	it("waits for the settings before deciding", () => {
		mocks.settings = undefined;
		renderPage();

		expect(mocks.redirectedTo).toBeNull();
	});
});
