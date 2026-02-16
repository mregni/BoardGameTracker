import { QUERY_KEYS } from "@/models";
import { getGameNightStatisticsCall, getGameNightsCall } from "../gameNightService";
import { createListQuery, createSingletonQuery } from "./queryFactory";

export const getGameNights = createListQuery(QUERY_KEYS.gameNights, getGameNightsCall);

export const getGameNightStatistics = createSingletonQuery(QUERY_KEYS.gameNightStatistics, getGameNightStatisticsCall);
