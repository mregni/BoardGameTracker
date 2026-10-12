import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import { type GameNight, GameNightRsvpState } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";

const states = {
	[GameNightRsvpState.Accepted]: { label: "coming", className: "" },
	[GameNightRsvpState.Pending]: { label: "maybe", className: "opacity-60" },
	[GameNightRsvpState.Declined]: { label: "cant-come", className: "opacity-35 grayscale" },
} as const;

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
			{rsvps.length > 0 && (
				<ul className="flex flex-wrap gap-2" data-testid="rsvp-players">
					{rsvps.map((rsvp) => (
						<li key={rsvp.id} className={states[rsvp.state].className}>
							<AvatarChip title={rsvp.player.name} image={rsvp.player.image} />
							<span className="sr-only">{t(states[rsvp.state].label)}</span>
						</li>
					))}
				</ul>
			)}
		</BgtCard>
	);
};
