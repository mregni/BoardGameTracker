import { describe, expect, it, vi } from "vitest";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { RsvpResponseForm } from "./RsvpResponseForm";

vi.mock("@/components/BgtForm", () => ({
	BgtSimpleSelect: ({ items }: { items: { value: number; label: string }[] }) => (
		<ul>
			{items.map((item) => (
				<li key={item.value}>{item.label}</li>
			))}
		</ul>
	),
}));

const rsvp = (id: number, playerId: number, name: string): GameNightRsvps => ({
	id,
	playerId,
	gameNightId: 1,
	state: GameNightRsvpState.Pending,
	player: { id: playerId, name, image: null, email: null, badges: [] },
});

describe("RsvpResponseForm", () => {
	it("does not offer the host as a responder", () => {
		renderWithTheme(
			<RsvpResponseForm
				invitedPlayers={[rsvp(1, 10, "Host Hank"), rsvp(2, 20, "Guest Gina")]}
				hostId={10}
				onSubmit={vi.fn()}
				isSubmitting={false}
			/>,
		);

		expect(screen.queryByText("Host Hank")).not.toBeInTheDocument();
		expect(screen.getByText("Guest Gina")).toBeInTheDocument();
	});
});
