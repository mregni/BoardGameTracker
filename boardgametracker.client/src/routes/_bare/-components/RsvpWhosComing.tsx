import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import { type GameNight, GameNightRsvpState } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";

const groups = [
	{ state: GameNightRsvpState.Accepted, label: "group.coming", tone: "coming" },
	{ state: GameNightRsvpState.Pending, label: "group.pending", tone: "pending" },
	{ state: GameNightRsvpState.Declined, label: "group.declined", tone: "declined" },
] as const;

interface Props {
	gameNight: GameNight;
}

export const RsvpWhosComing = ({ gameNight }: Props) => {
	const { t } = useTranslation("rsvp");
	const rsvps = sortRsvps(gameNight.invitedPlayers, gameNight.hostId);

	return (
		<BgtCard className="gap-3">
			<BgtText size="4" weight="bold">
				{t("whos-coming")}
			</BgtText>
			<RsvpProgress rsvps={gameNight.invitedPlayers} />
			{groups.map((group) => {
				const players = rsvps.filter((rsvp) => rsvp.state === group.state);
				if (players.length === 0) {
					return null;
				}

				const headingId = `rsvp-group-${group.state}`;
				return (
					<section key={group.state} aria-labelledby={headingId} className="flex flex-col gap-1.5">
						<BgtText size="2" color="gray" id={headingId}>
							{t(group.label, { count: players.length })}
						</BgtText>
						<ul className="flex flex-wrap gap-2" data-testid={headingId}>
							{players.map((rsvp) => (
								<li key={rsvp.id}>
									<AvatarChip title={rsvp.player.name} image={rsvp.player.image} tone={group.tone}>
										{rsvp.playerId === gameNight.hostId && <span className="text-gray-400">· {t("host")}</span>}
									</AvatarChip>
								</li>
							))}
						</ul>
					</section>
				);
			})}
		</BgtCard>
	);
};
