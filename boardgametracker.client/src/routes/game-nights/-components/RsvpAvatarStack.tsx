import { useNavigate } from "@tanstack/react-router";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

export const MOBILE_MAX = 5;
export const DESKTOP_MAX = 8;

const stateOpacity: Record<GameNightRsvpState, string> = {
	[GameNightRsvpState.Accepted]: "",
	[GameNightRsvpState.Maybe]: "opacity-80",
	[GameNightRsvpState.Pending]: "opacity-60",
	[GameNightRsvpState.Declined]: "opacity-35",
};

export const RsvpAvatarStack = ({ rsvps }: { rsvps: GameNightRsvps[] }) => {
	const navigate = useNavigate();
	const isDesktop = useMediaQuery("(min-width: 768px)");
	const max = isDesktop ? DESKTOP_MAX : MOBILE_MAX;

	const visible = rsvps.slice(0, max);
	const hidden = rsvps.slice(max);

	return (
		<div className="flex flex-wrap items-center gap-1.5" data-testid="rsvp-avatar-stack">
			{visible.map((rsvp) => (
				<div key={rsvp.id} className={stateOpacity[rsvp.state]}>
					<BgtAvatar
						title={rsvp.player.name}
						image={rsvp.player.image}
						onClick={() => navigate({ to: `/players/${rsvp.player.id}` })}
					/>
				</div>
			))}
			{hidden.length > 0 && (
				<span
					title={hidden.map((rsvp) => rsvp.player.name).join(", ")}
					className="inline-flex h-7 min-w-7 items-center justify-center rounded-md bg-card-light px-1.5 text-xs font-semibold text-gray-300"
				>
					+{hidden.length}
				</span>
			)}
		</div>
	);
};
