import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { AvatarChipButton } from "@/components/GameNight/AvatarChip";
import type { Game, GameNightDetail, Player, RunningGame, Settings } from "@/models";
import { attendeeIds, hasPlayOpened, isNightOver, latestPlays } from "../-utils/gameNightPlay";
import { GameNightDetailHeader } from "./GameNightDetailHeader";
import { NightPlanPanel } from "./NightPlanPanel";
import { NightSectionTitle } from "./NightSectionTitle";
import { NightSidePanel } from "./NightSidePanel";
import { NightSummaryStats } from "./NightSummaryStats";
import { PlayedSessionsList } from "./PlayedSessionsList";
import { RunningGameCard } from "./RunningGameCard";

interface Props {
	detail: GameNightDetail;
	settings: Settings;
	games: Game[];
	players: Player[];
	canWrite: boolean;
	onEdit: () => void;
	onDelete: () => void;
	onEnd: () => void;
	onReopen: () => void;
	onEditAttendees: () => void;
	onStart: (gameId: number | null, playerIds: number[]) => void;
	onLog: () => void;
	onFinish: (runningGame: RunningGame) => void;
	onDiscard: (runningGame: RunningGame) => void;
}

export const GameNightDetailView = (props: Props) => {
	const {
		detail,
		settings,
		games,
		players,
		canWrite,
		onEdit,
		onDelete,
		onEnd,
		onReopen,
		onEditAttendees,
		onStart,
		onLog,
		onFinish,
		onDiscard,
	} = props;
	const { t } = useTranslation("game-nights");
	const { gameNight, sessions, runningGames } = detail;

	const isOver = isNightOver(detail);
	const isPlanned = gameNight.endedAt === null && !hasPlayOpened(gameNight);
	const canPlay = canWrite && gameNight.endedAt === null && !isPlanned;
	const isOverByTime = isOver && gameNight.endedAt === null;
	const gameOf = (gameId: number) => games.find((game) => game.id === gameId) ?? null;
	const attendees = attendeeIds(gameNight)
		.map((id) => players.find((player) => player.id === id))
		.filter((player): player is Player => player !== undefined);
	const playAgain = latestPlays(sessions);
	const startWithAttendees = () => onStart(null, attendeeIds(gameNight));

	return (
		<div className={cx("flex flex-col gap-3 xl:gap-6", canPlay && "pb-16 md:pb-0")}>
			<GameNightDetailHeader
				gameNight={gameNight}
				settings={settings}
				isOver={isOver}
				isPlanned={isPlanned}
				canWrite={canWrite}
				hasRunningGames={runningGames.length > 0}
				onEdit={onEdit}
				onDelete={onDelete}
				onEnd={onEnd}
				onReopen={onReopen}
				onStart={startWithAttendees}
				onLog={onLog}
			/>

			{isPlanned && <NightPlanPanel gameNight={gameNight} canWrite={canWrite} onManageRsvps={onEditAttendees} />}

			{runningGames.map((runningGame) => (
				<RunningGameCard
					key={runningGame.id}
					runningGame={runningGame}
					game={gameOf(runningGame.gameId)}
					players={players}
					settings={settings}
					canWrite={canWrite}
					onFinish={onFinish}
					onDiscard={onDiscard}
				/>
			))}

			{canPlay && !isOver && playAgain.length > 0 && (
				<div className="flex flex-wrap items-center gap-2">
					<span className="text-sm text-gray-400">{t("play.again")}</span>
					{playAgain.map((session) => {
						const game = gameOf(session.gameId);
						return (
							<AvatarChipButton
								key={session.gameId}
								title={game?.title ?? ""}
								image={game?.image}
								label={t("play.again-game", { game: game?.title ?? "" })}
								onClick={() =>
									onStart(
										session.gameId,
										session.playerSessions.map((x) => x.playerId),
									)
								}
							/>
						);
					})}
				</div>
			)}

			{!isPlanned && sessions.length > 0 && (
				<NightSummaryStats
					summary={gameNight.summary}
					players={players}
					isOver={isOver}
					uiLanguage={settings.uiLanguage}
				/>
			)}

			{!isPlanned && (
				<div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] xl:gap-6">
					<section className="flex flex-col gap-2" aria-labelledby="played-games">
						<NightSectionTitle id="played-games">{isOver ? t("played.after") : t("played.tonight")}</NightSectionTitle>
						<PlayedSessionsList
							sessions={sessions}
							games={games}
							players={players}
							uiLanguage={settings.uiLanguage}
							canOpen={canWrite}
							emptyText={isOverByTime ? t("played.empty-after") : t("played.empty")}
						/>
					</section>
					<NightSidePanel
						gameNight={gameNight}
						attendees={attendees}
						playedGameIds={sessions.map((x) => x.gameId)}
						canWrite={canWrite}
						onEditAttendees={onEditAttendees}
					/>
				</div>
			)}
		</div>
	);
};
