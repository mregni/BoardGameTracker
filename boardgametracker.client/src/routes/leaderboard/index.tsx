import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useCallback, useMemo } from "react";
import { useTranslation } from "react-i18next";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import { BgtButton } from "@/components/BgtButton/BgtButton";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { getGames } from "@/services/queries/games";
import { getLeaderboard } from "@/services/queries/leaderboard";
import { LeaderboardFilters } from "./-components/LeaderboardFilters";
import { LeaderboardPodium, podiumPlayers } from "./-components/LeaderboardPodium";
import { LeaderboardRanking } from "./-components/LeaderboardRanking";
import { LeaderboardRules } from "./-components/LeaderboardRules";
import { LeaderboardTiles } from "./-components/LeaderboardTiles";
import {
	DEFAULT_MIN_SESSIONS,
	hasActiveFilters,
	type LeaderboardSearch,
	leaderboardSearchSchema,
	MIN_SESSION_OPTIONS,
	toLeaderboardParams,
} from "./-utils/leaderboardSearch";

export const Route = createFileRoute("/leaderboard/")({
	component: RouteComponent,
	validateSearch: leaderboardSearchSchema,
	loaderDeps: ({ search }) => search,
	loader: ({ context: { queryClient }, deps }) => {
		queryClient.query(getLeaderboard(toLeaderboardParams(deps))).catch(noop);
		queryClient.query(getGames()).catch(noop);
	},
});

function RouteComponent() {
	const { t } = useTranslation("leaderboard");
	const search = Route.useSearch();
	const navigate = useNavigate({ from: Route.fullPath });
	const today = useMemo(() => new Date(), []);
	const params = useMemo(() => toLeaderboardParams(search, today), [search, today]);
	const { data: leaderboard, isLoading, isPlaceholderData } = useQuery(getLeaderboard(params));
	const { data: games = [] } = useQuery(getGames());

	const updateSearch = useCallback(
		(next: Partial<LeaderboardSearch>) => {
			navigate({ search: (previous) => ({ ...previous, ...next }), replace: true });
		},
		[navigate],
	);

	const mostSessions = Math.max(0, ...(leaderboard?.players.map((entry) => entry.playCount) ?? []));
	const lowerMinimum = MIN_SESSION_OPTIONS.filter(
		(count) => count <= mostSessions && count < (leaderboard?.minimumSessions ?? 0),
	).at(-1);

	const gameTitle = search.game ? games.find((game) => game.id === search.game)?.title : undefined;
	const podium = podiumPlayers(leaderboard?.players ?? []);
	const rest = leaderboard?.players.filter((entry) => !podium.includes(entry)) ?? [];

	if (!isLoading && leaderboard && leaderboard.players.length === 0 && !hasActiveFilters(search)) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={TrophyIcon}
				title={t("empty.title")}
				description={t("empty.description")}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader header={t("title")} icon={TrophyIcon} />
			<BgtPageContent>
				<LeaderboardFilters search={search} games={games} onChange={updateSearch} />
				<BgtPageContent
					isLoading={isLoading}
					data={{ leaderboard }}
					className={cx("transition-opacity", isPlaceholderData && "opacity-60")}
				>
					{({ leaderboard }) =>
						leaderboard.players.length === 0 ? (
							<BgtEmptyState
								icon={TrophyIcon}
								title={t("empty.filtered-title")}
								description={t("empty.filtered-description")}
								action={{ label: t("empty.reset"), onClick: () => navigate({ search: {}, replace: true }) }}
							/>
						) : (
							<>
								<LeaderboardRules leaderboard={leaderboard} />
								{leaderboard.rankedCount === 0 && (
									<div
										role="status"
										className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-card-border bg-card px-4 py-3"
									>
										<p className="text-sm text-white/80">
											{t("ranking.nobody-ranked", { count: leaderboard.minimumSessions })}
										</p>
										{lowerMinimum && (
											<BgtButton
												onClick={() =>
													updateSearch({ min: lowerMinimum === DEFAULT_MIN_SESSIONS ? undefined : lowerMinimum })
												}
											>
												{t("ranking.lower-minimum", { count: lowerMinimum })}
											</BgtButton>
										)}
									</div>
								)}
								<LeaderboardPodium players={leaderboard.players} rankBy={leaderboard.rankBy} />
								<LeaderboardTiles leaderboard={leaderboard} podium={podium} />
								{rest.length > 0 && (
									<LeaderboardRanking
										players={rest}
										rankBy={leaderboard.rankBy}
										minimumSessions={leaderboard.minimumSessions}
										label={gameTitle ? `${t("ranking.label")} · ${gameTitle}` : t("ranking.label")}
									/>
								)}
							</>
						)
					}
				</BgtPageContent>
			</BgtPageContent>
		</BgtPage>
	);
}
