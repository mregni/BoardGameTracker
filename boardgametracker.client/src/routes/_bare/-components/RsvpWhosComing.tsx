import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { RsvpGroups } from "@/components/GameNight/RsvpGroups";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import type { GameNight } from "@/models";

interface Props {
	gameNight: GameNight;
}

export const RsvpWhosComing = ({ gameNight }: Props) => {
	const { t } = useTranslation("rsvp");

	return (
		<BgtCard className="gap-3">
			<BgtHeading as="h2" size="4">
				{t("whos-coming")}
			</BgtHeading>
			<RsvpProgress rsvps={gameNight.invitedPlayers} />
			<RsvpGroups gameNight={gameNight} />
		</BgtCard>
	);
};
