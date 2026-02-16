import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getCompareCall } from "../compareService";

export const getCompare = (playerOne: number, playerTwo: number) =>
	queryOptions({
		queryKey: [QUERY_KEYS.compare, playerOne, playerTwo],
		queryFn: () => getCompareCall(playerOne, playerTwo),
	});
