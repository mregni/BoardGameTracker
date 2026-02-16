import { useQueries, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deleteLocationCall } from "@/services/locationService";
import { getLocations } from "@/services/queries/locations";

interface Props {
	onDeleteSuccess?: () => void;
}

export const useLocationsData = ({ onDeleteSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { infoToast, errorToast } = useToasts();

	const [locationsQuery] = useQueries({
		queries: [getLocations()],
	});

	const locations = locationsQuery.data ?? [];

	const deleteLocation = async (id: number) => {
		try {
			await deleteLocationCall(id);
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
			infoToast("location.notifications.deleted");
			onDeleteSuccess?.();
		} catch {
			errorToast("location.notifications.delete-failed");
		}
	};

	return {
		locations,
		deleteLocation,
		isLoading: locationsQuery.isLoading,
	};
};
