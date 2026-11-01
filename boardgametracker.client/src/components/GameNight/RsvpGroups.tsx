import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { textSizeClasses } from "@/components/BgtText/textStyles";
import { type GameNight, GameNightRsvpState } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";
import { AvatarChip } from "./AvatarChip";

const groups = [
	{ state: GameNightRsvpState.Accepted, label: "group.coming", tone: "coming" },
	{ state: GameNightRsvpState.Maybe, label: "group.maybe", tone: "maybe" },
	{ state: GameNightRsvpState.Pending, label: "group.pending", tone: "pending" },
	{ state: GameNightRsvpState.Declined, label: "group.declined", tone: "declined" },
] as const;

interface Props {
	gameNight: GameNight;
	onOpenPlayer?: (playerId: number) => void;
}

export const RsvpGroups = ({ gameNight, onOpenPlayer }: Props) => {
	const { t } = useTranslation("rsvp");
	const rsvps = sortRsvps(gameNight.invitedPlayers, gameNight.hostId);

	return groups.map((group) => {
		const players = rsvps.filter((rsvp) => rsvp.state === group.state);
		if (players.length === 0) {
			return null;
		}

		const headingId = `rsvp-group-${group.state}`;
		return (
			<section key={group.state} aria-labelledby={headingId} className="flex flex-col gap-1.5">
				<h3 className={cx(textSizeClasses["2"], "text-gray-400")} id={headingId}>
					{t(group.label, { count: players.length })}
				</h3>
				<ul className="flex flex-wrap gap-2" data-testid={headingId}>
					{players.map((rsvp) => (
						<li key={rsvp.id}>
							<AvatarChip
								title={rsvp.player.name}
								image={rsvp.player.image}
								tone={group.tone}
								onClick={onOpenPlayer && (() => onOpenPlayer(rsvp.playerId))}
							>
								{rsvp.playerId === gameNight.hostId && <span className="text-gray-400">· {t("host")}</span>}
							</AvatarChip>
						</li>
					))}
				</ul>
			</section>
		);
	});
};
