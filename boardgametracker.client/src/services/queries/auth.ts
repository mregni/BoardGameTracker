import { QUERY_KEYS } from "@/models";
import { getProfileCall, getUsersCall } from "../authService";
import { createListQuery, createSingletonQuery } from "./queryFactory";

export const getProfile = createSingletonQuery(QUERY_KEYS.profile, getProfileCall);
export const getUsers = createListQuery(QUERY_KEYS.users, getUsersCall);
