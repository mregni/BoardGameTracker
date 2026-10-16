import { describe, expect, it } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { CompareCard } from "./CompareCard";

describe("CompareCard", () => {
	it("uses a light red label on the red card so it stays readable", () => {
		renderWithTheme(<CompareCard color="red" label="Total wins" value={3} isWinner={false} />);

		const label = screen.getByText("Total wins");
		expect(label).toHaveClass("text-red-200");
		expect(label.className).not.toMatch(/text-error/);
		expect(label.closest(".border")).toHaveClass("bg-player-one/10", "border-player-one/30");
	});

	it("uses a light blue label on the blue card", () => {
		renderWithTheme(<CompareCard color="blue" label="Sessions" value={8} isWinner />);

		expect(screen.getByText("Sessions")).toHaveClass("text-blue-200");
		expect(screen.getByText("8")).toBeInTheDocument();
	});
});
