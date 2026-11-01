import { describe, expect, it, vi } from "vitest";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";
import { renderWithTheme, screen, userEvent, within } from "@/test/test-utils";
import { RsvpResponseForm } from "./RsvpResponseForm";

vi.mock("@/components/BgtForm", () => ({
	BgtSelect: ({
		label,
		items,
		value,
		onValueChange,
	}: {
		label: string;
		items: { value: number; label: string }[];
		value: number | null;
		onValueChange: (value: number) => void;
	}) => (
		<label>
			{label}
			<select value={String(value ?? "")} onChange={(event) => onValueChange(Number(event.target.value))}>
				<option value="">-</option>
				{items.map((item) => (
					<option key={item.value} value={item.value}>
						{item.label}
					</option>
				))}
			</select>
		</label>
	),
}));

const rsvp = (id: number, playerId: number, name: string): GameNightRsvps => ({
	id,
	playerId,
	gameNightId: 1,
	state: GameNightRsvpState.Pending,
	player: { id: playerId, name, image: null, email: null, badges: [] },
});

const invitedPlayers = [rsvp(1, 10, "Host Hank"), rsvp(2, 20, "Guest Gina")];

describe("RsvpResponseForm", () => {
	it("does not offer the host as a responder", () => {
		renderWithTheme(
			<RsvpResponseForm
				invitedPlayers={invitedPlayers}
				hostId={10}
				initialAnswer={null}
				onSubmit={vi.fn()}
				isSubmitting={false}
			/>,
		);

		const names = screen.getByRole("group", { name: "who-are-you" });
		expect(within(names).queryByRole("button", { name: /Host Hank/ })).not.toBeInTheDocument();
		expect(within(names).getByRole("button", { name: /Guest Gina/ })).toBeInTheDocument();
	});

	it("falls back to a searchable name picker for a long guest list", () => {
		const crowd = Array.from({ length: 9 }, (_, i) => rsvp(100 + i, 200 + i, `Guest ${i}`));
		renderWithTheme(
			<RsvpResponseForm
				invitedPlayers={[invitedPlayers[0], ...crowd]}
				hostId={10}
				initialAnswer={null}
				onSubmit={vi.fn()}
				isSubmitting={false}
			/>,
		);

		expect(screen.getByRole("combobox", { name: "who-are-you" })).toBeInTheDocument();
		expect(screen.queryByRole("group", { name: "who-are-you" })).not.toBeInTheDocument();
	});

	it("answers with the picked name and response", async () => {
		const user = userEvent.setup();
		const onSubmit = vi.fn();
		renderWithTheme(
			<RsvpResponseForm
				invitedPlayers={invitedPlayers}
				hostId={10}
				initialAnswer={null}
				onSubmit={onSubmit}
				isSubmitting={false}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Guest Gina/ }));
		await user.click(screen.getByRole("button", { name: "maybe" }));
		await user.click(screen.getByRole("button", { name: "send-answer" }));

		expect(screen.getByRole("button", { name: "maybe" })).toHaveAttribute("aria-pressed", "true");
		expect(onSubmit).toHaveBeenCalledWith(invitedPlayers[1], GameNightRsvpState.Maybe);
	});

	it("preselects the previous answer", () => {
		renderWithTheme(
			<RsvpResponseForm
				invitedPlayers={invitedPlayers}
				hostId={10}
				initialAnswer={{ playerId: 20, state: GameNightRsvpState.Declined }}
				onSubmit={vi.fn()}
				isSubmitting={false}
			/>,
		);

		expect(screen.getByRole("button", { name: /Guest Gina/ })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "cant-come" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "send-answer" })).toBeEnabled();
	});

	it("blocks a second submit while saving", () => {
		renderWithTheme(
			<RsvpResponseForm
				invitedPlayers={invitedPlayers}
				hostId={10}
				initialAnswer={{ playerId: 20, state: GameNightRsvpState.Accepted }}
				onSubmit={vi.fn()}
				isSubmitting
			/>,
		);

		expect(screen.getByRole("button", { name: "send-answer" })).toBeDisabled();
	});
});
