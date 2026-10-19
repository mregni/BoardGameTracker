import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	forgotPasswordCall: vi.fn(),
	emailEnabled: true,
	captured: {} as { Page: FC },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return config;
	},
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/authService", () => ({
	forgotPasswordCall: mocks.forgotPasswordCall,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ emailEnabled: mocks.emailEnabled }),
	}),
}));

import "./forgot-password";

describe("ForgotPasswordPage", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.emailEnabled = true;
	});

	it("sends a reset link for the username and confirms without saying whether it exists", async () => {
		const user = userEvent.setup();
		mocks.forgotPasswordCall.mockResolvedValue(undefined);
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByText("forgot-subtitle")).toBeInTheDocument();
		await user.type(screen.getByLabelText("username"), "lotte");
		await user.click(screen.getByRole("button", { name: "forgot-submit" }));

		await waitFor(() => expect(mocks.forgotPasswordCall).toHaveBeenCalledWith({ username: "lotte" }));
		expect(await screen.findByText("forgot-confirmation")).toBeInTheDocument();
	});

	it("asks to contact an admin and offers no form when email is off", async () => {
		const user = userEvent.setup();
		mocks.emailEnabled = false;
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByText("ask-admin-reset")).toBeInTheDocument();
		expect(screen.queryByText("forgot-subtitle")).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "forgot-submit" })).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "back-to-login" }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login" });
	});
});
