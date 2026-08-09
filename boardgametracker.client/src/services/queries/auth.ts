import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getLinkablePlayersCall, getProfileCall, getUsersCall } from "../authService";
import { createListQuery, createSingletonQuery } from "./queryFactory";

export const getProfile = createSingletonQuery(QUERY_KEYS.profile, getProfileCall);
export const getUsers = createListQuery(QUERY_KEYS.users, getUsersCall);

export const getLinkablePlayers = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.players, "linkable"],
		queryFn: getLinkablePlayersCall,
	});
