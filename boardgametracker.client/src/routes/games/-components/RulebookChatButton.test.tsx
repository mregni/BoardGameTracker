import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { RulebookChatButton } from "./RulebookChatButton";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mockNavigate,
}));

class MockResizeObserver {
	observe() {}
	unobserve() {}
	disconnect() {}
}

describe("RulebookChatButton", () => {
	beforeEach(() => {
		mockNavigate.mockClear();
		vi.stubGlobal("ResizeObserver", MockResizeObserver);
	});

	describe("Enabled", () => {
		it("should render an enabled button", () => {
			renderWithTheme(<RulebookChatButton gameId={42} disabled={false} />);

			expect(screen.getByRole("button", { name: "ask-button" })).not.toBeDisabled();
		});

		it("should navigate to chat with the gameId when clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<RulebookChatButton gameId={42} disabled={false} />);

			await user.click(screen.getByRole("button", { name: "ask-button" }));

			expect(mockNavigate).toHaveBeenCalledWith({ to: "/chat", search: { gameId: 42 } });
		});

		it("should not render a tooltip trigger", async () => {
			const user = userEvent.setup();
			renderWithTheme(<RulebookChatButton gameId={42} disabled={false} />);

			await user.tab();

			expect(screen.queryByText("ask-button-disabled")).not.toBeInTheDocument();
		});
	});

	describe("Disabled", () => {
		it("should render a disabled button", () => {
			renderWithTheme(<RulebookChatButton gameId={42} disabled={true} />);

			expect(screen.getByRole("button", { name: "ask-button" })).toBeDisabled();
		});

		it("should show the disabled reason tooltip on keyboard focus", async () => {
			const user = userEvent.setup();
			renderWithTheme(<RulebookChatButton gameId={42} disabled={true} />);

			await user.tab();

			const tooltips = await screen.findAllByText("ask-button-disabled");
			expect(tooltips.length).toBeGreaterThan(0);
			expect(mockNavigate).not.toHaveBeenCalled();
		});
	});
});
