import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import type { GameNight } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";
import { NightSectionTitle } from "./NightSectionTitle";
import { RsvpAvatarStack } from "./RsvpAvatarStack";

interface Props {
	gameNight: GameNight;
	canWrite: boolean;
	onManageRsvps: () => void;
}

export const NightPlanPanel = ({ gameNight, canWrite, onManageRsvps }: Props) => {
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();

	return (
		<div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] xl:gap-6" data-testid="night-plan">
			<section className="flex flex-col gap-2" aria-labelledby="night-rsvps">
				<NightSectionTitle id="night-rsvps" keepCase>
					{t("plan.rsvps")}
				</NightSectionTitle>
				<BgtCard className="gap-3">
					<RsvpProgress rsvps={gameNight.invitedPlayers} />
					<div className="flex flex-wrap items-center justify-between gap-2">
						<RsvpAvatarStack rsvps={sortRsvps(gameNight.invitedPlayers, gameNight.hostId)} />
						{canWrite && (
							<BgtButton variant="cancel" size="1" onClick={onManageRsvps}>
								{t("card.manage-rsvps")}
							</BgtButton>
						)}
					</div>
				</BgtCard>
			</section>

			<section className="flex flex-col gap-2" aria-labelledby="planned-games">
				<NightSectionTitle id="planned-games">{t("planned.title")}</NightSectionTitle>
				{gameNight.suggestedGames.length === 0 ? (
					<BgtText size="2" color="gray">
						{t("plan.no-games")}
					</BgtText>
				) : (
					<div className="flex flex-wrap gap-2">
						{gameNight.suggestedGames.map((game) => (
							<AvatarChip
								key={game.id}
								title={game.title}
								image={game.image}
								onClick={() => navigate({ to: `/games/${game.id}` })}
							/>
						))}
					</div>
				)}
			</section>
		</div>
	);
};
