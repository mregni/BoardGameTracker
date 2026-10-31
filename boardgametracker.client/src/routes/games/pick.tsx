import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useMemo } from "react";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/gamepad.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { getGames } from "@/services/queries/games";
import { ANY, filterGames, type GamesFilterSearch, PLAY_TIMES, PLAYER_COUNTS } from "./-components/GamesFilters";
import { PICK_ACTION_SELECTOR, PickWheel } from "./-components/PickWheel";
import { useGamesData } from "./-hooks/useGamesData";
import { isPlayable, parseGamesSearch, parseIds, pickWheelGames, withoutUndefined } from "./-utils/gamesSearch";

type PickSearch = Pick<GamesFilterSearch, "players" | "playTime" | "category"> & {
	wheel?: number[];
	pick?: number;
	out?: number[];
};

const WHEEL_SIZE = 10;

export const Route = createFileRoute("/games/pick")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getGames()).catch(noop);
	},
	validateSearch: (search: Record<string, unknown>): PickSearch => {
		const { players, playTime, category } = parseGamesSearch(search);
		const pick = Number(search.pick);
		return {
			players,
			playTime,
			category,
			wheel: parseIds(search.wheel),
			pick: Number.isInteger(pick) && pick > 0 ? pick : undefined,
			out: parseIds(search.out),
		};
	},
});

function RouteComponent() {
	const { players, playTime, category, wheel, pick, out } = Route.useSearch();
	const { t } = useTranslation("games");
	const navigate = useNavigate({ from: Route.fullPath });
	const { games, isLoading } = useGamesData();

	const playable = useMemo(() => games.filter(isPlayable), [games]);
	const categories = useMemo(
		() => [...new Set(playable.flatMap((game) => game.categories.map((cat) => cat.name)))].sort(),
		[playable],
	);

	const pool = useMemo(() => {
		const inCategory =
			category === undefined
				? playable
				: playable.filter((game) => game.categories.some((cat) => cat.name === category));
		return filterGames(inCategory, { playerCount: players, maxPlayTime: playTime }).filter(
			(game) => !out?.includes(game.id),
		);
	}, [playable, category, players, playTime, out]);

	const wheelGames = useMemo(() => {
		if (!wheel) return undefined;
		const byId = new Map(pool.map((game) => [game.id, game]));
		const chosen = wheel.map((id) => byId.get(id));
		return chosen.every((game) => game !== undefined) ? chosen : undefined;
	}, [wheel, pool]);

	const update = (partial: Partial<PickSearch>) =>
		navigate({ search: (prev) => withoutUndefined({ ...prev, ...partial }), replace: true, resetScroll: false });

	const changeFilter = (partial: Partial<PickSearch>) => update({ ...partial, wheel: undefined, pick: undefined });

	useEffect(() => {
		if (isLoading || wheelGames || pool.length === 0) return;
		navigate({
			search: (prev) =>
				withoutUndefined({ ...prev, wheel: pickWheelGames(pool, WHEEL_SIZE).map((game) => game.id), pick: undefined }),
			replace: true,
			resetScroll: false,
		});
	}, [isLoading, wheelGames, pool, navigate]);

	const excludeGame = (gameId: number) => {
		const current = wheelGames ?? [];
		const spare = pool.filter((game) => game.id !== gameId && !current.some((onWheel) => onWheel.id === game.id));
		const [replacement] = pickWheelGames(spare, 1);
		const nextWheel = current
			.map((game) => (game.id === gameId ? replacement : game))
			.filter((game) => game !== undefined)
			.map((game) => game.id);
		update({ out: [...(out ?? []), gameId], wheel: nextWheel, pick: undefined });
	};

	const restoreExcluded = () => {
		update({ out: undefined });
		requestAnimationFrame(() => document.querySelector<HTMLElement>(PICK_ACTION_SELECTOR)?.focus());
	};

	const loosenOptions = [
		players !== undefined && { key: "players" as const, label: `${t("filters.player-count")}: ${t("filters.any")}` },
		playTime !== undefined && { key: "playTime" as const, label: `${t("filters.play-time")}: ${t("filters.any")}` },
		category !== undefined && { key: "category" as const, label: `${t("filters.category")}: ${t("filters.any")}` },
		out !== undefined && { key: "out" as const, label: t("pick.restore", { count: out.length }) },
	].filter((option) => option !== false);

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
					<BgtCard className="flex flex-col gap-3 order-last lg:order-none">
						<BgtText color="white" opacity={70} size="2">
							{t("pick.description")}
						</BgtText>
						<BgtSimpleSelect
							label={t("filters.player-count")}
							value={players ?? ANY}
							items={withAny(
								PLAYER_COUNTS.map((c) => ({ value: c, label: t("filters.player-count-value", { count: c }) })),
							)}
							onValueChange={(value) => changeFilter({ players: value === ANY ? undefined : Number(value) })}
						/>
						<BgtSimpleSelect
							label={t("filters.play-time")}
							value={playTime ?? ANY}
							items={withAny(PLAY_TIMES.map((m) => ({ value: m, label: t("filters.play-time-value", { count: m }) })))}
							onValueChange={(value) => changeFilter({ playTime: value === ANY ? undefined : Number(value) })}
						/>
						<BgtSimpleSelect
							label={t("filters.category")}
							value={category ?? ANY}
							hasSearch
							items={withAny(categories.map((name) => ({ value: name, label: name })))}
							onValueChange={(value) => changeFilter({ category: value === ANY ? undefined : String(value) })}
						/>
						<p className="text-sm font-bold text-white" aria-live="polite">
							{pool.length > WHEEL_SIZE
								? t("pick.candidates-capped", { count: WHEEL_SIZE, total: pool.length })
								: t("pick.candidates", { count: pool.length })}
						</p>
						{out && out.length > 0 && (
							<button
								type="button"
								onClick={restoreExcluded}
								className="self-start text-sm text-primary-text hover:underline cursor-pointer"
							>
								{t("pick.restore", { count: out.length })}
							</button>
						)}
					</BgtCard>
					{pool.length === 0 ? (
						<div className="flex flex-col items-center gap-3 py-12 text-center">
							<BgtText color="white" opacity={70}>
								{t("pick.none")}
							</BgtText>
							<div className="flex flex-wrap justify-center gap-2">
								{loosenOptions.map((option) => (
									<BgtButton
										key={option.key}
										variant="cancel"
										className="min-h-11"
										onClick={() => changeFilter({ [option.key]: undefined })}
									>
										{option.label}
									</BgtButton>
								))}
							</div>
						</div>
					) : (
						wheelGames && (
							<PickWheel
								games={wheelGames}
								winnerId={pick}
								onSpinStart={() => update({ pick: undefined })}
								onResult={(gameId) => update({ pick: gameId })}
								onExclude={excludeGame}
							/>
						)
					)}
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
