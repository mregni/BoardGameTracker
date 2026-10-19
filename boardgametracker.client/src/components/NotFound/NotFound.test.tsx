import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: vi.fn() } }),
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

import { NotFound } from "./NotFound";

describe("NotFound", () => {
	it("says plainly that the page does not exist and offers the main pages", () => {
		renderWithTheme(<NotFound />);

		expect(screen.getByText("subtitle")).toBeInTheDocument();
		expect(screen.queryByText("description")).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "go-back" })).toBeInTheDocument();
		expect(screen.getByText("quick-nav")).toBeInTheDocument();
		expect(screen.getByText("not-found:nav.game-nights-description")).toBeInTheDocument();
	});
});
