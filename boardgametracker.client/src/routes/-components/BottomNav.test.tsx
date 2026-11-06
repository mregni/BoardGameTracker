import type { AriaRole, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useBottomNavStore } from "@/hooks/useHideBottomNav";
import { act, renderWithTheme, screen, userEvent, within } from "@/test/test-utils";
import { BottomNav } from "./BottomNav";

const Icon = () => <svg />;

const route = vi.hoisted(() => ({ pathname: "/games" }));

vi.mock("@tanstack/react-router", () => ({
	Link: ({
		to,
		className,
		onClick,
		children,
	}: {
		to: string;
		className?: string;
		onClick?: () => void;
		children: ReactNode;
	}) => (
		<a
			href={to}
			className={className}
			onClick={(event) => {
				event.preventDefault();
				onClick?.();
			}}
		>
			{children}
		</a>
	),
	useRouterState: () => ({ location: { pathname: route.pathname } }),
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
			{ icon: Icon, menuLabel: "loans", path: "/loans", mobileVisible: false },
		],
	}),
}));

vi.mock("@/components/BgtMenu/BgtMenuItem", () => ({
	BgtMenuItem: ({ item, role }: { item: { path: string }; role?: AriaRole }) => (
		<a href={item.path} role={role} data-testid="more-item">
			{item.path}
		</a>
	),
}));

describe("BottomNav", () => {
	beforeEach(() => {
		useBottomNavStore.setState({ hideRequests: 0 });
		route.pathname = "/games";
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
		expect(screen.getByRole("img", { name: "Jane Doe" })).not.toHaveClass("rounded-full");
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

		const sheet = screen.getAllByTestId("more-item")[0].closest(".fixed") as HTMLElement;
		expect(sheet).toHaveClass("bottom-(--bottom-nav-h)");
	});

	it("closes the More menu when a bar entry is tapped", async () => {
		const user = userEvent.setup();
		renderWithTheme(<BottomNav />);
		await user.click(screen.getByRole("button", { name: "common:more" }));
		expect(screen.getAllByTestId("more-item")).toHaveLength(2);

		await user.click(screen.getByRole("link", { name: "games" }));

		expect(screen.queryByTestId("more-item")).not.toBeInTheDocument();
	});

	it.each([
		["/games/12", "games"],
		["/games", "games"],
	])("lights the parent entry on %s", (pathname, name) => {
		route.pathname = pathname;
		renderWithTheme(<BottomNav />);

		expect(screen.getByRole("link", { name }).querySelector(".bg-primary")).toBeInTheDocument();
	});

	it("lights More on a page below a More entry", () => {
		route.pathname = "/locations/3";
		renderWithTheme(<BottomNav />);

		expect(screen.getByRole("button", { name: "common:more" }).querySelector(".bg-primary")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "games" }).querySelector(".bg-primary")).not.toBeInTheDocument();
	});

	it("does not light an entry for a path that only starts the same", () => {
		route.pathname = "/games-archive";
		renderWithTheme(<BottomNav />);

		expect(screen.getByRole("link", { name: "games" }).querySelector(".bg-primary")).not.toBeInTheDocument();
	});

	it("opens the More menu on its first entry and walks it with the arrow keys", async () => {
		const user = userEvent.setup();
		renderWithTheme(<BottomNav />);

		await user.click(screen.getByRole("button", { name: "common:more" }));

		const [first, second] = within(screen.getByRole("menu", { name: "common:more" })).getAllByRole("menuitem");
		expect(first).toHaveFocus();

		await user.keyboard("{ArrowDown}");
		expect(second).toHaveFocus();

		await user.keyboard("{ArrowDown}");
		expect(first).toHaveFocus();

		await user.keyboard("{ArrowUp}");
		expect(second).toHaveFocus();

		await user.keyboard("{Home}");
		expect(first).toHaveFocus();

		await user.keyboard("{End}");
		expect(second).toHaveFocus();
	});

	it("closes the More menu on Escape and returns to the More button", async () => {
		const user = userEvent.setup();
		renderWithTheme(<BottomNav />);
		const more = screen.getByRole("button", { name: "common:more" });
		await user.click(more);

		await user.keyboard("{Escape}");

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
		expect(more).toHaveFocus();
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
