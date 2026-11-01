import { QUERY_KEYS } from "@/models";
import {
	getLocationCall,
	getLocationSessionsCall,
	getLocationStatisticsCall,
	getLocationsCall,
} from "../locationService";
import { createEntityQuery, createListQuery, createNestedQuery } from "./queryFactory";

export const getLocations = createListQuery(QUERY_KEYS.locations, getLocationsCall);

const location = createEntityQuery(QUERY_KEYS.locations, getLocationCall);
const statistics = createNestedQuery(QUERY_KEYS.locations, QUERY_KEYS.statistics, getLocationStatisticsCall);
const sessions = createNestedQuery(QUERY_KEYS.locations, QUERY_KEYS.sessions, getLocationSessionsCall);

export const getLocation = (id: number) => ({ ...location(id), meta: { silent: true } });

export const getLocationStatistics = (id: number) => ({ ...statistics(id), meta: { silent: true } });

export const getLocationSessions = (id: number) => ({ ...sessions(id), meta: { silent: true } });
