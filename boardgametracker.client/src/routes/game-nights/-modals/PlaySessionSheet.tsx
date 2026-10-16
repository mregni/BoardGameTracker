import { useSelector } from "@tanstack/react-form";
import { noop } from "@tanstack/react-query";
import { differenceInMinutes, format, startOfMinute, subMinutes } from "date-fns";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { useAppForm } from "@/hooks/form";
import {
	type CreateSession,
	type CreateSessionPlayer,
	CreateSessionSchema,
	type Game,
	type GameNight,
	type Location,
	type Player,
	type RunningGame,
	type Settings,
} from "@/models";
import { SessionFormFields } from "@/routes/sessions/-components/SessionFormFields";
import { useSessionFormState } from "@/routes/sessions/-hooks/useSessionFormState";
import { applyPlayMode, coopDefaults } from "@/routes/sessions/-utils/coopSession";
import { sessionFormOpts } from "@/routes/sessions/-utils/sessionFormOpts";
import { formatMinutesToDuration } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";

const DEFAULT_MINUTES = 30;

const endingNow = (minutes: number) => subMinutes(startOfMinute(new Date()), minutes);

interface Props {
	close: () => void;
	gameNight: GameNight;
	runningGame: RunningGame | null;
	games: Game[];
	players: Player[];
	locations: Location[];
	nightGameIds: number[];
	attendeeIds: number[];
	settings: Settings;
	isPending: boolean;
	onSave: (session: CreateSession) => Promise<unknown>;
}

const toPlayers = (playerIds: number[]): CreateSessionPlayer[] =>
	playerIds.map((playerId) => ({ playerId, won: false, firstPlay: false, score: null }));

export const PlaySessionSheet = (props: Props) => {
	const {
		close,
		gameNight,
		runningGame,
		games,
		players,
		locations,
		nightGameIds,
		attendeeIds,
		settings,
		isPending,
		onSave,
	} = props;
	const { t } = useTranslation(["game-nights", "player-session", "common"]);
	const [initialPlayers] = useState(() => toPlayers(runningGame?.playerIds ?? attendeeIds));
	const [defaults] = useState(() => {
		const gameId = runningGame?.gameId ?? 0;
		if (runningGame) {
			const start = new Date(runningGame.start);
			return { gameId, start, minutes: Math.max(1, differenceInMinutes(new Date(), start)) };
		}
		const minutes = games.find((x) => x.id === gameId)?.maxPlayTime ?? DEFAULT_MINUTES;
		return { gameId, start: endingNow(minutes), minutes };
	});

	const form = useAppForm({
		...sessionFormOpts,
		defaultValues: {
			...sessionFormOpts.defaultValues,
			...defaults,
			locationId: gameNight.locationId,
			playerSessions: initialPlayers,
			...coopDefaults(games.find((x) => x.id === defaults.gameId)),
		},
		onSubmit: async ({ value }) => {
			const session = CreateSessionSchema.parse({
				...applyPlayMode(value, sessionState.hasScoring),
				expansionIds: sessionState.selectedExpansionIds,
				gameNightId: gameNight.id,
			});
			await onSave(session);
			close();
		},
	});

	const selectedGameId = useSelector(form.store, (state) => state.values.gameId);
	const sessionState = useSessionFormState({
		selectedGameId,
		games,
		initialPlayers,
		setPlayers: (updater) => form.setFieldValue("playerSessions", updater),
	});

	const runningGameInfo = games.find((x) => x.id === runningGame?.gameId);
	const finishedGame = runningGame
		? { title: runningGameInfo?.title ?? "", image: runningGameInfo?.image ?? null }
		: null;

	const syncStartWithDuration = (minutes: number | undefined) => {
		if (typeof minutes !== "number" || minutes <= 0) return;
		if (form.getFieldMeta("start")?.isDirty) return;
		form.setFieldValue("start", endingNow(minutes), { dontUpdateMeta: true });
	};

	const handleGameChange = (gameId: number) => {
		const selected = games.find((x) => x.id === gameId);
		if (!selected) return;
		const minutes = selected.maxPlayTime ?? DEFAULT_MINUTES;
		form.setFieldValue("minutes", minutes);
		syncStartWithDuration(minutes);
	};

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent size="sheet" className="lg:max-w-5xl">
				<form onSubmit={handleFormSubmit(form)} className="flex flex-col gap-3">
					<div className="flex flex-col gap-0.5 pr-6">
						<BgtDialogTitle>
							{runningGame ? t("play.finish.title", { game: finishedGame?.title ?? "" }) : t("play.log.title")}
						</BgtDialogTitle>
						{runningGame && (
							<BgtDialogDescription className="text-sm">
								{t("play.finish.subtitle", {
									time: format(defaults.start, settings.timeFormat),
									duration: formatMinutesToDuration(defaults.minutes, ["hours", "minutes"], settings.uiLanguage),
									location: gameNight.location.name,
								})}
							</BgtDialogDescription>
						)}
					</div>

					<SessionFormFields
						form={form}
						games={games}
						recentGameIds={nightGameIds}
						recentGamesLabel={t("play.games.tonight")}
						fixedGame={finishedGame}
						locations={locations}
						playerList={players}
						frequentPlayerIds={attendeeIds}
						state={sessionState}
						disabled={isPending}
						onGameChange={handleGameChange}
						onMinutesChange={runningGame ? noop : syncStartWithDuration}
					/>

					<BgtDialogFooter onCancel={close} confirmLabel={t("play.save")} confirmType="submit" loading={isPending} />
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
