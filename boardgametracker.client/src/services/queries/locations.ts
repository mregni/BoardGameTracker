import { QUERY_KEYS } from "@/models";
import {
	getLocationCall,
	getLocationSessionsCall,
	getLocationStatisticsCall,
	getLocationsCall,
} from "../locationService";
import { createEntityQuery, createListQuery, createNestedQuery } from "./queryFactory";

export const getLocations = createListQuery(QUERY_KEYS.locations, getLocationsCall);

export const getLocation = createEntityQuery(QUERY_KEYS.locations, getLocationCall);

export const getLocationStatistics = createNestedQuery(
	QUERY_KEYS.locations,
	QUERY_KEYS.statistics,
	getLocationStatisticsCall,
);

export const getLocationSessions = createNestedQuery(
	QUERY_KEYS.locations,
	QUERY_KEYS.sessions,
	getLocationSessionsCall,
);
