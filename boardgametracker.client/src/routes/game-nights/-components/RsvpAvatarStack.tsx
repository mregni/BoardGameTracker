import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

interface Props {
	rsvps: GameNightRsvps[];
	mobileMax?: number;
	desktopMax?: number;
}

const ring = "max-md:ring-2 max-md:ring-[color-mix(in_srgb,var(--color-primary)_10%,var(--color-background))]";
const surface = cx("max-md:bg-[color-mix(in_srgb,var(--color-primary)_10%,var(--color-background))]", ring);

const stateOpacity: Record<GameNightRsvpState, string> = {
	[GameNightRsvpState.Accepted]: "",
	[GameNightRsvpState.Pending]: "opacity-60",
	[GameNightRsvpState.Declined]: "opacity-35",
};

const MoreChip = ({ rsvps, className }: { rsvps: GameNightRsvps[]; className: string }) => (
	<span
		title={rsvps.map((rsvp) => rsvp.player.name).join(", ")}
		className={cx(
			"-ml-1 h-7 min-w-7 items-center justify-center rounded-md bg-card-light px-1.5 text-xs font-semibold text-gray-300 md:ml-0",
			ring,
			className,
		)}
	>
		+{rsvps.length}
	</span>
);

export const RsvpAvatarStack = (props: Props) => {
	const { rsvps, mobileMax = 5, desktopMax = 8 } = props;
	const navigate = useNavigate();

	const visible = rsvps.slice(0, Math.max(mobileMax, desktopMax));
	const hiddenOnMobile = rsvps.slice(mobileMax);
	const hiddenOnDesktop = rsvps.slice(desktopMax);

	return (
		<div className="flex flex-wrap items-center md:gap-1.5" data-testid="rsvp-avatar-stack">
			{visible.map((rsvp, index) => (
				<div
					key={rsvp.id}
					data-mobile-hidden={index >= mobileMax || undefined}
					data-desktop-hidden={index >= desktopMax || undefined}
					className={cx(
						"-ml-1 rounded-md first:ml-0 md:ml-0",
						surface,
						index >= mobileMax && "max-md:hidden",
						index >= desktopMax && "md:hidden",
					)}
				>
					<div className={stateOpacity[rsvp.state]}>
						<BgtAvatar
							title={rsvp.player.name}
							image={rsvp.player.image}
							onClick={() => navigate({ to: `/players/${rsvp.player.id}` })}
						/>
					</div>
				</div>
			))}
			{hiddenOnMobile.length > 0 && <MoreChip rsvps={hiddenOnMobile} className="inline-flex md:hidden" />}
			{hiddenOnDesktop.length > 0 && <MoreChip rsvps={hiddenOnDesktop} className="hidden md:inline-flex" />}
		</div>
	);
};
