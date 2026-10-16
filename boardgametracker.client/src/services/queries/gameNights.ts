import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getGameNightByLinkCall, getGameNightCall, getGameNightsCall } from "../gameNightService";
import { createEntityQuery, createListQuery } from "./queryFactory";

export const getGameNights = createListQuery(QUERY_KEYS.gameNights, getGameNightsCall);

export const getGameNight = createEntityQuery(QUERY_KEYS.gameNights, getGameNightCall);

export const getGameNightByLink = (linkId: string) =>
	queryOptions({
		queryKey: [QUERY_KEYS.gameNights, "link", linkId],
		queryFn: () => getGameNightByLinkCall(linkId),
		enabled: !!linkId,
	});
