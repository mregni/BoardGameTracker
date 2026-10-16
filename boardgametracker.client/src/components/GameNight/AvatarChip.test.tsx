import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { AvatarChip, AvatarChipButton } from "./AvatarChip";

describe("AvatarChip", () => {
	it("puts a round avatar inside the pill with even padding", () => {
		const { container } = renderWithTheme(<AvatarChip title="Azul" image="/azul.png" />);

		expect(container.firstChild).toHaveClass("rounded-full", "p-1");
		expect(screen.getByRole("img", { name: "Azul" })).toHaveClass("rounded-full");
	});

	it("uses the winner tone", () => {
		const { container } = renderWithTheme(
			<AvatarChip title="Kathleen" image={null} tone="winner">
				60
			</AvatarChip>,
		);

		expect(container.firstChild).toHaveClass("border-yellow-400/50");
		expect(screen.getByText("K").parentElement).toHaveClass("rounded-full");
		expect(screen.getByText("60")).toBeInTheDocument();
	});

	it("uses a round avatar in the button variant", () => {
		renderWithTheme(<AvatarChipButton title="Jules" image="/jules.png" label="Add Jules" onClick={vi.fn()} />);

		expect(screen.getByRole("img", { name: "Jules" })).toHaveClass("rounded-full");
	});
});
