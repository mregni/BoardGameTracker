import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getGameIdsWithManualsCall, getGameManualsCall, getGameNightManualsCall } from "../manualService";
import { createNestedQuery } from "./queryFactory";

export const getGameManuals = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.manuals, getGameManualsCall);

export const getGameIdsWithManuals = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.games, QUERY_KEYS.manuals],
		queryFn: getGameIdsWithManualsCall,
	});

export const getGameNightManuals = (linkId: string) =>
	queryOptions({
		queryKey: [QUERY_KEYS.gameNights, QUERY_KEYS.manuals, linkId],
		queryFn: () => getGameNightManualsCall(linkId),
		enabled: !!linkId,
	});
