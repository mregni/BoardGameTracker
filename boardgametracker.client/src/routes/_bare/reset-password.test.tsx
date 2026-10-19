import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as { userId?: string; token?: string },
	captured: {} as { Page: FC; validateSearch: { parse: (value: unknown) => unknown } },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC; validateSearch: { parse: (value: unknown) => unknown } }) => {
		mocks.captured.Page = config.component;
		mocks.captured.validateSearch = config.validateSearch;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/authService", () => ({
	confirmResetPasswordCall: vi.fn(),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

import "./reset-password";

const renderPage = () => renderWithProviders(<mocks.captured.Page />);

describe("ResetPasswordPage", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("explains an incomplete link and points to Forgot password instead of crashing", async () => {
		const user = userEvent.setup();
		mocks.search = {};
		renderPage();

		expect(screen.getByTestId("reset-link-incomplete")).toBeInTheDocument();
		expect(screen.getByText("reset-incomplete")).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "forgot-link" }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/forgot-password" });
		await user.click(screen.getByRole("button", { name: "back-to-login" }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login" });
	});

	it("treats a link with only a user as incomplete", () => {
		mocks.search = { userId: "user-1" };
		renderPage();

		expect(screen.getByTestId("reset-link-incomplete")).toBeInTheDocument();
	});

	it("shows the new password form for a complete link", () => {
		mocks.search = { userId: "user-1", token: "abc" };
		renderPage();

		expect(screen.queryByTestId("reset-link-incomplete")).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "reset-submit" })).toBeInTheDocument();
	});

	it("accepts the address without a token", () => {
		expect(mocks.captured.validateSearch.parse({})).toEqual({});
		expect(mocks.captured.validateSearch.parse({ userId: "u", token: "t" })).toEqual({ userId: "u", token: "t" });
	});
});
