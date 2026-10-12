import { describe, expect, it } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { WelcomeCard } from "./WelcomeCard";

describe("WelcomeCard", () => {
	it("names the linked player whose sessions are counted", () => {
		renderWithTheme(<WelcomeCard name="Mikhaël" playerName="reggi" playCount={10} totalSessions={12} />);

		expect(screen.getByText("welcome-back-title")).toBeInTheDocument();
		expect(screen.getByText("welcome-back-linked")).toBeInTheDocument();
	});

	it("falls back to the plain count without a player name", () => {
		renderWithTheme(<WelcomeCard name="Mikhaël" playerName={null} playCount={10} totalSessions={12} />);

		expect(screen.getByText("welcome-back")).toBeInTheDocument();
	});
});
