import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import MapPinIcon from "@/assets/icons/map-pin.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { usePermissions } from "@/hooks/usePermissions";
import { useStorageData } from "@/hooks/useStorage";
import type { Location } from "@/models";
import { getGames } from "@/services/queries/games";
import { getLocation, getLocationSessions, getLocationStatistics } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { getStorageLocations } from "@/services/queries/storage";
import { locationIdParamSchema } from "@/utils/routeSchemas";
import { storageItems } from "@/utils/storage";
import { useBack } from "../-hooks/useBack";
import { useGameById } from "../-hooks/useGameById";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { LocationHeader } from "./-components/LocationHeader";
import { LocationPlayedHere } from "./-components/LocationPlayedHere";
import { LocationSessionsCard } from "./-components/LocationSessionsCard";
import { LocationStatisticsGrid } from "./-components/LocationStatisticsGrid";
import { LocationStorageCard } from "./-components/LocationStorageCard";
import { useLocationData } from "./-hooks/useLocationData";
import { useLocationModals } from "./-hooks/useLocationModals";
import { EditLocationModal } from "./-modals/EditLocationModal";

export const Route = createFileRoute("/locations/$locationId")({
	component: RouteComponent,
	params: locationIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getLocation(params.locationId)).catch(noop);
		queryClient.query(getLocationStatistics(params.locationId)).catch(noop);
		queryClient.query(getLocationSessions(params.locationId)).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
		queryClient.query(getStorageLocations()).catch(noop);
	},
});

function RouteComponent() {
	const { locationId } = Route.useParams();
	const { t } = useTranslation(["location", "common"]);
	const { canWrite } = usePermissions();
	const modals = useLocationModals();
	const goBack = useBack("/locations");

	const { location, statistics, sessions, settings, isLoading, isError, isNotFound, retry, deleteLocation } =
		useLocationData({ locationId });
	const { gameById } = useGameById();
	const { nodes } = useStorageData();
	const { data: games = [] } = useQuery(getGames());
	const items = storageItems(games);

	if (isError) {
		return (
			<BgtEmptyPage
				showHeader={false}
				icon={MapPinIcon}
				title={t(isNotFound ? "detail.not-found.title" : "detail.load-failed.title")}
				description={t(isNotFound ? "detail.not-found.description" : "detail.load-failed.description")}
				action={
					isNotFound ? { label: t("detail.back"), onClick: goBack } : { label: t("common:retry"), onClick: retry }
				}
			/>
		);
	}

	const handleDelete = async () => {
		await deleteLocation();
		modals.deleteModal.hide();
	};

	const isInUse = ({ playCount, gameNightCount }: Location) => playCount > 0 || gameNightCount > 0;
	const inUseDescription = ({ playCount, gameNightCount }: Location) =>
		[
			playCount > 0 && t("delete.in-use.sessions", { count: playCount }),
			gameNightCount > 0 && t("delete.in-use.game-nights", { count: gameNightCount }),
			t("delete.in-use.hint"),
		]
			.filter(Boolean)
			.join(" ");
	const deleteDescription = ({ id, name, storedCount }: Location) => {
		const inside = nodes.filter((node) => node.locationId === id).length;
		return [
			t("delete.description", { name }),
			inside > 0 && t("delete.storage.inside", { count: inside }),
			storedCount > 0 && t("delete.storage.unplaced", { count: storedCount }),
		]
			.filter(Boolean)
			.join(" ");
	};

	return (
		<BgtPage>
			<BgtPageHeader backAction={goBack} backText={t("detail.back")} />
			<BgtPageContent isLoading={isLoading} data={{ location, statistics, settings }}>
				{({ location, statistics, settings }) => (
					<>
						<LocationHeader
							name={location.name}
							lastPlayed={sessions[0]?.start ?? null}
							lastGame={sessions[0] && gameById(sessions[0].gameId)?.title}
							uiLanguage={settings.uiLanguage}
							canWrite={canWrite}
							onEdit={modals.editModal.show}
							onDelete={modals.deleteModal.show}
						/>
						{sessions.length === 0 ? (
							<>
								<LocationStorageCard location={location} nodes={nodes} items={items} />
								<BgtText size="2" color="white" opacity={60}>
									{t("detail.no-sessions")}
								</BgtText>
							</>
						) : (
							<>
								<LocationStatisticsGrid
									playCount={location.playCount}
									storedCount={location.storedCount}
									statistics={statistics}
								/>
								<LocationPlayedHere sessions={sessions} />
								<LocationSessionsCard
									sessions={sessions}
									dateFormat={settings.dateFormat}
									uiLanguage={settings.uiLanguage}
									canOpen={canWrite}
								/>
								<LocationStorageCard location={location} nodes={nodes} items={items} />
							</>
						)}
						<BgtDeleteModal
							title={location.name}
							open={modals.deleteModal.isOpen}
							close={modals.deleteModal.hide}
							onDelete={isInUse(location) ? undefined : handleDelete}
							heading={isInUse(location) ? t("delete.in-use.title") : undefined}
							description={isInUse(location) ? inUseDescription(location) : deleteDescription(location)}
						/>
						{modals.editModal.isOpen && (
							<EditLocationModal location={location} open={modals.editModal.isOpen} close={modals.editModal.hide} />
						)}
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
