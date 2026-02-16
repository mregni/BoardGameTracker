import { createFileRoute } from "@tanstack/react-router";
import { isPast } from "date-fns";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { getGameNights } from "@/services/queries/gameNights";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { FilterTabs, type FilterType } from "./-components/FilterTabs";
import { GameNightCard } from "./-components/GameNightCard";
import { NoGameNights } from "./-components/NoGameNights";
import { useGameNightActions } from "./-hooks/useGameNightActions";
import { useGameNightData } from "./-hooks/useGameNightData";
import { useGameNightModals } from "./-hooks/useGameNightModals";
import { CreateGameNightModal } from "./-modals/CreateGameNightModal";
import { EditGameNightModal } from "./-modals/EditGameNightModal";
import { ManageRSVPsModal } from "./-modals/ManageRSVPsModal";

export const Route = createFileRoute("/game-nights/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getGameNights());
		queryClient.prefetchQuery(getPlayers());
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getLocations());
	},
});

function RouteComponent() {
	const { t } = useTranslation();
	const [filter, setFilter] = useState<FilterType>("all");
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

	const upcomingCount = useMemo(() => gameNights.filter((gn) => !isPast(gn.startDate)).length, [gameNights]);
	const pastCount = useMemo(() => gameNights.filter((gn) => isPast(gn.startDate)).length, [gameNights]);

	const filteredGameNights = useMemo(() => {
		switch (filter) {
			case "upcoming":
				return gameNights.filter((gn) => !isPast(gn.startDate));
			case "past":
				return gameNights.filter((gn) => isPast(gn.startDate));
			default:
				return gameNights;
		}
	}, [gameNights, filter]);

	if (gameNights.length === 0) {
		return (
			<BgtEmptyPage
				header={t("game-nights.title")}
				icon={Calendar}
				title={t("game-nights.empty.title")}
				description={t("game-nights.empty.description")}
				action={{
					label: t("game-nights.create.button"),
					onClick: modals.createModal.show,
				}}
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
				header={t("game-nights.title")}
				actions={[
					{
						onClick: modals.createModal.show,
						variant: "primary",
						content: "game-nights.create.button",
					},
				]}
			/>
			<BgtPageContent isLoading={isLoading} data={{ settings, gameNights }}>
				{({ settings }) => (
					<>
						<FilterTabs
							filter={filter}
							onFilterChange={setFilter}
							allCount={gameNights.length}
							upcomingCount={upcomingCount}
							pastCount={pastCount}
						/>

						{filteredGameNights.length === 0 ? (
							<NoGameNights filter={filter} onCreateClick={modals.createModal.show} />
						) : (
							<div className="space-y-4">
								{filteredGameNights.map((gameNight) => (
									<GameNightCard
										key={gameNight.id}
										gameNight={gameNight}
										settings={settings}
										onEdit={actions.handleEditGameNight}
										onDelete={() => {
											actions.handleDeleteClick(gameNight);
											modals.deleteModal.show();
										}}
										onManageRsvps={actions.handleManageRsvps}
									/>
								))}
							</div>
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
							onUpdateRsvp={updateRsvp}
							isLoading={isLoading}
						/>

						<BgtDeleteModal
							open={modals.deleteModal.isOpen}
							close={() => {
								modals.deleteModal.hide();
								setSelectedGameNightId(null);
							}}
							onDelete={actions.handleDelete}
							title={t("game-nights.delete.title")}
							description={t("game-nights.delete.description", {
								title: selectedGameNight?.title,
							})}
						/>
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
