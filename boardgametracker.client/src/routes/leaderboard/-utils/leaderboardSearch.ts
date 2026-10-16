import { format, startOfYear, subMonths } from "date-fns";
import { z } from "zod";
import type { LeaderboardParams, LeaderboardRankBy } from "@/models";

export const PERIODS = ["all", "year", "12m", "custom"] as const;
export const MIN_SESSION_OPTIONS = [1, 3, 5, 10] as const;
export const RANK_BY_OPTIONS: LeaderboardRankBy[] = ["wins", "winRate", "sessions"];
export const DEFAULT_MIN_SESSIONS = 5;
export const DEFAULT_RANK_BY: LeaderboardRankBy = "wins";

export type LeaderboardPeriod = (typeof PERIODS)[number];

const isoDate = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

export const leaderboardSearchSchema = z.object({
	period: z.enum(PERIODS).optional().catch(undefined),
	from: isoDate.optional().catch(undefined),
	to: isoDate.optional().catch(undefined),
	game: z.number().int().positive().optional().catch(undefined),
	min: z.number().int().min(1).max(1000).optional().catch(undefined),
	rank: z.enum(["wins", "winRate", "sessions"]).optional().catch(undefined),
});

export type LeaderboardSearch = z.infer<typeof leaderboardSearchSchema>;

const toIso = (date: Date) => format(date, "yyyy-MM-dd");

export const periodRange = (search: LeaderboardSearch, today: Date): { from?: string; to?: string } => {
	switch (search.period) {
		case "year":
			return { from: toIso(startOfYear(today)) };
		case "12m":
			return { from: toIso(subMonths(today, 12)) };
		case "custom":
			if (search.from && search.to && search.from > search.to) {
				return { from: search.to, to: search.from };
			}
			return { from: search.from, to: search.to };
		default:
			return {};
	}
};

export const toLeaderboardParams = (search: LeaderboardSearch, today: Date = new Date()): LeaderboardParams => {
	const { from, to } = periodRange(search, today);
	return {
		...(from ? { from } : {}),
		...(to ? { to } : {}),
		...(search.game ? { gameId: search.game } : {}),
		minSessions: search.min ?? DEFAULT_MIN_SESSIONS,
		rankBy: search.rank ?? DEFAULT_RANK_BY,
	};
};

export const hasActiveFilters = (search: LeaderboardSearch): boolean =>
	(search.period !== undefined && search.period !== "all") || search.game !== undefined;
