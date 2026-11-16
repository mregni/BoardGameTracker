import { QUERY_KEYS } from "@/models";
import { getStorageLocationsCall } from "../storageService";
import { createListQuery } from "./queryFactory";

export const getStorageLocations = createListQuery(QUERY_KEYS.storage, getStorageLocationsCall);
