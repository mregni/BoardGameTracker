import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { getLocations } from "@/services/queries/locations";
import { sortByActivity } from "@/utils/sortUtils";

export const useLocationsData = () => {
	const { data, isLoading } = useQuery(getLocations());

	const locations = useMemo(() => sortByActivity(data ?? []), [data]);

	return {
		locations,
		isLoading,
	};
};
