import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/gamepad.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { getGames } from "@/services/queries/games";
import { ANY, filterGames, type GamesFilterSearch, PLAY_TIMES, PLAYER_COUNTS } from "./-components/GamesFilters";
import { PickWheel } from "./-components/PickWheel";
import { useGamesData } from "./-hooks/useGamesData";
import { parseGamesSearch, pickWheelGames, withoutUndefined } from "./-utils/gamesSearch";

type PickSearch = Pick<GamesFilterSearch, "players" | "playTime" | "category">;

export const Route = createFileRoute("/games/pick")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getGames()).catch(noop);
	},
	validateSearch: (search: Record<string, unknown>): PickSearch => {
		const { players, playTime, category } = parseGamesSearch(search);
		return { players, playTime, category };
	},
});

function RouteComponent() {
	const { players, playTime, category } = Route.useSearch();
	const { t } = useTranslation("games");
	const navigate = useNavigate({ from: Route.fullPath });
	const { games, isLoading } = useGamesData();

	const categories = useMemo(
		() => [...new Set(games.flatMap((game) => game.categories.map((cat) => cat.name)))].sort(),
		[games],
	);

	const candidates = useMemo(() => {
		const inCategory =
			category === undefined ? games : games.filter((game) => game.categories.some((cat) => cat.name === category));
		return pickWheelGames(filterGames(inCategory, { playerCount: players, maxPlayTime: playTime }));
	}, [games, category, players, playTime]);

	const update = (partial: Partial<PickSearch>) =>
		navigate({ search: (prev) => withoutUndefined({ ...prev, ...partial }), replace: true });

	const withAny = <T extends string | number>(items: { value: T; label: string }[]) => [
		{ value: ANY, label: t("filters.any") },
		...items,
	];

	if (isLoading) return null;

	return (
		<BgtPage>
			<BgtPageHeader header={t("pick.title")} icon={Game} backAction={() => navigate({ to: "/games" })} />
			<BgtPageContent>
				<div className="grid gap-6 lg:grid-cols-[320px_1fr] lg:items-start">
					<BgtCard className="flex flex-col gap-3">
						<BgtText color="white" opacity={70} size="2">
							{t("pick.description")}
						</BgtText>
						<BgtSimpleSelect
							label={t("filters.player-count")}
							value={players ?? ANY}
							items={withAny(
								PLAYER_COUNTS.map((c) => ({ value: c, label: t("filters.player-count-value", { count: c }) })),
							)}
							onValueChange={(value) => update({ players: value === ANY ? undefined : Number(value) })}
						/>
						<BgtSimpleSelect
							label={t("filters.play-time")}
							value={playTime ?? ANY}
							items={withAny(PLAY_TIMES.map((m) => ({ value: m, label: t("filters.play-time-value", { count: m }) })))}
							onValueChange={(value) => update({ playTime: value === ANY ? undefined : Number(value) })}
						/>
						<BgtSimpleSelect
							label={t("filters.category")}
							value={category ?? ANY}
							hasSearch
							items={withAny(categories.map((name) => ({ value: name, label: name })))}
							onValueChange={(value) => update({ category: value === ANY ? undefined : String(value) })}
						/>
						<BgtText size="2" className="text-card-value font-bold">
							{t("pick.candidates", { count: candidates.length })}
						</BgtText>
					</BgtCard>
					{candidates.length > 0 ? (
						<PickWheel key={candidates.map((game) => game.id).join("-")} games={candidates} />
					) : (
						<BgtText color="white" opacity={70} className="text-center py-12">
							{t("pick.none")}
						</BgtText>
					)}
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
