import { QUERY_KEYS } from "@/models";
import { getLocationsCall } from "../locationService";
import { createListQuery } from "./queryFactory";

export const getLocations = createListQuery(QUERY_KEYS.locations, getLocationsCall);
