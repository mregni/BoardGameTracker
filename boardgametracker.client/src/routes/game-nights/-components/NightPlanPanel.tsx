import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { PlannedGameList } from "@/components/GameNight/PlannedGameList";
import { RsvpGroups } from "@/components/GameNight/RsvpGroups";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import { type GameNight, GameNightRsvpState } from "@/models";
import { useInviteLink } from "../-hooks/useInviteLink";
import { NightSectionTitle } from "./NightSectionTitle";

interface Props {
	gameNight: GameNight;
	canWrite: boolean;
	onManageRsvps: () => void;
}

export const NightPlanPanel = ({ gameNight, canWrite, onManageRsvps }: Props) => {
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();
	const { copied, copy } = useInviteLink(gameNight);
	const waiting = gameNight.invitedPlayers.filter((rsvp) => rsvp.state === GameNightRsvpState.Pending).length;

	return (
		<div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] xl:gap-6" data-testid="night-plan">
			<section className="flex flex-col gap-2" aria-labelledby="night-rsvps">
				<NightSectionTitle id="night-rsvps">{t("plan.rsvps")}</NightSectionTitle>
				<BgtCard className="gap-3">
					<RsvpProgress rsvps={gameNight.invitedPlayers} />
					<RsvpGroups gameNight={gameNight} onOpenPlayer={(id) => navigate({ to: `/players/${id}` })} />
					{canWrite && (
						<div className="flex flex-col gap-2 border-t border-white/10 pt-3">
							{waiting > 0 && (
								<BgtText size="2" color="gray">
									{t("plan.waiting", { count: waiting })}
								</BgtText>
							)}
							<div className="flex flex-wrap gap-2">
								<BgtButton variant={waiting > 0 ? "primary" : "cancel"} size="1" onClick={copy} disabled={copied}>
									{copied ? t("card.copied") : t("card.copy-link")}
								</BgtButton>
								<BgtButton variant="cancel" size="1" onClick={onManageRsvps}>
									{t("card.manage-rsvps")}
								</BgtButton>
							</div>
						</div>
					)}
				</BgtCard>
			</section>

			<section className="flex flex-col gap-2" aria-labelledby="planned-games">
				<NightSectionTitle id="planned-games">{t("planned.title")}</NightSectionTitle>
				{gameNight.suggestedGames.length === 0 ? (
					<BgtText size="2" color="gray">
						{t("plan.no-games")}
					</BgtText>
				) : (
					<PlannedGameList games={gameNight.suggestedGames} onOpenGame={(id) => navigate({ to: `/games/${id}` })} />
				)}
			</section>
		</div>
	);
};
