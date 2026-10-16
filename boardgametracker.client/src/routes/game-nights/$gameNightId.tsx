import { noop } from "@tanstack/react-query";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { useModalState } from "@/hooks/useModalState";
import { usePermissions } from "@/hooks/usePermissions";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { CreateSession, RunningGame } from "@/models";
import { useNewSessionData } from "@/routes/sessions/-hooks/useNewSessionData";
import { getGameNight } from "@/services/queries/gameNights";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { gameNightIdParamSchema } from "@/utils/routeSchemas";
import { useBack } from "../-hooks/useBack";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { GameNightDetailView } from "./-components/GameNightDetailView";
import { useGameNightData } from "./-hooks/useGameNightData";
import { useGameNightDetail } from "./-hooks/useGameNightDetail";
import { useGameNightModals } from "./-hooks/useGameNightModals";
import { EditGameNightModal } from "./-modals/EditGameNightModal";
import { EndNightModal } from "./-modals/EndNightModal";
import { ManageRSVPsModal } from "./-modals/ManageRSVPsModal";
import { PlaySessionSheet } from "./-modals/PlaySessionSheet";
import { StartGameSheet } from "./-modals/StartGameSheet";
import { attendeeIds, nightGameIds } from "./-utils/gameNightPlay";

export const Route = createFileRoute("/game-nights/$gameNightId")({
	component: RouteComponent,
	params: gameNightIdParamSchema,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.query({ ...getSettings(), staleTime: "static" });
		if (!settings.gameNightsEnabled) {
			throw redirect({ to: "/" });
		}
	},
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getGameNight(params.gameNightId)).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getLocations()).catch(noop);
	},
});

type Sheet =
	| { kind: "start"; gameId: number | null; playerIds: number[] }
	| { kind: "finish"; runningGame: RunningGame }
	| { kind: "log" };

function RouteComponent() {
	const { gameNightId } = Route.useParams();
	const { t } = useTranslation("game-nights");
	const { canWrite } = usePermissions();
	const navigate = useNavigate();
	const goBack = useBack("/game-nights");
	const invalidator = useQueryInvalidator();
	const modals = useGameNightModals();
	const [sheet, setSheet] = useState<Sheet | null>(null);
	const [discarding, setDiscarding] = useState<RunningGame | null>(null);

	const {
		settings,
		players,
		games,
		locations,
		isLoading,
		updateGameNight,
		updateInvitedPlayers,
		updateRsvp,
		deleteGameNight,
		isUpdating,
	} = useGameNightData();
	const {
		detail,
		isLoading: isDetailLoading,
		startGame,
		finishGame,
		discardGame,
		endNight,
		reopenNight,
		isPending,
	} = useGameNightDetail(gameNightId);
	const endModal = useModalState();
	const { saveSession, isPending: isSaving } = useNewSessionData({
		onSuccess: () => invalidator.invalidateGameNights(),
	});

	const closeSheet = () => setSheet(null);

	const handleDelete = async () => {
		await deleteGameNight(gameNightId);
		modals.deleteModal.hide();
		navigate({ to: "/game-nights" });
	};

	const handleDiscard = async () => {
		if (discarding) await discardGame(discarding.id);
		setDiscarding(null);
	};

	return (
		<BgtPage>
			<BgtPageHeader backAction={goBack} backText={t("detail.back")} />
			<BgtPageContent isLoading={isLoading || isDetailLoading} data={{ detail, settings }}>
				{({ detail, settings }) => {
					const attendees = attendeeIds(detail.gameNight);
					const gameIds = nightGameIds(detail);

					const saveSheet = (session: CreateSession) =>
						sheet?.kind === "finish"
							? finishGame({
									id: sheet.runningGame.id,
									command: {
										playerSessions: session.playerSessions,
										start: session.start,
										minutes: session.minutes,
										locationId: session.locationId,
										comment: session.comment,
										expansionIds: session.expansionIds ?? [],
										isCoop: session.isCoop,
										teamWon: session.teamWon,
										teamScore: session.teamScore,
									},
								})
							: saveSession(session);

					return (
						<>
							<GameNightDetailView
								detail={detail}
								settings={settings}
								games={games}
								players={players}
								canWrite={canWrite}
								onEdit={modals.editModal.show}
								onDelete={modals.deleteModal.show}
								onEnd={endModal.show}
								onReopen={() => reopenNight()}
								onEditAttendees={modals.manageRsvpModal.show}
								onStart={(gameId, playerIds) => setSheet({ kind: "start", gameId, playerIds })}
								onLog={() => setSheet({ kind: "log" })}
								onFinish={(runningGame) => setSheet({ kind: "finish", runningGame })}
								onDiscard={setDiscarding}
							/>

							{sheet?.kind === "start" && (
								<StartGameSheet
									close={closeSheet}
									games={games}
									players={players}
									nightGameIds={gameIds}
									attendeeIds={attendees}
									initialGameId={sheet.gameId}
									initialPlayerIds={sheet.playerIds}
									isPending={isPending}
									onStart={startGame}
								/>
							)}

							{(sheet?.kind === "finish" || sheet?.kind === "log") && (
								<PlaySessionSheet
									close={closeSheet}
									gameNight={detail.gameNight}
									runningGame={sheet.kind === "finish" ? sheet.runningGame : null}
									games={games}
									players={players}
									locations={locations}
									nightGameIds={gameIds}
									attendeeIds={attendees}
									settings={settings}
									isPending={isPending || isSaving}
									onSave={saveSheet}
								/>
							)}

							<EditGameNightModal
								open={modals.editModal.isOpen}
								close={modals.editModal.hide}
								gameNight={detail.gameNight}
								players={players}
								games={games}
								locations={locations}
								isLoading={isUpdating}
								onSave={updateGameNight}
							/>

							<ManageRSVPsModal
								open={modals.manageRsvpModal.isOpen}
								close={modals.manageRsvpModal.hide}
								gameNight={detail.gameNight}
								players={players}
								settings={settings}
								onUpdateRsvp={updateRsvp}
								onUpdateGameNight={updateInvitedPlayers}
								isLoading={isUpdating}
							/>

							<EndNightModal open={endModal.isOpen} close={endModal.hide} isPending={isPending} onConfirm={endNight} />

							<BgtDeleteModal
								open={modals.deleteModal.isOpen}
								close={modals.deleteModal.hide}
								onDelete={handleDelete}
								title={detail.gameNight.title}
								heading={t("delete.title")}
								description={t("delete.description", { title: detail.gameNight.title })}
							/>

							<BgtDeleteModal
								open={discarding !== null}
								close={() => setDiscarding(null)}
								onDelete={handleDiscard}
								title={detail.gameNight.title}
								heading={t("play.discard.title")}
								description={t("play.discard.description")}
							/>
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
