import { describe, expect, it } from "vitest";
import { render, screen } from "@/test/test-utils";
import { GameNightDateBox } from "./GameNightDateBox";

const date = new Date(2026, 9, 24, 19, 30);

describe("GameNightDateBox", () => {
	it("should render the weekday, day and month", () => {
		render(<GameNightDateBox date={date} uiLanguage="en-us" />);

		expect(screen.getByText("Sat")).toBeInTheDocument();
		expect(screen.getByText("24")).toBeInTheDocument();
		expect(screen.getByText("Oct")).toBeInTheDocument();
	});

	it("should use the strong primary fill for the highlight variant", () => {
		const { container } = render(<GameNightDateBox date={date} uiLanguage="en-us" variant="highlight" />);

		expect(container.firstChild).toHaveClass("bg-primary-strong", "text-white");
		expect(container.firstChild).not.toHaveClass("bg-primary");
	});

	it("should use a soft outline for the soft variant", () => {
		const { container } = render(<GameNightDateBox date={date.toISOString()} uiLanguage="en-us" />);

		expect(container.firstChild).toHaveClass("border-white/10", "bg-white/5");
		expect(container.firstChild).not.toHaveClass("bg-primary-strong");
	});

	it("should render a compact day for the compact variant", () => {
		const { container } = render(
			<GameNightDateBox date={date} uiLanguage="en-us" variant="compact" className="extra" />,
		);

		expect(container.firstChild).toHaveClass("text-xs", "extra");
		expect(screen.getByText("24")).toHaveClass("text-lg");
	});
});
