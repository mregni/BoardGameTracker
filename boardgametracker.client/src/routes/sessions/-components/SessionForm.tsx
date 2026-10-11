import type { AnyFieldApi } from "@tanstack/react-form";
import { useRouter } from "@tanstack/react-router";
import { addMinutes } from "date-fns";
import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtTextArea } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { useAppForm } from "@/hooks/form";
import { type CreateSession, CreateSessionSchema, type Expansion, type Game, type Player } from "@/models";
import { usePlayerModal } from "@/routes/players/-hooks/usePlayerModal";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useSessionForm } from "../-hooks/useSessionForm";
import { useSessionFormState } from "../-hooks/useSessionFormState";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { SessionExpansionSelector } from "./SessionExpansionSelector";
import { SessionFormFields } from "./SessionFormFields";
import { SessionPlayerManager } from "./SessionPlayerManager";

interface InitialPlayerSession {
	playerId: number;
	won: boolean;
	firstPlay: boolean;
	score?: number | null;
}

interface Props {
	game?: Game | undefined;
	locationId?: number | null;
	minutes?: number | undefined;
	comment?: string | null;
	start?: Date | undefined;
	expansions?: Expansion[] | undefined;
	playerSessions?: InitialPlayerSession[] | undefined;
	onClick: (data: CreateSession) => Promise<void>;
	buttonText: string;
	title: string;
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
		disabled,
	} = props;

	const { t } = useTranslation();
	const router = useRouter();
	const { locations, games, players: playerList, suggestions } = useSessionForm();
	const { savePlayer } = usePlayerModal({});
	const isNewSession = locationId === undefined;
	const [initialStart] = useState(() => start ?? addMinutes(new Date(), -(minutes ?? 30)));

	const form = useAppForm({
		...sessionFormOpts,
		defaultValues: {
			...sessionFormOpts.defaultValues,
			gameId: game?.id ?? 0,
			locationId: locationId ?? 0,
			minutes: minutes ?? 30,
			comment: comment ?? "",
			start: initialStart,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreateSessionSchema.parse({
				...value,
				playerSessions: players.map((x) => (hasScoring ? x : { ...x, score: null })),
				expansionIds: selectedExpansionIds,
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

	const {
		selectedGameId,
		hasScoring,
		expansionList,
		selectedExpansionIds,
		setSelectedExpansionIds,
		players,
		addPlayer,
		removePlayer,
		setScore,
		toggleWon,
		toggleFirstPlay,
	} = useSessionFormState({
		form,
		games,
		initialGame: game,
		initialExpansions: expansions,
		initialPlayerSessions: playerSessions,
		onDurationDefaulted: syncStartWithDuration,
	});

	const lastLocationId = suggestions?.lastLocationId ?? null;

	useEffect(() => {
		if (!isNewSession || lastLocationId === null) return;
		if (form.getFieldValue("locationId") !== 0 || form.getFieldMeta("locationId")?.isDirty) return;
		if (!locations.some((x) => x.id === lastLocationId)) return;
		form.setFieldValue("locationId", lastLocationId, { dontUpdateMeta: true });
	}, [form, isNewSession, lastLocationId, locations]);

	const createPlayer = useCallback(
		async (name: string): Promise<Player | undefined> => {
			try {
				return await savePlayer({ id: 0, name, image: null, email: null, badges: [] });
			} catch {
				return undefined;
			}
		},
		[savePlayer],
	);

	const handleCancel = useCallback(() => {
		router.history.back();
	}, [router]);

	return (
		<BgtPage>
			<BgtPageContent className="flex-1 items-center">
				<BgtCard title={title} className="w-full max-w-3xl my-auto">
					<form onSubmit={handleFormSubmit(form)} className="w-full">
						<div className="flex flex-col gap-4 w-full">
							<SessionFormFields
								form={form}
								games={games}
								recentGameIds={suggestions?.recentGameIds ?? []}
								locations={locations}
								disabled={disabled}
								onMinutesChange={syncStartWithDuration}
							/>

							<SessionPlayerManager
								form={form}
								players={players}
								playerList={playerList}
								frequentPlayerIds={suggestions?.frequentPlayerIds ?? []}
								hasScoring={hasScoring}
								disabled={disabled}
								onAddPlayer={addPlayer}
								onCreatePlayer={createPlayer}
								onRemovePlayer={removePlayer}
								onScoreChange={setScore}
								onToggleWon={toggleWon}
								onToggleFirstPlay={toggleFirstPlay}
							/>

							<SessionExpansionSelector
								expansionList={expansionList}
								selectedIds={selectedExpansionIds}
								selectedGameId={selectedGameId}
								disabled={disabled}
								onSelectionChange={setSelectedExpansionIds}
							/>

							<form.Field name="comment" validators={zodValidator(CreateSessionSchema, "comment")}>
								{(field: AnyFieldApi) => (
									<BgtTextArea field={field} disabled={disabled} label={t("player-session:new.comment.label")} />
								)}
							</form.Field>

							<div className="sticky bottom-20 z-10 -mx-4 px-4 py-3 bg-background/95 backdrop-blur-sm flex flex-row gap-2 md:static md:mx-0 md:px-0 md:py-0 md:mt-2 md:bg-transparent md:backdrop-blur-none">
								<BgtButton
									variant="cancel"
									type="button"
									disabled={disabled}
									className="flex-none"
									onClick={handleCancel}
								>
									{t("cancel")}
								</BgtButton>
								<BgtButton type="submit" disabled={disabled} className="flex-1" variant="primary">
									{disabled && <Bars className="size-4" />}
									{buttonText}
								</BgtButton>
							</div>
						</div>
					</form>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
};
