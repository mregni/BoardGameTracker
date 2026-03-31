import { QUERY_KEYS } from "@/models";
import { getStatistics } from "../dashboardService";
import { createEntityQueryWithKeys } from "./queryFactory";

export const getDashboardStatistics = createEntityQueryWithKeys(
	[QUERY_KEYS.dashboard, QUERY_KEYS.statistics],
	getStatistics,
);
