import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtText } from "@/components/BgtText/BgtText";
import { type GameNight, GameNightRsvpState, type Settings } from "@/models";
import { sortRsvps } from "../-utils/sortRsvps";
import { AvatarChip } from "./AvatarChip";
import { GameNightDateBox } from "./GameNightDateBox";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	canWrite: boolean;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
}

export const PastGameNightItem = (props: Props) => {
	const { gameNight, settings, canWrite, onEdit, onDelete } = props;
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();

	const attendees = sortRsvps(gameNight.invitedPlayers, gameNight.hostId).filter(
		(rsvp) => rsvp.state === GameNightRsvpState.Accepted,
	);

	return (
		<details className="group rounded-lg border border-primary/20 bg-primary/10">
			<summary className="grid cursor-pointer list-none grid-cols-[3.25rem_minmax(0,1fr)_auto] items-center gap-3 px-3 py-2.5 [&::-webkit-details-marker]:hidden">
				<GameNightDateBox date={gameNight.startDate} uiLanguage={settings.uiLanguage} variant="compact" />
				<div className="flex min-w-0 flex-col gap-0.5">
					<BgtText size="3" weight="bold" className="wrap-anywhere">
						{gameNight.title}
					</BgtText>
					<BgtText size="2" color="gray">
						{t("past-night.summary", {
							location: gameNight.location.name,
							attended: t("past-night.attended-count", { count: attendees.length }),
						})}
					</BgtText>
				</div>
				<CaretRight className="size-4 text-gray-400 transition-transform group-open:rotate-90" />
			</summary>
			<div className="flex flex-col gap-3 border-t border-dashed border-white/10 p-3">
				<div className="flex flex-col gap-1.5">
					<BgtText size="2" color="gray">
						{t("past-night.attended")}
					</BgtText>
					{attendees.length === 0 ? (
						<BgtText size="2">{t("past-night.nobody")}</BgtText>
					) : (
						<div className="flex flex-wrap gap-2">
							{attendees.map((rsvp) => (
								<AvatarChip
									key={rsvp.id}
									title={rsvp.player.name}
									image={rsvp.player.image}
									onClick={() => navigate({ to: `/players/${rsvp.player.id}` })}
								/>
							))}
						</div>
					)}
				</div>
				{canWrite && (
					<div className="flex justify-end">
						<BgtEditDeleteButtons onEdit={() => onEdit(gameNight)} onDelete={() => onDelete(gameNight)} />
					</div>
				)}
			</div>
		</details>
	);
};
