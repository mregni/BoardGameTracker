import { type AnyFieldApi, useSelector } from "@tanstack/react-form";
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
import { BgtDateTimePicker, BgtInputField } from "@/components/BgtForm";
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
import { SessionPlayerManager } from "@/routes/sessions/-components/SessionPlayerManager";
import { useSessionFormState } from "@/routes/sessions/-hooks/useSessionFormState";
import { sessionFormOpts } from "@/routes/sessions/-utils/sessionFormOpts";
import { formatMinutesToDuration } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";

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
		},
		onSubmit: async ({ value }) => {
			const session = CreateSessionSchema.parse({
				...value,
				playerSessions: value.playerSessions.map((x) => (hasScoring ? x : { ...x, score: null })),
				expansionIds: [],
				gameNightId: gameNight.id,
			});
			await onSave(session);
			close();
		},
	});

	const selectedGameId = useSelector(form.store, (state) => state.values.gameId);
	const { hasScoring, addPlayer, removePlayer, setScore, toggleWon, toggleFirstPlay } = useSessionFormState({
		selectedGameId,
		games,
		initialPlayers,
		setPlayers: (updater) => form.setFieldValue("playerSessions", updater),
	});

	const runningGameTitle = games.find((x) => x.id === runningGame?.gameId)?.title ?? "";

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
			<BgtDialogContent size="sheet" className="lg:max-w-4xl">
				<form
					onSubmit={handleFormSubmit(form)}
					className="flex flex-col gap-3 lg:grid lg:grid-cols-2 lg:grid-rows-[auto_1fr_auto] lg:gap-x-6"
				>
					<div className="flex flex-col gap-0.5 pr-6 lg:col-start-1 lg:row-start-1">
						<BgtDialogTitle>
							{runningGame ? t("play.finish.title", { game: runningGameTitle }) : t("play.log.title")}
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

					<div className="min-w-0 lg:col-start-1 lg:row-start-2">
						{runningGame ? (
							<div className="grid grid-cols-[minmax(0,1fr)_7rem] gap-3">
								<form.Field name="start" validators={zodValidator(CreateSessionSchema, "start")}>
									{(field: AnyFieldApi) => (
										<BgtDateTimePicker field={field} disabled={isPending} label={t("player-session:new.start.label")} />
									)}
								</form.Field>
								<form.Field name="minutes" validators={zodValidator(CreateSessionSchema, "minutes")}>
									{(field: AnyFieldApi) => (
										<BgtInputField
											field={field}
											type="number"
											inputMode="numeric"
											disabled={isPending}
											label={t("player-session:new.duration.label")}
											suffixLabel={t("common:minutes-abbreviation")}
										/>
									)}
								</form.Field>
							</div>
						) : (
							<SessionFormFields
								form={form}
								games={games}
								recentGameIds={nightGameIds}
								recentGamesLabel={t("play.games.tonight")}
								locations={locations}
								disabled={isPending}
								onGameChange={handleGameChange}
								onMinutesChange={syncStartWithDuration}
							/>
						)}
					</div>

					<div className="min-w-0 lg:col-start-2 lg:row-span-2 lg:row-start-1">
						<SessionPlayerManager
							form={form}
							playerList={players}
							frequentPlayerIds={attendeeIds}
							hasScoring={hasScoring}
							disabled={isPending}
							onAddPlayer={addPlayer}
							onRemovePlayer={removePlayer}
							onScoreChange={setScore}
							onToggleWon={toggleWon}
							onToggleFirstPlay={toggleFirstPlay}
						/>
					</div>

					<BgtDialogFooter
						className="lg:col-span-2 lg:row-start-3"
						onCancel={close}
						confirmLabel={t("play.save")}
						confirmType="submit"
						loading={isPending}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
