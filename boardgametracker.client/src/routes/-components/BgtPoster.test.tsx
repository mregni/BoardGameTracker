import { describe, expect, it } from "vitest";
import { render } from "@/test/test-utils";
import { BgtPoster } from "./BgtPoster";

describe("BgtPoster", () => {
	it("passes an uploaded file name with spaces and parentheses as a quoted css url", () => {
		const { container } = render(<BgtPoster title="Mikhael" image="/images/profile/IMG 1234 (1)_ab12cd.webp" />);

		const poster = container.firstElementChild as HTMLElement;
		expect(poster.style.getPropertyValue("--image-url")).toBe('url("/images/profile/IMG 1234 (1)_ab12cd.webp")');
		expect(poster).toHaveClass("bg-(image:--image-url)");
	});

	it("shows the first letter on a colour when there is no image", () => {
		const { container, getByText } = render(<BgtPoster title="kathleen" image={null} />);

		const poster = container.firstElementChild as HTMLElement;
		expect(poster.style.getPropertyValue("--image-url")).toBe("");
		expect(getByText("k")).toBeInTheDocument();
	});
});
