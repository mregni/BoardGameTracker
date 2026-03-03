import { describe, expect, it } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { BgtMenuLogo } from "./BgtMenuLogo";

describe("BgtMenuLogo", () => {
	describe("Rendering", () => {
		it("should render logo text", () => {
			renderWithTheme(<BgtMenuLogo />);
			expect(screen.getByText("Board games")).toBeInTheDocument();
		});
	});
});
