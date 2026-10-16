import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import CheckIcon from "@/assets/icons/check.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import type { GameNight, Player } from "@/models";
import { NightSectionTitle } from "./NightSectionTitle";

interface Props {
	gameNight: GameNight;
	attendees: Player[];
	playedGameIds: number[];
	canWrite: boolean;
	onEditAttendees: () => void;
}

export const NightSidePanel = (props: Props) => {
	const { gameNight, attendees, playedGameIds, canWrite, onEditAttendees } = props;
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();

	return (
		<div className="flex flex-col gap-3">
			<section className="flex flex-col gap-2" aria-labelledby="at-the-table">
				<NightSectionTitle id="at-the-table">{t("table.title")}</NightSectionTitle>
				{attendees.length === 0 ? (
					<BgtText size="2" color="gray">
						{t("past-night.nobody")}
					</BgtText>
				) : (
					<div className="flex flex-wrap gap-2">
						{attendees.map((player) => (
							<AvatarChip
								key={player.id}
								title={player.name}
								image={player.image}
								tone="coming"
								onClick={() => navigate({ to: `/players/${player.id}` })}
							>
								{player.id === gameNight.hostId && <span className="text-gray-400">{t("table.host")}</span>}
							</AvatarChip>
						))}
					</div>
				)}
				{canWrite && (
					<div>
						<BgtButton variant="text" size="1" className="pl-0" onClick={onEditAttendees}>
							{t("table.edit")}
						</BgtButton>
					</div>
				)}
			</section>

			{gameNight.suggestedGames.length > 0 && (
				<section className="flex flex-col gap-2" aria-labelledby="planned-games">
					<NightSectionTitle id="planned-games">{t("planned.title")}</NightSectionTitle>
					<div className="flex flex-wrap gap-2">
						{gameNight.suggestedGames.map((game) => {
							const played = playedGameIds.includes(game.id);
							return (
								<AvatarChip
									key={game.id}
									title={game.title}
									image={game.image}
									tone={played ? "coming" : "neutral"}
									onClick={() => navigate({ to: `/games/${game.id}` })}
								>
									{played && <CheckIcon className="size-3.5 text-green-300" aria-label={t("planned.played")} />}
								</AvatarChip>
							);
						})}
					</div>
				</section>
			)}
		</div>
	);
};
