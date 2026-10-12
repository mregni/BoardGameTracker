import { describe, expect, it } from "vitest";
import { type GameNight, GameNightRsvpState, type Player } from "@/models";
import { renderWithTheme, screen, within } from "@/test/test-utils";
import { RsvpWhosComing } from "./RsvpWhosComing";

const player = (id: number, name: string) => ({ id, name, image: null }) as unknown as Player;

const rsvp = (id: number, name: string, state: GameNightRsvpState) => ({
	id,
	playerId: id,
	gameNightId: 4,
	state,
	player: player(id, name),
});

const gameNight = (invitedPlayers: ReturnType<typeof rsvp>[]) =>
	({ id: 4, hostId: 1, host: player(1, "Hank"), invitedPlayers }) as unknown as GameNight;

describe("RsvpWhosComing", () => {
	it("groups the players by answer with their counts, host first", () => {
		renderWithTheme(
			<RsvpWhosComing
				gameNight={gameNight([
					rsvp(5, "Anna", GameNightRsvpState.Accepted),
					rsvp(1, "Hank", GameNightRsvpState.Accepted),
					rsvp(6, "Gina", GameNightRsvpState.Pending),
					rsvp(7, "Bert", GameNightRsvpState.Pending),
					rsvp(8, "Olaf", GameNightRsvpState.Declined),
				])}
			/>,
		);

		const coming = screen.getByRole("region", { name: "group.coming" });
		const pending = screen.getByRole("region", { name: "group.pending" });
		const declined = screen.getByRole("region", { name: "group.declined" });

		expect(within(coming).getAllByRole("listitem")).toHaveLength(2);
		expect(within(coming).getAllByRole("listitem")[0]).toHaveTextContent("Hank· host");
		expect(within(pending).getAllByRole("listitem")).toHaveLength(2);
		expect(within(declined).getAllByRole("listitem")).toHaveLength(1);
		expect(within(declined).getByText("Olaf")).toBeInTheDocument();
		expect(within(declined).getByText("Olaf").closest(".rounded-full")).toHaveClass("border-red-400/50");
	});

	it("hides groups without players", () => {
		renderWithTheme(
			<RsvpWhosComing
				gameNight={gameNight([
					rsvp(1, "Hank", GameNightRsvpState.Accepted),
					rsvp(6, "Gina", GameNightRsvpState.Pending),
				])}
			/>,
		);

		expect(screen.getByRole("region", { name: "group.coming" })).toBeInTheDocument();
		expect(screen.getByRole("region", { name: "group.pending" })).toBeInTheDocument();
		expect(screen.queryByRole("region", { name: "group.declined" })).not.toBeInTheDocument();
	});
});
