import { noop } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { getGames } from "@/services/queries/games";
import { getLocation, getLocationSessions, getLocationStatistics } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { locationIdParamSchema } from "@/utils/routeSchemas";
import { useBack } from "../-hooks/useBack";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { LocationHeader } from "./-components/LocationHeader";
import { LocationSessionsCard } from "./-components/LocationSessionsCard";
import { LocationStatisticsGrid } from "./-components/LocationStatisticsGrid";
import { useLocationData } from "./-hooks/useLocationData";
import { useLocationModals } from "./-hooks/useLocationModals";
import { EditLocationModal } from "./-modals/EditLocationModal";

export const Route = createFileRoute("/locations/$locationId")({
	component: RouteComponent,
	params: locationIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getLocation(params.locationId)).catch(noop);
		queryClient.query(getLocationStatistics(params.locationId)).catch(noop);
		queryClient.query(getLocationSessions(params.locationId, 5)).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
	},
});

function RouteComponent() {
	const { locationId } = Route.useParams();
	const { t } = useTranslation("location");
	const { canWrite } = usePermissions();
	const modals = useLocationModals();
	const goBack = useBack("/locations");

	const { location, statistics, sessions, settings, isLoading, deleteLocation } = useLocationData({ locationId });

	const handleDelete = async () => {
		await deleteLocation();
		modals.deleteModal.hide();
	};

	return (
		<BgtPage>
			<BgtPageHeader backAction={goBack} backText={t("detail.back")} />
			<BgtPageContent isLoading={isLoading} data={{ location, statistics, settings }}>
				{({ location, statistics, settings }) => (
					<>
						<LocationHeader
							name={location.name}
							canWrite={canWrite}
							onEdit={modals.editModal.show}
							onDelete={modals.deleteModal.show}
						/>
						<LocationStatisticsGrid playCount={location.playCount} statistics={statistics} />
						<LocationSessionsCard
							sessions={sessions}
							dateFormat={settings.dateFormat}
							uiLanguage={settings.uiLanguage}
							canOpen={canWrite}
						/>
						<BgtDeleteModal
							title={location.name}
							open={modals.deleteModal.isOpen}
							close={modals.deleteModal.hide}
							onDelete={handleDelete}
							description={
								t("delete.description", { name: location.name }) +
								" " +
								(location.playCount > 0 ? t("delete.extra-description", { count: location.playCount }) : "")
							}
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
