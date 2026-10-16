import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getCompareCall, getDefaultPairCall } from "../compareService";

export const getCompare = (playerOne: number, playerTwo: number) =>
	queryOptions({
		queryKey: [QUERY_KEYS.compare, playerOne, playerTwo],
		queryFn: () => getCompareCall(playerOne, playerTwo),
		enabled: playerOne > 0 && playerTwo > 0 && playerOne !== playerTwo,
	});

export const getDefaultComparePair = (enabled = true) =>
	queryOptions({
		queryKey: [QUERY_KEYS.compare, "default-pair"],
		queryFn: getDefaultPairCall,
		enabled,
	});
