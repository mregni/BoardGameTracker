import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo } from "react";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import Users from "@/assets/icons/users.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { SearchInputField } from "@/components/BgtForm";
import { BgtImageCard } from "@/components/BgtImageCard/BgtImageCard";
import { BgtCardList } from "@/components/BgtLayout/BgtCardList";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { BgtText } from "@/components/BgtText/BgtText";
import { useDebouncedSearchQuery } from "@/hooks/useDebouncedSearchQuery";
import { usePermissions } from "@/hooks/usePermissions";
import { getPlayersOverview } from "@/services/queries/players";
import { toRelative } from "@/utils/dateUtils";
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

const SKELETON_TILES = ["a", "b", "c", "d", "e", "f"];

const PlayersSkeleton = () => (
	<BgtCardList aria-hidden="true">
		{SKELETON_TILES.map((key) => (
			<div key={key} className="aspect-square rounded-lg bg-card motion-safe:animate-pulse" />
		))}
	</BgtCardList>
);

function RouteComponent() {
	const { q, sort = "recent" } = Route.useSearch();
	const { t, i18n } = useTranslation(["player", "common", "games"]);
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
	const ordered = useMemo(() => [...played, ...notPlayed], [played, notPlayed]);

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
				<div className="flex w-full flex-col gap-4">
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
									? t("count-filtered", { shown: visiblePlayers.length, count: players.length })
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
						<BgtCardList>
							{ordered.map((x) => (
								<BgtImageCard
									key={x.id}
									title={x.name}
									image={x.image}
									link={`/players/${x.id}`}
									meta={
										x.playCount > 0 ? (
											<BgtText size="1" className="text-card-value truncate">
												{t("list.record", { wins: x.winCount, count: x.playCount })}
												{sort === "recent" && x.lastPlayed && ` · ${toRelative(x.lastPlayed, i18n.language)}`}
											</BgtText>
										) : (
											<BgtText size="1" color="white" opacity={50} className="truncate">
												{t("games:never-played")}
											</BgtText>
										)
									}
								/>
							))}
						</BgtCardList>
					)}
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
