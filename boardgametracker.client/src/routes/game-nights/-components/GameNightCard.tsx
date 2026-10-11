import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import MapPin from "@/assets/icons/map-pin.svg?react";
import User from "@/assets/icons/user.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import type { GameNight, Settings } from "@/models";
import { relativeDay } from "../-utils/relativeDay";
import { sortRsvps } from "../-utils/sortRsvps";
import { GameNightActions } from "./GameNightActions";
import { GameNightDateBox } from "./GameNightDateBox";
import { RsvpAvatarStack } from "./RsvpAvatarStack";
import { RsvpProgress } from "./RsvpProgress";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	canWrite: boolean;
	highlight?: boolean;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
	onManageRsvps: (gameNight: GameNight) => void;
}

export const GameNightCard = (props: Props) => {
	const { gameNight, settings, canWrite, highlight = false, onEdit, onDelete, onManageRsvps } = props;
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();

	const when = relativeDay(gameNight.startDate);
	const rsvps = sortRsvps(gameNight.invitedPlayers, gameNight.hostId);

	return (
		<BgtCard data-testid="game-night-card">
			<article className="grid grid-cols-[3.5rem_minmax(0,1fr)] gap-3 md:grid-cols-[4.75rem_minmax(0,1fr)_auto] md:gap-x-4 md:gap-y-2.5">
				<GameNightDateBox
					date={gameNight.startDate}
					uiLanguage={settings.uiLanguage}
					variant={highlight ? "highlight" : "soft"}
					className="col-start-1 row-start-1 row-end-2 self-start md:row-end-3"
				/>
				<div className="col-start-2 row-start-1 flex min-w-0 flex-col items-start gap-1.5 self-center">
					<span
						className={cx(
							"rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap tabular-nums",
							highlight ? "bg-primary/20 text-indigo-300" : "bg-white/10 text-gray-300",
						)}
					>
						{t("card.when", {
							when: t(`card.relative.${when.key}`, { count: when.count }),
							time: format(gameNight.startDate, settings.timeFormat),
						})}
					</span>
					<BgtHeading size="5" className="wrap-anywhere">
						{gameNight.title}
					</BgtHeading>
				</div>
				{canWrite && (
					<div className="col-start-1 col-end-3 row-start-2 md:col-start-3 md:col-end-4 md:row-start-1 md:self-start">
						<GameNightActions gameNight={gameNight} onEdit={onEdit} onDelete={onDelete} />
					</div>
				)}
				<div
					className={cx(
						"col-start-1 col-end-3 flex min-w-0 flex-col gap-3 md:col-start-2 md:col-end-4 md:row-start-2",
						canWrite ? "row-start-3" : "row-start-2",
					)}
				>
					<div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-gray-400">
						<span className="inline-flex items-center gap-1.5">
							<MapPin className="size-4 shrink-0" />
							{gameNight.location.name}
						</span>
						<span className="inline-flex items-center gap-1.5">
							<User className="size-4 shrink-0" />
							{t("card.host", { host: gameNight.host.name })}
						</span>
					</div>

					<div className="flex flex-col gap-2">
						<RsvpProgress rsvps={gameNight.invitedPlayers} />
						<div className="flex flex-wrap items-center justify-between gap-2">
							<RsvpAvatarStack rsvps={rsvps} />
							{canWrite && (
								<BgtButton variant="cancel" size="1" onClick={() => onManageRsvps(gameNight)}>
									{t("card.manage-rsvps")}
								</BgtButton>
							)}
						</div>
					</div>

					{gameNight.suggestedGames.length > 0 && (
						<div className="flex flex-wrap items-center gap-2">
							<BgtText size="2" color="gray">
								{t("card.suggested-games")}
							</BgtText>
							{gameNight.suggestedGames.map((game) => (
								<div
									key={game.id}
									className="rounded-full border border-white/10 bg-white/5 py-1 pr-3 pl-1 text-xs font-medium"
								>
									<BgtAvatar
										image={game.image}
										title={game.title}
										size="small"
										withTitle
										onClick={() => navigate({ to: `/games/${game.id}` })}
									/>
								</div>
							))}
						</div>
					)}

					{gameNight.notes && (
						<div className="rounded-lg border border-white/10 bg-white/5 p-3">
							<BgtText size="2" color="gray">
								{gameNight.notes}
							</BgtText>
						</div>
					)}
				</div>
			</article>
		</BgtCard>
	);
};
