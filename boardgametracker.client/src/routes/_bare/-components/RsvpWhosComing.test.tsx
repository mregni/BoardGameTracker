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
		expect(within(declined).getByRole("listitem").querySelector("svg")).toBeNull();
	});

	it("shows maybe apart from no answer, between coming and no answer", () => {
		renderWithTheme(
			<RsvpWhosComing
				gameNight={gameNight([
					rsvp(1, "Hank", GameNightRsvpState.Accepted),
					rsvp(6, "Gina", GameNightRsvpState.Pending),
					rsvp(9, "Mona", GameNightRsvpState.Maybe),
				])}
			/>,
		);

		const maybe = screen.getByRole("region", { name: "group.maybe" });
		const pending = screen.getByRole("region", { name: "group.pending" });
		expect(within(maybe).getByText("Mona")).toBeInTheDocument();
		expect(within(maybe).getByText("Mona").closest(".rounded-full")).toHaveClass("border-amber-400/50");
		expect(within(pending).queryByText("Mona")).not.toBeInTheDocument();
		expect(within(pending).getByText("Gina").closest(".rounded-full")).toHaveClass("border-white/20");
		expect(screen.getAllByRole("region").map((region) => region.getAttribute("aria-labelledby"))).toEqual([
			"rsvp-group-accepted",
			"rsvp-group-maybe",
			"rsvp-group-pending",
		]);
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
