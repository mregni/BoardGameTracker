import { createFileRoute, redirect } from "@tanstack/react-router";
import { isPast } from "date-fns";
import { type ReactNode, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { usePermissions } from "@/hooks/usePermissions";
import type { GameNight } from "@/models";
import { getGameNights } from "@/services/queries/gameNights";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { GameNightCard } from "./-components/GameNightCard";
import { PastGameNightItem } from "./-components/PastGameNightItem";
import { useGameNightActions } from "./-hooks/useGameNightActions";
import { useGameNightData } from "./-hooks/useGameNightData";
import { useGameNightModals } from "./-hooks/useGameNightModals";
import { CreateGameNightModal } from "./-modals/CreateGameNightModal";
import { EditGameNightModal } from "./-modals/EditGameNightModal";
import { ManageRSVPsModal } from "./-modals/ManageRSVPsModal";

export const Route = createFileRoute("/game-nights/")({
	component: RouteComponent,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.ensureQueryData(getSettings());
		if (!settings.gameNightsEnabled) {
			throw redirect({ to: "/" });
		}
	},
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getGameNights());
		queryClient.prefetchQuery(getPlayers());
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getLocations());
	},
});

function RouteComponent() {
	const { t } = useTranslation("game-nights");
	const { canWrite } = usePermissions();
	const [selectedGameNightId, setSelectedGameNightId] = useState<number | null>(null);

	const {
		gameNights,
		settings,
		players,
		games,
		locations,
		isLoading,
		createGameNight,
		deleteGameNight,
		updateGameNight,
		updateInvitedPlayers,
		updateRsvp,
		isCreating,
		isUpdating,
	} = useGameNightData();

	const modals = useGameNightModals();

	const selectedGameNight = useMemo(
		() => gameNights.find((gn) => gn.id === selectedGameNightId) ?? null,
		[gameNights, selectedGameNightId],
	);

	const actions = useGameNightActions({
		createGameNight,
		deleteGameNight,
		selectedGameNightId,
		onCreateModalClose: modals.createModal.hide,
		onEditModalOpen: modals.editModal.show,
		onDeleteModalClose: modals.deleteModal.hide,
		onManageRsvpModalOpen: modals.manageRsvpModal.show,
		setSelectedGameNightId,
	});

	const upcomingGameNights = useMemo(
		() =>
			gameNights
				.filter((gn) => !isPast(gn.startDate))
				.sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime()),
		[gameNights],
	);

	const pastGameNights = useMemo(
		() =>
			gameNights
				.filter((gn) => isPast(gn.startDate))
				.sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime()),
		[gameNights],
	);

	const handleDeleteClick = (gameNight: GameNight) => {
		actions.handleDeleteClick(gameNight);
		modals.deleteModal.show();
	};

	if (!isLoading && gameNights.length === 0) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={Calendar}
				title={t("empty.title")}
				description={t("empty.description")}
				action={canWrite ? { label: t("create.button"), onClick: modals.createModal.show } : undefined}
			>
				<CreateGameNightModal
					open={modals.createModal.isOpen}
					close={modals.createModal.hide}
					players={players}
					games={games}
					locations={locations}
					isLoading={isCreating}
					onSave={actions.handleCreate}
				/>
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				icon={Calendar}
				header={t("title")}
				actions={
					canWrite
						? [
								{
									onClick: modals.createModal.show,
									variant: "primary",
									content: "game-nights:create.button",
								},
							]
						: []
				}
			/>
			<BgtPageContent isLoading={isLoading} data={{ settings, gameNights }}>
				{({ settings }) => (
					<>
						<section className="flex flex-col gap-3" aria-labelledby="upcoming-game-nights">
							<SectionTitle id="upcoming-game-nights">{t("upcoming")}</SectionTitle>
							{upcomingGameNights.length === 0 ? (
								<BgtText size="2" color="gray">
									{t("empty.upcoming")}
								</BgtText>
							) : (
								upcomingGameNights.map((gameNight, index) => (
									<GameNightCard
										key={gameNight.id}
										gameNight={gameNight}
										settings={settings}
										canWrite={canWrite}
										highlight={index === 0}
										onEdit={actions.handleEditGameNight}
										onDelete={handleDeleteClick}
										onManageRsvps={actions.handleManageRsvps}
									/>
								))
							)}
						</section>

						{pastGameNights.length > 0 && (
							<section className="flex flex-col gap-2" aria-labelledby="past-game-nights">
								<SectionTitle id="past-game-nights">{t("past")}</SectionTitle>
								{pastGameNights.map((gameNight) => (
									<PastGameNightItem
										key={gameNight.id}
										gameNight={gameNight}
										settings={settings}
										canWrite={canWrite}
										onEdit={actions.handleEditGameNight}
										onDelete={handleDeleteClick}
									/>
								))}
							</section>
						)}

						<CreateGameNightModal
							open={modals.createModal.isOpen}
							close={modals.createModal.hide}
							players={players}
							games={games}
							locations={locations}
							isLoading={isCreating}
							onSave={actions.handleCreate}
						/>

						<EditGameNightModal
							open={modals.editModal.isOpen}
							close={() => {
								modals.editModal.hide();
								setSelectedGameNightId(null);
							}}
							gameNight={selectedGameNight}
							players={players}
							games={games}
							locations={locations}
							isLoading={isUpdating}
							onSave={updateGameNight}
						/>

						<ManageRSVPsModal
							open={modals.manageRsvpModal.isOpen}
							close={() => {
								modals.manageRsvpModal.hide();
								setSelectedGameNightId(null);
							}}
							gameNight={selectedGameNight}
							players={players}
							settings={settings}
							onUpdateRsvp={updateRsvp}
							onUpdateGameNight={updateInvitedPlayers}
							isLoading={isUpdating}
						/>

						<BgtDeleteModal
							open={modals.deleteModal.isOpen}
							close={() => {
								modals.deleteModal.hide();
								setSelectedGameNightId(null);
							}}
							onDelete={actions.handleDelete}
							title={selectedGameNight?.title ?? ""}
							heading={t("delete.title")}
							description={t("delete.description", {
								title: selectedGameNight?.title,
							})}
						/>
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}

const SectionTitle = ({ id, children }: { id: string; children: ReactNode }) => (
	<h2
		id={id}
		className="flex items-center gap-3 text-xs tracking-widest text-gray-400 uppercase after:h-px after:flex-1 after:bg-white/10"
	>
		{children}
	</h2>
);
