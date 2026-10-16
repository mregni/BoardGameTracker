import { useQueries, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deleteLocationCall } from "@/services/locationService";
import { getLocation, getLocationSessions, getLocationStatistics } from "@/services/queries/locations";
import { getSettings } from "@/services/queries/settings";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	locationId: number;
}

export const useLocationData = ({ locationId }: Props) => {
	const queryClient = useQueryClient();
	const navigate = useNavigate();
	const { successToast, errorToast } = useToasts();

	const [locationQuery, statisticsQuery, sessionsQuery, settingsQuery] = useQueries({
		queries: [
			getLocation(locationId),
			getLocationStatistics(locationId),
			getLocationSessions(locationId),
			getSettings(),
		],
	});

	const deleteLocation = async () => {
		try {
			await deleteLocationCall(locationId);
		} catch (error) {
			errorToast(apiErrorMessage(error, "location:notifications.delete-failed"));
			return;
		}

		successToast("location:notifications.deleted");
		await navigate({ to: "/locations" });
		queryClient.removeQueries({ queryKey: [QUERY_KEYS.locations, locationId] });
		await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
		await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
	};

	return {
		location: locationQuery.data,
		statistics: statisticsQuery.data,
		sessions: sessionsQuery.data ?? [],
		settings: settingsQuery.data,
		isLoading:
			locationQuery.isLoading || statisticsQuery.isLoading || sessionsQuery.isLoading || settingsQuery.isLoading,
		deleteLocation,
	};
};
