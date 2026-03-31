import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getGameNightByLinkCall, getGameNightStatisticsCall, getGameNightsCall } from "../gameNightService";
import { createListQuery, createSingletonQuery } from "./queryFactory";

export const getGameNights = createListQuery(QUERY_KEYS.gameNights, getGameNightsCall);

export const getGameNightStatistics = createSingletonQuery(QUERY_KEYS.gameNightStatistics, getGameNightStatisticsCall);

export const getGameNightByLink = (linkId: string) =>
	queryOptions({
		queryKey: [QUERY_KEYS.gameNights, "link", linkId],
		queryFn: () => getGameNightByLinkCall(linkId),
		enabled: !!linkId,
	});
