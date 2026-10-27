import { describe, expect, it } from "vitest";
import { render, screen } from "@/test/test-utils";
import { BgtLoadingSpinner } from "./BgtLoadingSpinner";

describe("BgtLoadingSpinner", () => {
	describe("Rendering", () => {
		it("should render the loading icon", () => {
			render(<BgtLoadingSpinner />);
			expect(screen.getByTestId("bgt-spinner")).toBeInTheDocument();
		});

		it("should show the given label instead of the default text", () => {
			render(<BgtLoadingSpinner label="Importing 5/12" />);
			expect(screen.getByText("Importing 5/12")).toBeInTheDocument();
		});
	});
});
