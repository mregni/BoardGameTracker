import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

interface Props {
	rsvps: GameNightRsvps[];
	max?: number;
}

const ring = "ring-2 ring-[color-mix(in_srgb,var(--color-primary)_10%,var(--color-background))]";
const surface = cx("bg-[color-mix(in_srgb,var(--color-primary)_10%,var(--color-background))]", ring);

const stateOpacity: Record<GameNightRsvpState, string> = {
	[GameNightRsvpState.Accepted]: "",
	[GameNightRsvpState.Pending]: "opacity-60",
	[GameNightRsvpState.Declined]: "opacity-35",
};

export const RsvpAvatarStack = (props: Props) => {
	const { rsvps, max = 5 } = props;
	const navigate = useNavigate();

	const visible = rsvps.slice(0, max);
	const hidden = rsvps.slice(max);

	return (
		<div className="flex items-center" data-testid="rsvp-avatar-stack">
			{visible.map((rsvp) => (
				<div key={rsvp.id} className={cx("-ml-1 rounded-md first:ml-0", surface)}>
					<div className={stateOpacity[rsvp.state]}>
						<BgtAvatar
							title={rsvp.player.name}
							image={rsvp.player.image}
							onClick={() => navigate({ to: `/players/${rsvp.player.id}` })}
						/>
					</div>
				</div>
			))}
			{hidden.length > 0 && (
				<span
					title={hidden.map((rsvp) => rsvp.player.name).join(", ")}
					className={cx(
						"-ml-1 inline-flex h-7 min-w-7 items-center justify-center rounded-md px-1.5 text-xs font-semibold text-gray-300 bg-card-light",
						ring,
					)}
				>
					+{hidden.length}
				</span>
			)}
		</div>
	);
};
