import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getCountsCall } from "../countService";

export const getCounts = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.counts],
		queryFn: () => getCountsCall(),
	});
