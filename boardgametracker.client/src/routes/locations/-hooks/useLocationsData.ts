import { useQuery } from "@tanstack/react-query";
import { getLocations } from "@/services/queries/locations";

export const useLocationsData = () => {
	const { data, isLoading } = useQuery(getLocations());

	return {
		locations: data ?? [],
		isLoading,
	};
};
