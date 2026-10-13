import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { AvatarChipButton } from "@/components/GameNight/AvatarChip";
import type { Game, GameNightDetail, Player, RunningGame, Settings } from "@/models";
import { attendeeIds, isNightOver, latestPlays } from "../-utils/gameNightPlay";
import { GameNightDetailHeader } from "./GameNightDetailHeader";
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
	const canPlay = canWrite && gameNight.endedAt === null;
	const gameOf = (gameId: number) => games.find((game) => game.id === gameId) ?? null;
	const attendees = attendeeIds(gameNight)
		.map((id) => players.find((player) => player.id === id))
		.filter((player): player is Player => player !== undefined);
	const playAgain = latestPlays(sessions);

	return (
		<div className={cx("flex flex-col gap-3 xl:gap-6", canPlay && "pb-16 md:pb-0")}>
			<GameNightDetailHeader
				gameNight={gameNight}
				settings={settings}
				isOver={isOver}
				canWrite={canWrite}
				hasRunningGames={runningGames.length > 0}
				onEdit={onEdit}
				onDelete={onDelete}
				onEnd={onEnd}
				onReopen={onReopen}
			/>

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

			{canPlay && (
				<div className="flex flex-col gap-2">
					<div className="fixed inset-x-0 bottom-20 z-20 grid grid-cols-2 gap-2 border-t border-white/10 bg-background/95 px-4 py-3 backdrop-blur-sm md:static md:flex md:border-0 md:bg-transparent md:p-0 md:backdrop-blur-none">
						<BgtButton
							variant="primary"
							size="2"
							className="py-3 text-sm"
							onClick={() => onStart(null, attendeeIds(gameNight))}
						>
							{t("play.start.button")}
						</BgtButton>
						<BgtButton variant="cancel" size="2" className="py-3 text-sm" onClick={onLog}>
							{t("play.log.button")}
						</BgtButton>
					</div>
					{!isOver && playAgain.length > 0 && (
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
				</div>
			)}

			{sessions.length > 0 && (
				<NightSummaryStats
					summary={gameNight.summary}
					players={players}
					isOver={isOver}
					uiLanguage={settings.uiLanguage}
				/>
			)}

			<div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] xl:gap-6">
				<section className="flex flex-col gap-2" aria-labelledby="played-games">
					<NightSectionTitle id="played-games">{isOver ? t("played.after") : t("played.tonight")}</NightSectionTitle>
					<PlayedSessionsList
						sessions={sessions}
						games={games}
						players={players}
						uiLanguage={settings.uiLanguage}
						canOpen={canWrite}
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
		</div>
	);
};
