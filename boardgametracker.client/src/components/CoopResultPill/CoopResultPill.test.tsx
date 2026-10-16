import { describe, expect, it } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { CoopResultPill } from "./CoopResultPill";

describe("CoopResultPill", () => {
	it("shows a green won pill", () => {
		renderWithTheme(<CoopResultPill won />);

		expect(screen.getByText("coop.won")).toHaveClass("text-ok");
	});

	it("shows a red lost pill", () => {
		renderWithTheme(<CoopResultPill won={false} className="ml-auto" />);

		const pill = screen.getByText("coop.lost");
		expect(pill).toHaveClass("text-bad");
		expect(pill).toHaveClass("ml-auto");
	});
});
