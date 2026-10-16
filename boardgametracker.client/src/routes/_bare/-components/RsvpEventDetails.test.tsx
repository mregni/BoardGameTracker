import { describe, expect, it } from "vitest";
import type { GameNight, Settings } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { RsvpEventDetails } from "./RsvpEventDetails";

const gameNight = (notes: string) =>
	({
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
		notes,
	}) as unknown as GameNight;

const settings = { timeFormat: "HH:mm", uiLanguage: "en-US" } as Settings;

describe("RsvpEventDetails", () => {
	it("shows the date box, title, long date, location and host", () => {
		renderWithTheme(<RsvpEventDetails gameNight={gameNight("")} settings={settings} />);

		expect(screen.getByText("Sun")).toBeInTheDocument();
		expect(screen.getByText("11")).toBeInTheDocument();
		expect(screen.getByRole("heading", { name: "Strategy Night" })).toBeInTheDocument();
		expect(screen.getByText("Sunday, October 11th, 2026")).toBeInTheDocument();
		expect(screen.getByText("Thuis")).toBeInTheDocument();
		expect(screen.getByText("hosted-by")).toBeInTheDocument();
		expect(screen.getByText("reggi")).toBeInTheDocument();
		expect(screen.getByText("game-nights:card.when")).toBeInTheDocument();
		expect(screen.getByText("invited")).toBeInTheDocument();
	});

	it("shows the host notes only when there are any", () => {
		const { rerender } = renderWithTheme(<RsvpEventDetails gameNight={gameNight("")} settings={settings} />);
		expect(screen.queryByText("Bring snacks")).not.toBeInTheDocument();

		rerender(<RsvpEventDetails gameNight={gameNight("Bring snacks")} settings={settings} />);

		expect(screen.getByText("Bring snacks")).toBeInTheDocument();
	});
});
