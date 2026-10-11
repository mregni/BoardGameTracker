import { QUERY_KEYS } from "@/models";
import {
	getPlayerCall,
	getPlayerSessionsCall,
	getPlayerStatisticsCall,
	getPlayersCall,
	getPlayersOverviewCall,
} from "../playerService";
import {
	createEntityQuery,
	createEntityQueryWithKeys,
	createListQuery,
	createNestedQuery,
	createNestedQueryWithKeys,
} from "./queryFactory";

export const getPlayers = createListQuery(QUERY_KEYS.players, getPlayersCall);

export const getPlayersOverview = createEntityQueryWithKeys(
	[QUERY_KEYS.players, QUERY_KEYS.overview],
	getPlayersOverviewCall,
);

export const getPlayer = createEntityQuery(QUERY_KEYS.player, getPlayerCall);

export const getPlayerStatistics = createNestedQuery(QUERY_KEYS.player, QUERY_KEYS.statistics, getPlayerStatisticsCall);

export const getPlayerSessions = createNestedQuery(QUERY_KEYS.player, QUERY_KEYS.sessions, getPlayerSessionsCall);

export const getPlayerSessionsShortList = createNestedQueryWithKeys(
	QUERY_KEYS.player,
	QUERY_KEYS.sessions,
	[QUERY_KEYS.shortlist],
	getPlayerSessionsCall,
);
