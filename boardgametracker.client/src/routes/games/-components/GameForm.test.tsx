import { describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { GameForm } from "./GameForm";

const router = vi.hoisted(() => ({ navigate: vi.fn(), back: vi.fn() }));

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: router.back, canGoBack: () => false } }),
	useNavigate: () => router.navigate,
}));

vi.mock("../-hooks/useGameForm", () => ({
	useGameForm: () => ({ settings: { currency: "€" }, uploadImage: vi.fn(), isLoading: false }),
}));

describe("GameForm", () => {
	it("shows the page title and a back link above the form", async () => {
		const user = userEvent.setup();
		renderWithProviders(
			<GameForm title="Edit game" buttonText="Save" backTo="/games/3" onClick={vi.fn()} disabled={false} />,
		);

		expect(screen.getByRole("heading", { name: "Edit game" })).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: /common:back/ }));

		expect(router.navigate).toHaveBeenCalledWith({ to: "/games/3", replace: true });
	});
});
