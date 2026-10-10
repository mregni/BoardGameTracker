import { describe, expect, it } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { SessionCommentCell } from "./SessionCommentCell";

describe("SessionCommentCell", () => {
	it("shows the comment with the full text as tooltip", () => {
		renderWithTheme(<SessionCommentCell comment="  Close game, decided on the last turn " />);

		const cell = screen.getByText("Close game, decided on the last turn");
		expect(cell).toHaveAttribute("title", "Close game, decided on the last turn");
	});

	it("renders nothing without a comment", () => {
		const { container } = renderWithTheme(<SessionCommentCell comment="   " />);

		expect(container.textContent).toBe("");
	});
});
