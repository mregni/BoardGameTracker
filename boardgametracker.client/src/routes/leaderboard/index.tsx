import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo } from "react";
import { useTranslation } from "react-i18next";
import InfoIcon from "@/assets/icons/info.svg?react";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import type { Leaderboard } from "@/models";
import { getGames } from "@/services/queries/games";
import { getLeaderboard } from "@/services/queries/leaderboard";
import { LeaderboardFilters } from "./-components/LeaderboardFilters";
import { LeaderboardPodium } from "./-components/LeaderboardPodium";
import { LeaderboardRanking } from "./-components/LeaderboardRanking";
import { LeaderboardTiles } from "./-components/LeaderboardTiles";
import {
	hasActiveFilters,
	type LeaderboardSearch,
	leaderboardSearchSchema,
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

const NoPodium = ({ leaderboard, gameTitle }: { leaderboard: Leaderboard; gameTitle?: string }) => {
	const { t } = useTranslation("leaderboard");
	const unranked = leaderboard.players.filter((entry) => !entry.isRanked);
	const reason =
		leaderboard.rankedCount === 0
			? t(gameTitle ? "no-podium.none-game" : "no-podium.none", { game: gameTitle })
			: t(gameTitle ? "no-podium.game" : "no-podium.all", {
					count: leaderboard.rankedCount,
					minimum: leaderboard.minimumPlayersForPodium,
					game: gameTitle,
				});
	const hint =
		unranked.length === 1
			? t("no-podium.include-one", { name: unranked[0].name })
			: unranked.length > 1
				? t("no-podium.include-more")
				: null;

	return (
		<p
			role="note"
			className="grid grid-cols-[1.375rem_minmax(0,1fr)] items-start gap-2 rounded-[10px] border border-card-border bg-card-light px-3 py-2.5 text-sm"
		>
			<InfoIcon aria-hidden="true" className="mt-0.5 size-4 text-primary-text" />
			<span>
				{reason}
				{hint && ` ${hint}`}
			</span>
		</p>
	);
};

function RouteComponent() {
	const { t } = useTranslation("leaderboard");
	const search = Route.useSearch();
	const navigate = useNavigate({ from: Route.fullPath });
	const today = useMemo(() => new Date(), []);
	const params = useMemo(() => toLeaderboardParams(search, today), [search, today]);
	const { data: leaderboard, isLoading } = useQuery(getLeaderboard(params));
	const { data: games = [] } = useQuery(getGames());

	const updateSearch = useCallback(
		(next: Partial<LeaderboardSearch>) => {
			navigate({ search: (previous) => ({ ...previous, ...next }), replace: true });
		},
		[navigate],
	);

	const gameTitle = search.game ? games.find((game) => game.id === search.game)?.title : undefined;

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
				<LeaderboardFilters search={search} games={games} today={today} onChange={updateSearch} />
				<BgtPageContent isLoading={isLoading} data={{ leaderboard }}>
					{({ leaderboard }) =>
						leaderboard.players.length === 0 ? (
							<BgtEmptyState
								icon={TrophyIcon}
								title={t("empty.filtered-title")}
								description={t("empty.filtered-description")}
							/>
						) : (
							<>
								<LeaderboardTiles leaderboard={leaderboard} />
								{leaderboard.showPodium ? (
									<LeaderboardPodium
										players={leaderboard.players.filter((entry) => entry.isRanked)}
										rankBy={leaderboard.rankBy}
									/>
								) : (
									<NoPodium leaderboard={leaderboard} gameTitle={gameTitle} />
								)}
								<LeaderboardRanking
									players={leaderboard.players}
									minimumSessions={leaderboard.minimumSessions}
									minimumPlayersForPodium={leaderboard.minimumPlayersForPodium}
									label={gameTitle ? `${t("ranking.label")} · ${gameTitle}` : t("ranking.label")}
									winCount={leaderboard.winCount}
									coopWinCount={leaderboard.coopWinCount}
								/>
							</>
						)
					}
				</BgtPageContent>
			</BgtPageContent>
		</BgtPage>
	);
}
