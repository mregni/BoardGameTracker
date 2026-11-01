import { noop } from "@tanstack/react-query";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo } from "react";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { SearchInputField } from "@/components/BgtForm";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { useDebouncedSearchQuery } from "@/hooks/useDebouncedSearchQuery";
import { usePermissions } from "@/hooks/usePermissions";
import { getPlayersOverview } from "@/services/queries/players";
import { PlayerRow } from "./-components/PlayerRow";
import { usePlayersData } from "./-hooks/usePlayersData";
import {
	filterPlayers,
	PLAYER_SORTS,
	type PlayerSort,
	type PlayersSearch,
	parsePlayersSearch,
	sortPlayers,
} from "./-utils/playersList";

export const Route = createFileRoute("/players/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getPlayersOverview()).catch(noop);
	},
	validateSearch: (search: Record<string, unknown>): PlayersSearch => parsePlayersSearch(search),
});

const SKELETON_ROWS = ["a", "b", "c", "d", "e", "f"];

const PlayersSkeleton = () => (
	<ul aria-hidden="true" className="flex flex-col gap-2">
		{SKELETON_ROWS.map((key) => (
			<li key={key} className="h-[66px] rounded-[10px] border border-card-border bg-card motion-safe:animate-pulse" />
		))}
	</ul>
);

function RouteComponent() {
	const { q, sort = "recent" } = Route.useSearch();
	const { t } = useTranslation(["player", "common"]);
	const { canWrite } = usePermissions();
	const navigate = useNavigate();
	const { players, isLoading, isError, refetch } = usePlayersData();

	const updateSearch = useCallback(
		(partial: PlayersSearch) => {
			navigate({ to: "/players", search: (prev) => parsePlayersSearch({ ...prev, ...partial }), replace: true });
		},
		[navigate],
	);

	const [filterValue, setFilterValue] = useDebouncedSearchQuery(q, (value) => updateSearch({ q: value }));
	const clearFilter = () => {
		setFilterValue("");
		updateSearch({ q: undefined });
	};

	const visiblePlayers = useMemo(() => filterPlayers(players, q), [players, q]);
	const played = useMemo(
		() =>
			sortPlayers(
				visiblePlayers.filter((x) => x.playCount > 0),
				sort,
			),
		[visiblePlayers, sort],
	);
	const notPlayed = useMemo(
		() =>
			sortPlayers(
				visiblePlayers.filter((x) => x.playCount === 0),
				"name",
			),
		[visiblePlayers],
	);

	const sortOptions = PLAYER_SORTS.map((value) => ({ value, label: t(`list.sort-${value}`) }));
	const addPlayer = () => navigate({ to: "/players/new" });

	if (isError && players.length === 0) {
		return (
			<BgtEmptyPage
				header={t("common:players")}
				icon={Users}
				emptyIcon={AlertTriangle}
				title={t("list.error-title")}
				description={t("list.error-description")}
				action={{ label: t("common:retry"), onClick: () => refetch() }}
			/>
		);
	}

	if (!isLoading && players.length === 0) {
		return (
			<BgtEmptyPage
				header={t("common:players")}
				icon={Users}
				title={t("empty.title")}
				description={t("empty.description")}
				action={canWrite ? { label: t("new.button"), onClick: addPlayer } : undefined}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("common:players")}
				icon={Users}
				actions={[
					{
						content: "common:leaderboard",
						icon: Trophy,
						variant: "cancel",
						onClick: () => navigate({ to: "/leaderboard" }),
					},
					...(canWrite
						? [{ content: "player:new.button", icon: PlusIcon, variant: "primary" as const, onClick: addPlayer }]
						: []),
				]}
			></BgtPageHeader>
			<BgtPageContent>
				<div className="flex w-full max-w-3xl flex-col gap-4">
					<div className="flex flex-wrap items-center gap-2">
						<div className="w-full sm:w-64 xl:w-[300px]">
							<SearchInputField
								value={filterValue}
								onChange={(event) => setFilterValue(event.target.value)}
								onClear={clearFilter}
							/>
						</div>
						<BgtSegmentedControl
							options={sortOptions}
							value={sort}
							onChange={(value: PlayerSort) => updateSearch({ sort: value === "recent" ? undefined : value })}
							aria-label={t("list.sort")}
							size="sm"
							className="max-sm:[&>button]:min-h-11"
						/>
						<p aria-live="polite" className="ml-auto text-sm text-white/70 tabular-nums">
							{isLoading
								? null
								: q
									? t("count-filtered", { count: visiblePlayers.length, total: players.length })
									: t("count", { count: players.length })}
						</p>
					</div>
					{isLoading ? (
						<PlayersSkeleton />
					) : visiblePlayers.length === 0 ? (
						<div className="flex flex-col items-center gap-3">
							<BgtNoData message={t("list.no-match", { query: q })} className="min-h-0 py-4" />
							<BgtButton variant="cancel" onClick={clearFilter}>
								{t("list.clear")}
							</BgtButton>
						</div>
					) : (
						<>
							{played.length > 0 && (
								<ul className="flex flex-col gap-2">
									{played.map((x) => (
										<PlayerRow key={x.id} player={x} sort={sort} />
									))}
								</ul>
							)}
							{notPlayed.length > 0 && (
								<section aria-labelledby="players-not-played" className="mt-4 flex flex-col gap-3">
									<div className="flex items-baseline justify-between gap-3">
										<h2 id="players-not-played" className="text-sm font-semibold text-white/80">
											{t("list.not-played")}
										</h2>
										{canWrite && (
											<Link
												to="/sessions/new"
												className="rounded-sm text-sm text-primary-text hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
											>
												{t("list.log-game")}
											</Link>
										)}
									</div>
									<ul className="flex flex-wrap gap-2">
										{notPlayed.map((x) => (
											<li key={x.id}>
												<Link
													to="/players/$playerId"
													params={{ playerId: x.id }}
													className="flex min-h-11 items-center gap-2 rounded-full border border-card-border bg-card py-1 pr-4 pl-1.5 text-sm text-white transition-colors hover:border-primary/60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
												>
													<span aria-hidden="true">
														<BgtAvatar image={x.image} title={x.name} size="medium" />
													</span>
													{x.name}
												</Link>
											</li>
										))}
									</ul>
								</section>
							)}
						</>
					)}
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
