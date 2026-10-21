import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { Sidebar } from "./Sidebar";

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
	useMenuInfo: () => ({ versionInfo: undefined, counts: [], menuItems: [] }),
}));

vi.mock("@/components/BgtMenu/BgtMenuLogo", () => ({
	BgtMenuLogo: () => <div />,
}));

vi.mock("./VersionCard", () => ({
	VersionCard: () => <div />,
}));

describe("Sidebar", () => {
	beforeEach(() => {
		auth.state = { user: null, isAuthenticated: false, authStatus: { authEnabled: false } };
	});

	it("shows the profile picture of the signed-in user with the square avatar shape", () => {
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

		renderWithTheme(<Sidebar />);

		const avatar = screen.getByRole("img", { name: "Jane Doe" });
		expect(avatar).toHaveAttribute("src", "/images/profile/jane.webp");
		expect(avatar).not.toHaveClass("rounded-full");
		expect(screen.getByText("Jane Doe")).toBeInTheDocument();
	});

	it("falls back to the initial when the user has no picture", () => {
		auth.state = {
			user: { id: "u1", username: "jane", displayName: null, roles: ["User"], profileImage: null },
			isAuthenticated: true,
			authStatus: { authEnabled: true },
		};

		renderWithTheme(<Sidebar />);

		expect(screen.queryByRole("img")).not.toBeInTheDocument();
		expect(screen.getByTitle("jane")).toHaveTextContent("j");
	});

	it("shows no user block while authentication is off", () => {
		renderWithTheme(<Sidebar />);

		expect(screen.queryByRole("button", { name: "common:logout" })).not.toBeInTheDocument();
	});
});
