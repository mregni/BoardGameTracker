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

const auth = vi.hoisted(() => ({
	state: { user: null as unknown, isAuthenticated: false, authStatus: { authEnabled: false } },
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: unknown) => unknown) => selector(auth.state),
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
		auth.state = { user: null, isAuthenticated: false, authStatus: { authEnabled: false } };
	});

	it("shows the profile picture of the signed-in user in the more sheet", async () => {
		auth.state = {
			user: {
				id: "u1",
				username: "jane",
				displayName: "Jane Doe",
				roles: ["User"],
				profileImage: "/images/profile/jane.webp",
			},
			isAuthenticated: true,
			authStatus: { authEnabled: true },
		};
		const user = userEvent.setup();
		renderWithTheme(<BottomNav />);

		await user.click(screen.getByRole("button", { name: "common:more" }));

		expect(screen.getByRole("img", { name: "Jane Doe" })).toHaveAttribute("src", "/images/profile/jane.webp");
		expect(screen.getByRole("img", { name: "Jane Doe" })).toHaveClass("rounded-full");
	});

	it("falls back to the initial of the signed-in user without a picture", async () => {
		auth.state = {
			user: { id: "u1", username: "jane", displayName: null, roles: ["User"], profileImage: null },
			isAuthenticated: true,
			authStatus: { authEnabled: true },
		};
		const user = userEvent.setup();
		renderWithTheme(<BottomNav />);

		await user.click(screen.getByRole("button", { name: "common:more" }));

		expect(screen.queryByRole("img", { name: "jane" })).not.toBeInTheDocument();
		expect(screen.getByTitle("jane")).toHaveTextContent("j");
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
