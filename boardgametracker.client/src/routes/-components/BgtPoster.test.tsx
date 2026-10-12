import { describe, expect, it } from "vitest";
import { render, screen } from "@/test/test-utils";
import { BgtPoster } from "./BgtPoster";

describe("BgtPoster", () => {
	it("shows an uploaded file name with spaces and parentheses as the image source", () => {
		render(<BgtPoster title="Mikhael" image="/images/profile/IMG 1234 (1)_ab12cd.webp" />);

		expect(screen.getByRole("img", { name: "Mikhael" })).toHaveAttribute(
			"src",
			"/images/profile/IMG 1234 (1)_ab12cd.webp",
		);
	});

	it("shows the first letter on a colour when there is no image", () => {
		render(<BgtPoster title="kathleen" image={null} />);

		expect(screen.getByRole("img", { name: "kathleen" })).not.toHaveAttribute("src");
		expect(screen.getByText("k")).toBeInTheDocument();
	});
});
