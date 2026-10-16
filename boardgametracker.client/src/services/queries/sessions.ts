import { QUERY_KEYS } from "@/models";
import { getSessionCall, getSessionSuggestionsCall } from "../sessionService";
import { createEntityQuery, createEntityQueryWithKeys } from "./queryFactory";

export const getSession = createEntityQuery(QUERY_KEYS.sessions, getSessionCall);

export const getSessionSuggestions = createEntityQueryWithKeys(
	[QUERY_KEYS.sessions, QUERY_KEYS.suggestions],
	getSessionSuggestionsCall,
);
