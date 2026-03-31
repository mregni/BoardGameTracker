import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getAllBadgesCall } from "../badgeService";

export const getBadges = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.badges],
		queryFn: () => getAllBadgesCall(),
	});
