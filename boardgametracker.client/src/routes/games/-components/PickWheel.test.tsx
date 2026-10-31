import { describe, expect, it, vi } from "vitest";
import type { Game } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { PickWheel } from "./PickWheel";

const navigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => navigate,
}));

const games = [{ id: 4, title: "Azul", image: "/images/azul.jpg" }] as Game[];

describe("PickWheel", () => {
	it("draws each game with its cover and opens the game it lands on", async () => {
		const { container } = renderWithTheme(<PickWheel games={games} />);

		expect(container.querySelector("image")).toHaveAttribute("href", "/images/azul.jpg");

		await userEvent.click(screen.getByRole("button", { name: "pick.spin" }));
		expect(await screen.findByText("pick.result")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "pick.open" }));
		expect(navigate).toHaveBeenCalledWith({ to: "/games/$gameId", params: { gameId: 4 } });
	});
});
