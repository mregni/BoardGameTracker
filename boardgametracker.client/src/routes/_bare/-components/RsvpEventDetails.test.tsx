import { describe, expect, it } from "vitest";
import type { GameNight } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { RsvpEventDetails } from "./RsvpEventDetails";

const gameNight = {
	id: 1,
	title: "Strategy Night",
	startDate: new Date(2026, 9, 11, 19, 30),
	location: { id: 2, name: "Thuis" },
	locationId: 2,
	host: { id: 3, name: "reggi", image: null },
	hostId: 3,
	linkId: "abc",
	invitedPlayers: [],
	suggestedGames: [],
	notes: "",
} as unknown as GameNight;

describe("RsvpEventDetails", () => {
	it("shows the long date in the app language and the time in the configured format", () => {
		renderWithTheme(<RsvpEventDetails gameNight={gameNight} timeFormat="HH:mm" />);

		expect(screen.getByText("Sunday, October 11th, 2026")).toBeInTheDocument();
		expect(screen.getByText("19:30")).toBeInTheDocument();
		expect(screen.getByText("Thuis")).toBeInTheDocument();
	});
});
