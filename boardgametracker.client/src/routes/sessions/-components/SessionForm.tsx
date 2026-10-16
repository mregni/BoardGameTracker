import { useSelector } from "@tanstack/react-form";
import { addMinutes } from "date-fns";
import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtDialogFooter } from "@/components/BgtDialog";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { useAppForm } from "@/hooks/form";
import {
	type CreateSession,
	type CreateSessionPlayer,
	CreateSessionSchema,
	type Expansion,
	type Game,
	type PlayerSession,
} from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { useBack } from "../../-hooks/useBack";
import { useSessionForm } from "../-hooks/useSessionForm";
import { useSessionFormState } from "../-hooks/useSessionFormState";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { SessionFormFields } from "./SessionFormFields";

interface Props {
	game?: Game;
	locationId?: number | null;
	minutes?: number;
	comment?: string | null;
	start?: Date;
	expansions?: Expansion[];
	playerSessions?: Pick<PlayerSession, "playerId" | "won" | "firstPlay" | "score">[];
	onClick: (data: CreateSession) => Promise<void>;
	buttonText: string;
	title: string;
	backTo: string;
	disabled: boolean;
}

export const SessionForm = (props: Props) => {
	const {
		game,
		locationId,
		minutes,
		comment = "",
		start,
		playerSessions = [],
		expansions = [],
		onClick,
		buttonText,
		title,
		backTo,
		disabled,
	} = props;

	const { t } = useTranslation();
	const goBack = useBack(backTo);
	const { locations, games, players: playerList, suggestions } = useSessionForm();
	const isNewSession = locationId === undefined;
	const [initialStart] = useState(() => start ?? addMinutes(new Date(), -(minutes ?? 30)));
	const [initialPlayers] = useState<CreateSessionPlayer[]>(() =>
		playerSessions.map(({ playerId, won, firstPlay, score }) => ({ playerId, won, firstPlay, score: score ?? null })),
	);

	const form = useAppForm({
		...sessionFormOpts,
		defaultValues: {
			...sessionFormOpts.defaultValues,
			gameId: game?.id ?? 0,
			locationId: locationId ?? 0,
			minutes: minutes ?? 30,
			comment: comment ?? "",
			start: initialStart,
			playerSessions: initialPlayers,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreateSessionSchema.parse({
				...value,
				playerSessions: value.playerSessions.map((x) => (sessionState.hasScoring ? x : { ...x, score: null })),
				expansionIds: sessionState.selectedExpansionIds,
			});
			await onClick(validatedData);
		},
	});

	const syncStartWithDuration = useCallback(
		(duration: number | undefined) => {
			if (!isNewSession || typeof duration !== "number" || duration <= 0) return;
			if (form.getFieldMeta("start")?.isDirty) return;
			form.setFieldValue("start", addMinutes(new Date(), -duration), { dontUpdateMeta: true });
		},
		[form, isNewSession],
	);

	const handleGameChange = useCallback(
		(gameId: number) => {
			const selected = games.find((x) => x.id === gameId);
			if (selected) form.setFieldValue("minutes", selected.maxPlayTime ?? 30);
		},
		[form, games],
	);

	const selectedGameId = useSelector(form.store, (state) => state.values.gameId);

	const sessionState = useSessionFormState({
		selectedGameId,
		games,
		initialGame: game,
		initialExpansions: expansions,
		initialPlayers,
		setPlayers: (updater) => form.setFieldValue("playerSessions", updater),
	});

	const lastLocationId = suggestions?.lastLocationId ?? null;

	useEffect(() => {
		if (!isNewSession || lastLocationId === null) return;
		if (form.getFieldValue("locationId") !== 0 || form.getFieldMeta("locationId")?.isDirty) return;
		if (!locations.some((x) => x.id === lastLocationId)) return;
		form.setFieldValue("locationId", lastLocationId, { dontUpdateMeta: true });
	}, [form, isNewSession, lastLocationId, locations]);

	return (
		<BgtPage>
			<BgtPageHeader backAction={goBack} backText={t("back")} header={title} />
			<BgtPageContent className="items-center">
				<BgtCard className="w-full max-w-3xl lg:max-w-5xl">
					<form onSubmit={handleFormSubmit(form)} className="flex w-full flex-col gap-4">
						<SessionFormFields
							form={form}
							games={games}
							recentGameIds={suggestions?.recentGameIds ?? []}
							recentGamesLabel={t("player-session:form.game.recent")}
							locations={locations}
							playerList={playerList}
							frequentPlayerIds={suggestions?.frequentPlayerIds ?? []}
							fixedGame={null}
							state={sessionState}
							disabled={disabled}
							onGameChange={handleGameChange}
							onMinutesChange={syncStartWithDuration}
						/>
						<div className="sticky bottom-20 z-10 -mx-4 bg-background/95 px-4 py-3 backdrop-blur-sm md:static md:mx-0 md:bg-transparent md:p-0 md:backdrop-blur-none">
							<BgtDialogFooter
								onCancel={goBack}
								cancelDisabled={disabled}
								confirmLabel={buttonText}
								confirmType="submit"
								loading={disabled}
							/>
						</div>
					</form>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
};
