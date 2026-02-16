import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { format, isPast } from "date-fns";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Clock from "@/assets/icons/clock.svg?react";
import MapPin from "@/assets/icons/map-pin.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { type GameNight, GameNightRsvpState, type Settings } from "@/models";
import { GameNightActions } from "./GameNightActions";
import { RsvpSection } from "./RsvpSection";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
	onManageRsvps: (gameNight: GameNight) => void;
}

export const GameNightCard = (props: Props) => {
	const { gameNight, settings, onEdit, onDelete, onManageRsvps } = props;
	const { t } = useTranslation();
	const navigate = useNavigate();

	const isUpcoming = !isPast(gameNight.startDate);

	return (
		<BgtCard>
			<div className="flex flex-col gap-4">
				<div className="flex items-start justify-between gap-4">
					<div className="flex-1">
						<div className="flex items-center gap-3 mb-2">
							<BgtText size="5" weight="bold">
								{gameNight.title}
							</BgtText>
							<span
								className={cx(
									"px-2 py-1 rounded text-xs",
									isUpcoming ? "bg-green-500/20 text-green-400" : "bg-gray-500/20 text-gray-400",
								)}
							>
								{isUpcoming ? t("game-nights.card.upcoming") : t("game-nights.card.past")}
							</span>
						</div>
						<div className="flex flex-wrap gap-4 text-sm">
							<BgtText size="2" color="gray" className="flex flex-row gap-2 items-center">
								<Calendar className="size-5" />
								{format(gameNight.startDate, settings.dateFormat)}
							</BgtText>
							<BgtText size="2" color="gray" className="flex flex-row gap-2 items-center">
								<Clock className="size-5" />
								{format(gameNight.startDate, settings.timeFormat)}
							</BgtText>
							<BgtText size="2" color="gray" className="flex flex-row gap-2 items-center">
								<MapPin className="size-5" />
								{gameNight.location.name}
							</BgtText>
							<BgtText size="2" color="gray" className="flex flex-row gap-2 items-center">
								<Users className="size-5" />
								{t("game-nights.card.hosted-by", { host: gameNight.host.name })}
							</BgtText>
						</div>
					</div>
					<div className="flex gap-2">
						<BgtButton onClick={() => onEdit(gameNight)} variant="text" size="2">
							{t("common.edit")}
						</BgtButton>
						<BgtButton onClick={() => onDelete(gameNight)} variant="error" size="2">
							{t("common.delete.button")}
						</BgtButton>
					</div>
				</div>

				<div className="grid grid-cols-1 md:grid-cols-3 gap-4">
					<RsvpSection
						title={t("common.accepted")}
						count={gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Accepted).length}
						players={gameNight.invitedPlayers
							.filter((x) => x.state === GameNightRsvpState.Accepted)
							.map((x) => x.player)}
						variant="accepted"
					/>
					<RsvpSection
						title={t("common.pending")}
						count={gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Pending).length}
						players={gameNight.invitedPlayers
							.filter((x) => x.state === GameNightRsvpState.Pending)
							.map((x) => x.player)}
						variant="pending"
					/>
					<RsvpSection
						title={t("common.declined")}
						count={gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Declined).length}
						players={gameNight.invitedPlayers
							.filter((x) => x.state === GameNightRsvpState.Declined)
							.map((x) => x.player)}
						variant="declined"
					/>
				</div>

				{gameNight.suggestedGames.length > 0 && (
					<div className="flex flex-col gap-2">
						<BgtText size="2" weight="medium" color="gray" className="mb-2">
							{t("game-nights.card.suggested-games")}:
						</BgtText>
						<div className="flex flex-wrap gap-2">
							{gameNight.suggestedGames.map((game) => (
								<div
									key={game.id}
									className={cx("pl-1 pr-2 py-1 bg-primary/20 text-primary border border-primary/30 rounded text-sm")}
								>
									<BgtAvatar
										image={game.image}
										title={game.title}
										withTitle
										onClick={() => navigate({ to: `/games/${game.id}` })}
									/>
								</div>
							))}
						</div>
					</div>
				)}

				{gameNight.notes && (
					<div className="bg-white/5 border border-white/10 rounded-lg p-3">
						<BgtText size="2" color="gray">
							{gameNight.notes}
						</BgtText>
					</div>
				)}

				{isUpcoming && <GameNightActions gameNight={gameNight} onManageRsvps={onManageRsvps} />}
			</div>
		</BgtCard>
	);
};
