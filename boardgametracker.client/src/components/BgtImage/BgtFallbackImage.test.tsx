import { describe, expect, it } from "vitest";
import { fireEvent, renderWithTheme, screen } from "@/test/test-utils";
import { BgtFallbackImage } from "./BgtFallbackImage";

describe("BgtFallbackImage", () => {
	it("should render the image when provided", () => {
		renderWithTheme(<BgtFallbackImage title="Catan" image="/catan.jpg" />);
		expect(screen.getByRole("img")).toHaveAttribute("src", "/catan.jpg");
	});

	it("should render the initial when there is no image", () => {
		renderWithTheme(<BgtFallbackImage title="Catan" image={null} />);
		expect(screen.getByText("C")).toBeInTheDocument();
		expect(screen.getByRole("img", { name: "Catan" })).not.toHaveAttribute("src");
	});

	it("should fall back to the initial when the image cannot be loaded", () => {
		renderWithTheme(<BgtFallbackImage title="Catan" image="/missing.jpg" />);

		fireEvent.error(screen.getByRole("img"));

		expect(screen.getByText("C")).toBeInTheDocument();
		expect(screen.getByRole("img", { name: "Catan" })).not.toHaveAttribute("src");
	});

	it("should try again when a new image is provided after a failure", () => {
		const { rerender } = renderWithTheme(<BgtFallbackImage title="Catan" image="/one.jpg" />);
		fireEvent.error(screen.getByRole("img"));

		rerender(<BgtFallbackImage title="Catan" image="/two.jpg" />);

		expect(screen.getByRole("img")).toHaveAttribute("src", "/two.jpg");
	});

	it("should load lazily when requested", () => {
		renderWithTheme(<BgtFallbackImage title="Catan" image="/catan.jpg" lazy />);
		expect(screen.getByRole("img")).toHaveAttribute("loading", "lazy");
	});
});
