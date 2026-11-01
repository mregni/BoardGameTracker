import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import Check from "@/assets/icons/check.svg?react";
import CircleHelp from "@/assets/icons/circle-help.svg?react";
import Crown from "@/assets/icons/crown.svg?react";
import X from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

export const MOBILE_MAX = 5;
export const DESKTOP_MAX = 8;

const stateStyle: Record<GameNightRsvpState, { ring: string; label: string }> = {
	[GameNightRsvpState.Accepted]: { ring: "ring-green-400", label: "rsvp.coming" },
	[GameNightRsvpState.Maybe]: { ring: "ring-amber-400", label: "rsvp.maybe" },
	[GameNightRsvpState.Pending]: { ring: "ring-white/25", label: "rsvp.no-answer" },
	[GameNightRsvpState.Declined]: { ring: "ring-red-400/70 opacity-60", label: "rsvp.not-coming" },
};

interface Props {
	rsvps: GameNightRsvps[];
	hostId?: number;
	linkPlayers?: boolean;
}

export const RsvpAvatarStack = ({ rsvps, hostId, linkPlayers = true }: Props) => {
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();
	const isDesktop = useMediaQuery("(min-width: 768px)");
	const [showAll, setShowAll] = useState(false);
	const max = showAll ? rsvps.length : isDesktop ? DESKTOP_MAX : MOBILE_MAX;

	const visible = rsvps.slice(0, max);
	const hidden = rsvps.slice(max);

	return (
		<ul className="flex flex-wrap items-center gap-3" data-testid="rsvp-avatar-stack">
			{visible.map((rsvp) => {
				const isHost = rsvp.playerId === hostId;
				const name = isHost ? t("rsvp.host-name", { name: rsvp.player.name }) : rsvp.player.name;
				const label = t("rsvp.avatar-label", { name, state: t(stateStyle[rsvp.state].label) });
				return (
					<li
						key={rsvp.id}
						className={cx("rounded-md ring-2 ring-offset-2 ring-offset-background", stateStyle[rsvp.state].ring)}
					>
						{!linkPlayers && <span className="sr-only">{label}</span>}
						<div className="relative" aria-hidden={linkPlayers ? undefined : true}>
							<BgtAvatar
								title={label}
								colorKey={rsvp.player.name}
								image={rsvp.player.image}
								onClick={linkPlayers ? () => navigate({ to: `/players/${rsvp.player.id}` }) : undefined}
							/>
							{isHost && (
								<span
									className="pointer-events-none absolute -top-2 -left-2 flex size-4 items-center justify-center rounded-full bg-background text-yellow-300 ring-2 ring-background"
									data-host-badge
								>
									<Crown className="size-3" aria-hidden="true" />
								</span>
							)}
							<StateBadge state={rsvp.state} />
						</div>
					</li>
				);
			})}
			{hidden.length > 0 && (
				<li className="relative z-10">
					<button
						type="button"
						onClick={() => setShowAll(true)}
						aria-label={t("rsvp.show-all", { count: rsvps.length })}
						className="inline-flex h-11 min-w-11 items-center justify-center rounded-md bg-card-light px-1.5 text-xs font-semibold text-gray-300 hover:bg-white/15 hover:text-white md:h-7 md:min-w-7"
					>
						+{hidden.length}
					</button>
				</li>
			)}
		</ul>
	);
};

const StateBadge = ({ state }: { state: GameNightRsvpState }) => {
	const badge =
		"pointer-events-none absolute -right-2 -bottom-2 flex size-4 items-center justify-center rounded-full ring-2 ring-background";

	switch (state) {
		case GameNightRsvpState.Accepted:
			return (
				<span className={cx(badge, "bg-green-400 text-background")} data-rsvp-badge="accepted">
					<Check className="size-3" aria-hidden="true" />
				</span>
			);
		case GameNightRsvpState.Maybe:
			return (
				<span className={cx(badge, "bg-background text-amber-400")} data-rsvp-badge="maybe">
					<CircleHelp className="size-4" aria-hidden="true" />
				</span>
			);
		case GameNightRsvpState.Declined:
			return (
				<span className={cx(badge, "bg-red-400 text-background")} data-rsvp-badge="declined">
					<X className="size-3" aria-hidden="true" />
				</span>
			);
		default:
			return null;
	}
};
