import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	resetDatabase: vi.fn(() => Promise.resolve()),
	factoryReset: vi.fn(() => Promise.resolve()),
}));

vi.mock("../-hooks/useDangerZone", () => ({
	useDangerZone: () => ({
		resetDatabase: mocks.resetDatabase,
		isResetting: false,
		factoryReset: mocks.factoryReset,
		isFactoryResetting: false,
	}),
}));

import { DangerZoneSection } from "./DangerZoneSection";

describe("DangerZoneSection", () => {
	beforeEach(() => {
		useAuth.setState({
			authStatus: { authEnabled: true },
			user: { id: "1", username: "admin", displayName: null, roles: ["Admin"] },
		});
	});

	afterEach(() => {
		useAuth.setState({ authStatus: null, user: null });
	});

	it("has a title but no cautionary caption", () => {
		renderWithProviders(<DangerZoneSection />);

		expect(screen.getByText("advanced.danger.title")).toBeInTheDocument();
		expect(screen.queryByText("advanced.danger.description")).not.toBeInTheDocument();
	});

	it("lists what a data reset removes", async () => {
		const user = userEvent.setup();
		renderWithProviders(<DangerZoneSection />);

		await user.click(screen.getByRole("button", { name: "advanced.danger.reset.button" }));

		expect(screen.getByText("advanced.danger.reset.confirm.description")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("explains the admin password of a factory reset in one line with a docs link", async () => {
		const user = userEvent.setup();
		renderWithProviders(<DangerZoneSection />);

		await user.click(screen.getByRole("button", { name: "advanced.danger.factory-reset.button" }));

		expect(screen.getByText(/confirm\.admin-account\s*$/)).toBeInTheDocument();
		expect(
			screen.getByRole("link", { name: "advanced.danger.factory-reset.confirm.admin-account-link" }),
		).toHaveAttribute("href", expect.stringContaining("/getting-started/authentication/"));
	});

	it("is hidden from non-admins", () => {
		useAuth.setState({ user: { id: "2", username: "jane", displayName: null, roles: ["User"] } });
		const { container } = renderWithProviders(<DangerZoneSection />);

		expect(container).toBeEmptyDOMElement();
	});
});
