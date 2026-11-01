import { noop } from "@tanstack/react-query";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import Calendar from "@/assets/icons/calendar.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { BgtText } from "@/components/BgtText/BgtText";
import { usePermissions } from "@/hooks/usePermissions";
import { type GameNight, GameNightStatisticsPeriod } from "@/models";
import { getGameNights } from "@/services/queries/gameNights";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { GameNightCard } from "./-components/GameNightCard";
import { GameNightListSkeleton } from "./-components/GameNightListSkeleton";
import { NightSectionTitle } from "./-components/NightSectionTitle";
import { PastGameNightList } from "./-components/PastGameNightList";
import { GameNightStatsView } from "./-components/stats/GameNightStatsView";
import { useGameNightActions } from "./-hooks/useGameNightActions";
import { useGameNightData } from "./-hooks/useGameNightData";
import { useGameNightModals } from "./-hooks/useGameNightModals";
import { CreateGameNightModal } from "./-modals/CreateGameNightModal";
import { EditGameNightModal } from "./-modals/EditGameNightModal";
import { ManageRSVPsModal } from "./-modals/ManageRSVPsModal";
import { deleteNightDescription } from "./-utils/deleteNightDescription";
import { splitGameNights } from "./-utils/gameNightPlay";
import { inviteState } from "./-utils/inviteRecipients";

const gameNightsSearchSchema = z.object({
	view: z.enum(["nights", "stats"]).optional(),
	period: z.enum(GameNightStatisticsPeriod).optional(),
});

type GameNightsView = "nights" | "stats";

export const Route = createFileRoute("/game-nights/")({
	component: RouteComponent,
	validateSearch: gameNightsSearchSchema,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.query({ ...getSettings(), staleTime: "static" });
		if (!settings.gameNightsEnabled) {
			throw redirect({ to: "/" });
		}
	},
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getGameNights()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getLocations()).catch(noop);
	},
});

function RouteComponent() {
	const { t } = useTranslation("game-nights");
	const { canWrite, isAdmin } = usePermissions();
	const navigate = useNavigate();
	const search = Route.useSearch();
	const view: GameNightsView = search.view ?? "nights";
	const period = search.period ?? GameNightStatisticsPeriod.AllTime;
	const [selectedGameNightId, setSelectedGameNightId] = useState<number | null>(null);

	const {
		gameNights,
		settings,
		players,
		games,
		locations,
		isLoading,
		loadFailed,
		retryLoad,
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

	const { blocker, askAdmin } = inviteState(settings, isAdmin);

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

	const {
		live: liveGameNights,
		upcoming: upcomingGameNights,
		past: pastGameNights,
	} = useMemo(() => splitGameNights(gameNights), [gameNights]);

	const changeSearch = (next: { view?: GameNightsView; period?: GameNightStatisticsPeriod }) =>
		navigate({
			to: "/game-nights",
			search: (previous) => ({ ...previous, ...next }),
			replace: true,
		});

	const handleDeleteClick = (gameNight: GameNight) => {
		actions.handleDeleteClick(gameNight);
		modals.deleteModal.show();
	};

	if (!isLoading && loadFailed) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={Calendar}
				title={t("load-error.title")}
				description={t("load-error.description")}
				action={{ label: t("load-error.retry"), onClick: () => void retryLoad() }}
			/>
		);
	}

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
									icon: PlusIcon,
								},
							]
						: []
				}
			>
				<BgtSegmentedControl
					aria-label={t("view.label")}
					size="sm"
					value={view}
					onChange={(value) => changeSearch({ view: value })}
					options={[
						{ value: "nights", label: t("view.nights") },
						{ value: "stats", label: t("view.stats") },
					]}
				/>
			</BgtPageHeader>
			<BgtPageContent
				isLoading={isLoading}
				data={{ settings, gameNights }}
				fallback={view === "nights" ? <GameNightListSkeleton /> : undefined}
			>
				{({ settings }) => (
					<>
						{view === "stats" ? (
							<GameNightStatsView
								period={period}
								onPeriodChange={(value) => changeSearch({ period: value })}
								settings={settings}
								players={players}
								games={games}
								locations={locations}
							/>
						) : (
							<>
								{canWrite && blocker && upcomingGameNights.length > 0 && (
									<BgtText size="2" color="gray" data-testid="invites-hint">
										{askAdmin ? t("invites.ask-admin") : t(`invites.${blocker}`)}
									</BgtText>
								)}

								{liveGameNights.length > 0 && (
									<section className="flex flex-col gap-3" aria-labelledby="live-game-nights">
										<NightSectionTitle id="live-game-nights">{t("live")}</NightSectionTitle>
										{liveGameNights.map((gameNight) => (
											<GameNightCard
												key={gameNight.id}
												gameNight={gameNight}
												settings={settings}
												players={players}
												canWrite={canWrite}
												highlight
												live
												onEdit={actions.handleEditGameNight}
												onDelete={handleDeleteClick}
												onManageRsvps={actions.handleManageRsvps}
											/>
										))}
									</section>
								)}

								<section className="flex flex-col gap-3" aria-labelledby="upcoming-game-nights">
									<NightSectionTitle id="upcoming-game-nights">{t("upcoming")}</NightSectionTitle>
									{upcomingGameNights.length === 0 ? (
										<div className="flex flex-col items-start gap-3 rounded-lg border border-dashed border-white/15 p-4 sm:flex-row sm:items-center sm:justify-between">
											<div className="flex flex-col gap-0.5">
												<BgtText size="3" weight="bold">
													{t("empty.upcoming")}
												</BgtText>
												{canWrite && (
													<BgtText size="2" color="gray">
														{t("empty.upcoming-hint")}
													</BgtText>
												)}
											</div>
											{canWrite && (
												<BgtButton size="2" onClick={modals.createModal.show}>
													{t("create.button")}
												</BgtButton>
											)}
										</div>
									) : (
										upcomingGameNights.map((gameNight, index) => (
											<GameNightCard
												key={gameNight.id}
												gameNight={gameNight}
												settings={settings}
												players={players}
												canWrite={canWrite}
												highlight={index === 0 && liveGameNights.length === 0}
												onEdit={actions.handleEditGameNight}
												onDelete={handleDeleteClick}
												onManageRsvps={actions.handleManageRsvps}
											/>
										))
									)}
								</section>

								{pastGameNights.length > 0 && (
									<section className="flex flex-col gap-3" aria-labelledby="past-game-nights">
										<NightSectionTitle id="past-game-nights">{t("past")}</NightSectionTitle>
										<PastGameNightList
											gameNights={pastGameNights}
											settings={settings}
											players={players}
											games={games}
										/>
									</section>
								)}
							</>
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
							description={deleteNightDescription(t, selectedGameNight)}
						/>
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
