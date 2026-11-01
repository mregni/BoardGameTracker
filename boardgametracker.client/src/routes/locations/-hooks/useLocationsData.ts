import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { getLocations } from "@/services/queries/locations";

export const useLocationsData = () => {
	const { data, isLoading, isError, refetch } = useQuery(getLocations());

	const locations = useMemo(
		() => [...(data ?? [])].sort((a, b) => b.playCount - a.playCount || a.name.localeCompare(b.name)),
		[data],
	);

	return {
		locations,
		isLoading,
		isError,
		refetch,
	};
};
