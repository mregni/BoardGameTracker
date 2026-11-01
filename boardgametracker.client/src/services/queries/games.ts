import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import {
	getBggCollectionCall,
	getGameCall,
	getGameExpansionsCall,
	getGamePlaySummariesCall,
	getGamePriceCall,
	getGameSessionsCall,
	getGameStatisticsCall,
	getGamesCall,
	getShameStatisticsCall,
	getShamesCall,
	getTrackedPricesCall,
} from "../gameService";
import { createEntityQuery, createListQuery, createNestedQuery, createNestedQueryWithKeys } from "./queryFactory";

export const getGames = createListQuery(QUERY_KEYS.games, getGamesCall);

export const getGamePlaySummaries = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.games, QUERY_KEYS.plays],
		queryFn: getGamePlaySummariesCall,
		retry: false,
	});

export const getGame = createEntityQuery(QUERY_KEYS.game, getGameCall);

export const getGameExpansions = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.expansions, getGameExpansionsCall);

export const getGameStatistics = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.statistics, getGameStatisticsCall);

export const getGamePrice = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.price, getGamePriceCall);

export const getTrackedPrices = createListQuery(QUERY_KEYS.trackedPrices, getTrackedPricesCall);

export const getGameSessions = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.sessions, getGameSessionsCall);

export const getGameSessionsShortList = createNestedQueryWithKeys(
	QUERY_KEYS.game,
	QUERY_KEYS.sessions,
	[QUERY_KEYS.shortlist],
	getGameSessionsCall,
);

export const getBggCollection = (username: string) =>
	queryOptions({
		queryKey: [QUERY_KEYS.game, QUERY_KEYS.bgg, username],
		queryFn: () => getBggCollectionCall(username),
		retry: false,
	});

export const getShames = (months?: number) =>
	queryOptions({
		queryKey: months === undefined ? [QUERY_KEYS.shames] : [QUERY_KEYS.shames, months],
		queryFn: () => getShamesCall(months),
	});

export const getShameStatistics = (months?: number) =>
	queryOptions({
		queryKey:
			months === undefined
				? [QUERY_KEYS.shames, QUERY_KEYS.statistics]
				: [QUERY_KEYS.shames, QUERY_KEYS.statistics, months],
		queryFn: () => getShameStatisticsCall(months),
	});
