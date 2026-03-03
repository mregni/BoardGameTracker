import { QUERY_KEYS } from "@/models";
import { getSessionCall } from "../sessionService";
import { createEntityQuery } from "./queryFactory";

export const getSession = createEntityQuery(QUERY_KEYS.sessions, getSessionCall);
