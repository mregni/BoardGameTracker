import { Link, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import MapPin from "@/assets/icons/map-pin.svg?react";
import User from "@/assets/icons/user.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import type { GameNight, Player, Settings } from "@/models";
import { relativeDay } from "@/utils/relativeDay";
import { sortRsvps } from "@/utils/sortRsvps";
import { GameNightActions } from "./GameNightActions";
import { RsvpAvatarStack } from "./RsvpAvatarStack";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	players: Player[];
	canWrite: boolean;
	highlight?: boolean;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
	onManageRsvps: (gameNight: GameNight) => void;
}

export const GameNightCard = (props: Props) => {
	const { gameNight, settings, players, canWrite, highlight = false, onEdit, onDelete, onManageRsvps } = props;
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();

	const rsvps = sortRsvps(gameNight.invitedPlayers, gameNight.hostId);

	return (
		<BgtCard data-testid="game-night-card">
			<article className="flex flex-col gap-3 md:grid md:grid-cols-[4.75rem_minmax(0,1fr)_auto] md:gap-x-4 md:gap-y-2.5">
				<div className="flex gap-3 md:contents">
					<GameNightDateBox
						date={gameNight.startDate}
						uiLanguage={settings.uiLanguage}
						variant={highlight ? "highlight" : "soft"}
						className="w-14 shrink-0 self-start md:row-span-2 md:w-auto"
					/>
					<div className="flex min-w-0 flex-col items-start gap-1.5 self-center">
						<span
							className={cx(
								"rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap tabular-nums first-letter:uppercase",
								highlight ? "bg-primary/20 text-indigo-300" : "bg-white/10 text-gray-300",
							)}
						>
							{t("card.when", {
								when: relativeDay(gameNight.startDate, settings.uiLanguage),
								time: format(gameNight.startDate, settings.timeFormat),
							})}
						</span>
						<BgtHeading size="5" className="wrap-anywhere">
							<Link
								to="/game-nights/$gameNightId"
								params={{ gameNightId: gameNight.id }}
								className="hover:text-primary-text hover:underline"
							>
								{gameNight.title}
							</Link>
						</BgtHeading>
					</div>
				</div>
				{canWrite && (
					<div className="md:self-start">
						<GameNightActions gameNight={gameNight} players={players} onEdit={onEdit} onDelete={onDelete} />
					</div>
				)}
				<div className="flex min-w-0 flex-col gap-3 md:col-span-2">
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
							<div className="flex flex-wrap items-center gap-2">
								{canWrite && (
									<BgtButton variant="cancel" size="1" onClick={() => onManageRsvps(gameNight)}>
										{t("card.manage-rsvps")}
									</BgtButton>
								)}
								<BgtButton
									variant={highlight ? "primary" : "cancel"}
									size="1"
									onClick={() => navigate({ to: "/game-nights/$gameNightId", params: { gameNightId: gameNight.id } })}
								>
									{t("card.open")}
								</BgtButton>
							</div>
						</div>
					</div>

					{gameNight.suggestedGames.length > 0 && (
						<div className="flex flex-wrap items-center gap-2">
							<BgtText size="2" color="gray">
								{t("card.suggested-games")}
							</BgtText>
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
