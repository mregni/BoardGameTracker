import { QUERY_KEYS } from "@/models";
import { getPlayerCall, getPlayerSessionsCall, getPlayerStatisticsCall, getPlayersCall } from "../playerService";
import { createEntityQuery, createListQuery, createNestedQuery, createNestedQueryWithKeys } from "./queryFactory";

export const getPlayers = createListQuery(QUERY_KEYS.players, getPlayersCall);

export const getPlayer = createEntityQuery(QUERY_KEYS.player, getPlayerCall);

export const getPlayerStatistics = createNestedQuery(QUERY_KEYS.player, QUERY_KEYS.statistics, getPlayerStatisticsCall);

export const getPlayerSessions = createNestedQuery(QUERY_KEYS.player, QUERY_KEYS.sessions, getPlayerSessionsCall);

export const getPlayerSessionsShortList = createNestedQueryWithKeys(
	QUERY_KEYS.player,
	QUERY_KEYS.sessions,
	[QUERY_KEYS.shortlist],
	getPlayerSessionsCall,
);
