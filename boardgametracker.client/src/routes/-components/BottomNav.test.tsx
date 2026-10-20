import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useBottomNavStore } from "@/hooks/useHideBottomNav";
import { act, renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BottomNav } from "./BottomNav";

const Icon = () => <svg />;

vi.mock("@tanstack/react-router", () => ({
	Link: ({ to, className, children }: { to: string; className?: string; children: ReactNode }) => (
		<a href={to} className={className}>
			{children}
		</a>
	),
	useRouterState: () => ({ location: { pathname: "/games" } }),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: unknown) => unknown) =>
		selector({ user: null, isAuthenticated: false, authStatus: { authEnabled: false } }),
}));

vi.mock("@/hooks/useLogout", () => ({
	useLogout: () => vi.fn(),
}));

vi.mock("../-hooks/useMenuInfo", () => ({
	useMenuInfo: () => ({
		versionInfo: { currentVersion: "1.0.0" },
		counts: [],
		menuItems: [
			{ icon: Icon, menuLabel: "games", path: "/games", mobileVisible: true },
			{ icon: Icon, menuLabel: "locations", path: "/locations", mobileVisible: false },
		],
	}),
}));

vi.mock("@/components/BgtMenu/BgtMenuItem", () => ({
	BgtMenuItem: ({ item }: { item: { path: string } }) => <div data-testid="more-item">{item.path}</div>,
}));

describe("BottomNav", () => {
	beforeEach(() => {
		useBottomNavStore.setState({ hideRequests: 0 });
	});

	it("sizes the nav from the shared height variable including the safe area", () => {
		renderWithTheme(<BottomNav />);

		const nav = screen.getByRole("navigation");
		expect(nav).toHaveClass("fixed", "bottom-0", "h-(--bottom-nav-h)", "pb-[env(safe-area-inset-bottom)]", "md:hidden");
	});

	it("opens the more sheet right above the nav", async () => {
		const user = userEvent.setup();
		renderWithTheme(<BottomNav />);

		await user.click(screen.getByRole("button", { name: "common:more" }));

		const sheet = screen.getByTestId("more-item").closest(".fixed") as HTMLElement;
		expect(sheet).toHaveClass("bottom-(--bottom-nav-h)");
	});

	it("disappears while a form asks to hide it and comes back afterwards", () => {
		renderWithTheme(<BottomNav />);
		expect(screen.getByRole("navigation")).toBeInTheDocument();

		act(() => useBottomNavStore.getState().requestHide());
		expect(screen.queryByRole("navigation")).not.toBeInTheDocument();

		act(() => useBottomNavStore.getState().releaseHide());
		expect(screen.getByRole("navigation")).toBeInTheDocument();
	});
});
