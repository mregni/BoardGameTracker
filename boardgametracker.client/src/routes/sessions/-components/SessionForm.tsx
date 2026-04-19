import { useRouter } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCenteredCard } from "@/components/BgtCard/BgtCenteredCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { useAppForm } from "@/hooks/form";
import {
	type CreatePlayerSessionNoScoring,
	type CreateSession,
	type CreateSessionPlayer,
	CreateSessionSchema,
	type Expansion,
	type Game,
} from "@/models";

import { handleFormSubmit } from "@/utils/formUtils";
import { useSessionForm } from "../-hooks/useSessionForm";
import { useSessionFormState } from "../-hooks/useSessionFormState";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { SessionExpansionSelector } from "./SessionExpansionSelector";
import { SessionFormFields } from "./SessionFormFields";
import { SessionPlayerManager } from "./SessionPlayerManager";
import { BgtCard } from "@/components/BgtCard/BgtCard";

interface Props {
	game?: Game | undefined;
	locationId?: number | undefined;
	minutes?: number | undefined;
	comment?: string | null;
	start?: Date | undefined;
	expansions?: Expansion[] | undefined;
	playerSessions?: CreateSessionPlayer[] | CreatePlayerSessionNoScoring[] | undefined;
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
	const { locations, games, players: playerList } = useSessionForm();

	const form = useAppForm({
		...sessionFormOpts,
		defaultValues: {
			...sessionFormOpts.defaultValues,
			gameId: game?.id ?? 0,
			locationId: locationId ?? 0,
			minutes: minutes ?? 30,
			comment: comment ?? "",
			start: start ?? new Date(),
			playerSessions: playerSessions,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreateSessionSchema.parse({
				...value,
				playerSessions: players,
				expansionIds: selectedExpansionIds,
			});
			await onClick(validatedData);
		},
	});

	const {
		selectedGameId,
		expansionList,
		selectedExpansionIds,
		setSelectedExpansionIds,
		players,
		addPlayer,
		updatePlayer,
		removePlayer,
		openEditPlayerModal,
		isCreateModalOpen,
		isUpdateModalOpen,
		openCreateModal,
		closeModal,
		playerIdToEdit,
	} = useSessionFormState({
		form,
		games,
		initialGameId: game?.id,
		initialExpansions: expansions,
		initialPlayerSessions: playerSessions,
	});

	const handleCancel = useCallback(() => {
		router.history.back();
	}, [router]);

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCard title={title}>
					<form onSubmit={handleFormSubmit(form)} className="w-full">
						<div className="flex flex-col gap-3 w-full">
							<SessionFormFields form={form} games={games} locations={locations} disabled={disabled} />

							<SessionExpansionSelector
								expansionList={expansionList}
								selectedIds={selectedExpansionIds}
								selectedGameId={selectedGameId}
								disabled={disabled}
								onSelectionChange={setSelectedExpansionIds}
							/>

							<SessionPlayerManager
								form={form}
								players={players}
								playerList={playerList}
								hasScoring={game?.hasScoring ?? true}
								disabled={disabled}
								isCreateModalOpen={isCreateModalOpen}
								isUpdateModalOpen={isUpdateModalOpen}
								playerIdToEdit={playerIdToEdit}
								onOpenCreateModal={openCreateModal}
								onCloseModal={closeModal}
								onEditPlayer={openEditPlayerModal}
								onAddPlayer={addPlayer}
								onUpdatePlayer={updatePlayer}
								onRemovePlayer={removePlayer}
							/>

							<div className="flex flex-row gap-2 mt-2">
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
