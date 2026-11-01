import { Link } from "@tanstack/react-router";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import type { Game, GameNight, Player, Settings } from "@/models";
import { getDateFnsLocale } from "@/utils/localeUtils";

export const PLAYED_VISIBLE = 4;

interface Props {
	gameNight: GameNight;
	settings: Settings;
	players: Player[];
	games: Game[];
}

export const PastGameNightItem = (props: Props) => {
	const { gameNight, settings, players, games } = props;
	const { t } = useTranslation("game-nights");

	const { gamesPlayed, winnerIds, attendeeIds, attendanceRecorded, playedGameIds } = gameNight.summary;
	const showAttended = gamesPlayed > 0 || attendanceRecorded;
	const winners = winnerIds
		.map((id) => players.find((player) => player.id === id))
		.filter((player): player is Player => player !== undefined);
	const played = (playedGameIds ?? [])
		.map((id) => games.find((game) => game.id === id))
		.filter((game): game is Game => game !== undefined);
	const winnersLabel = winners.length > 1 ? t("past-night.shared-most-wins") : t("past-night.most-wins");

	const summary = [
		gameNight.location.name,
		showAttended && t("past-night.attended-count", { count: attendeeIds.length }),
		gamesPlayed === 0 ? t("past-night.nothing-logged") : t("past-night.games-played", { count: gamesPlayed }),
	]
		.filter(Boolean)
		.join(" · ");

	return (
		<Link
			to="/game-nights/$gameNightId"
			params={{ gameNightId: gameNight.id }}
			className="grid grid-cols-[3.25rem_minmax(0,1fr)_auto] items-center gap-3 rounded-md px-2 py-3 transition-colors hover:bg-white/5 focus-visible:bg-white/5 focus-visible:outline-2 focus-visible:outline-white/80"
		>
			<div className="self-start" aria-hidden="true">
				<GameNightDateBox date={gameNight.startDate} uiLanguage={settings.uiLanguage} variant="compact" />
			</div>
			<div className="flex min-w-0 flex-col gap-0.5">
				<span className="sr-only">
					{format(new Date(gameNight.startDate), "PPPP", { locale: getDateFnsLocale(settings.uiLanguage) })}
				</span>
				<BgtText size="3" weight="bold" className="wrap-anywhere">
					{gameNight.title}
				</BgtText>
				<BgtText size="2" color="gray">
					{summary}
				</BgtText>
				{(played.length > 0 || winners.length > 0) && (
					<div className="mt-1.5 flex flex-wrap items-center gap-x-4 gap-y-2">
						{played.length > 0 && (
							<>
								<span className="sr-only">
									{t("past-night.played", { games: played.map((game) => game.title).join(", ") })}
								</span>
								<div className="flex items-center gap-1.5" aria-hidden="true" data-testid="played-games">
									{played.slice(0, PLAYED_VISIBLE).map((game) => (
										<BgtAvatar key={game.id} title={game.title} image={game.image} size="medium" />
									))}
									{played.length > PLAYED_VISIBLE && (
										<span className="text-xs font-semibold text-gray-400">+{played.length - PLAYED_VISIBLE}</span>
									)}
								</div>
							</>
						)}
						{winners.length > 0 && (
							<>
								<span className="sr-only">{`${winnersLabel}: ${winners.map((winner) => winner.name).join(", ")}`}</span>
								<div className="flex flex-wrap items-center gap-1.5" aria-hidden="true">
									<BgtText size="1" color="gray">
										{winnersLabel}
									</BgtText>
									{winners.map((winner) => (
										<AvatarChip key={winner.id} title={winner.name} image={winner.image} tone="winner" />
									))}
								</div>
							</>
						)}
					</div>
				)}
			</div>
			<CaretRight className="size-4 text-gray-400" aria-hidden="true" />
		</Link>
	);
};
