import { describe, expect, it } from "vitest";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { RsvpProgress } from "./RsvpProgress";

const rsvp = (id: number, state: GameNightRsvpState) => ({ id, playerId: id, gameNightId: 1, state }) as GameNightRsvps;

const segments = () => screen.queryAllByTestId("progress-segment");

describe("RsvpProgress", () => {
	it("shows the invited count, the summary and a bar per answer in order", () => {
		renderWithTheme(
			<RsvpProgress
				rsvps={[
					rsvp(1, GameNightRsvpState.Declined),
					rsvp(2, GameNightRsvpState.Accepted),
					rsvp(3, GameNightRsvpState.Accepted),
					rsvp(4, GameNightRsvpState.Pending),
				]}
			/>,
		);

		expect(screen.getByText("rsvp.invited")).toBeInTheDocument();
		const summary = "rsvp.summary.coming · rsvp.summary.pending · rsvp.summary.declined";
		expect(screen.getByTestId("rsvp-summary")).toHaveTextContent(summary);
		const bar = screen.getByRole("img", { name: summary });
		expect(bar).toHaveClass("h-2", "bg-white/10");
		expect(segments().map((segment) => segment.dataset.tone)).toEqual(["ok", "warn", "bad"]);
		expect(segments()[0]).toHaveStyle({ flexGrow: "50" });
		expect(segments()[1]).toHaveStyle({ flexGrow: "25" });
		expect(segments()[2]).toHaveStyle({ flexGrow: "25" });
		expect(segments()[0]).toHaveAttribute("title", "rsvp.summary.coming");
	});

	it("skips answers nobody gave", () => {
		renderWithTheme(
			<RsvpProgress rsvps={[rsvp(1, GameNightRsvpState.Accepted), rsvp(2, GameNightRsvpState.Pending)]} />,
		);

		expect(screen.getByRole("img", { name: "rsvp.summary.coming · rsvp.summary.pending" })).toBeInTheDocument();
		expect(segments().map((segment) => segment.dataset.tone)).toEqual(["ok", "warn"]);
	});

	it("renders an empty bar without invites", () => {
		renderWithTheme(<RsvpProgress rsvps={[]} />);

		expect(screen.getByTestId("rsvp-summary")).toBeEmptyDOMElement();
		expect(segments()).toHaveLength(0);
	});
});
