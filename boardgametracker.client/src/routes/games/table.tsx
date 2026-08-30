import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, Link, useRouter } from "@tanstack/react-router";
import type { ColumnDef } from "@tanstack/react-table";
import { format } from "date-fns";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Refresh from "@/assets/icons/refresh.svg?react";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtBadge } from "@/components/BgtBadge/BgtBadge";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtSimpleSelect } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { type Game, GameState, QUERY_KEYS } from "@/models";
import { getWantedPricesCall } from "@/services/gameService";
import { getWantedPrices } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";
import { RoundDecimal } from "@/utils/numberUtils";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { EditableNumberCell } from "./-components/EditableNumberCell";
import { EditableSelectCell } from "./-components/EditableSelectCell";
import { useGamesData } from "./-hooks/useGamesData";
import { useInlineGameUpdate } from "./-hooks/useInlineGameUpdate";

export const Route = createFileRoute("/games/table")({
	component: RouteComponent,
});

const ANY = "any";

function RouteComponent() {
	const { t, i18n } = useTranslation(["games", "game", "common"]);
	const router = useRouter();
	const { games, isLoading } = useGamesData();
	const { updateGame } = useInlineGameUpdate();
	const settingsQuery = useQuery(getSettings());
	const currency = settingsQuery.data?.currency;
	const dateFormat = settingsQuery.data?.dateFormat;
	const changeDetectionConfigured = settingsQuery.data?.changeDetectionStatus?.isConfigured ?? false;

	const [stateFilter, setStateFilter] = useState<string>(GameState.Wanted);
	const [languageFilter, setLanguageFilter] = useState<string>(ANY);

	const queryClient = useQueryClient();
	const showLivePrices = changeDetectionConfigured && stateFilter === GameState.Wanted;
	const wantedPricesQuery = useQuery({ ...getWantedPrices(), enabled: showLivePrices });
	const priceMap = useMemo(
		() => new Map((wantedPricesQuery.data ?? []).map((price) => [price.gameId, price])),
		[wantedPricesQuery.data],
	);
	const refreshPricesMutation = useMutation({
		mutationFn: () => getWantedPricesCall(true),
		onSuccess: (data) => queryClient.setQueryData([QUERY_KEYS.wantedPrices], data),
	});

	const formatRange = useCallback((min: number | null, max: number | null, suffix = ""): string => {
		if (min == null && max == null) return "-";
		if (min != null && max != null) {
			const range = min === max ? `${min}` : `${min} - ${max}`;
			return suffix ? `${range} ${suffix}` : range;
		}
		const value = (min ?? max) as number;
		return suffix ? `${value} ${suffix}` : `${value}`;
	}, []);

	const filtered = useMemo(
		() =>
			games.filter(
				(game) =>
					(stateFilter === ANY || game.state === stateFilter) &&
					(languageFilter === ANY ||
						(languageFilter === LANGUAGE_NONE ? !game.language : game.language === languageFilter)),
			),
		[games, stateFilter, languageFilter],
	);

	const totalPrice = useMemo(() => filtered.reduce((sum, game) => sum + (game.buyingPrice ?? 0), 0), [filtered]);
	const meanPrice = filtered.length > 0 ? RoundDecimal(totalPrice / filtered.length, 0.1) : 0;

	const stateItems = useMemo(
		() => [
			{ value: ANY, label: t("games:filters.any") },
			...Object.values(GameState).map((value) => ({ value, label: t(getItemStateTranslationKey(value, false)) })),
		],
		[t],
	);
	const languageItems = useMemo(
		() => [
			{ value: ANY, label: t("games:filters.any") },
			{ value: LANGUAGE_NONE, label: t("game:language.none") },
			{ value: LANGUAGE_INDEPENDENT, label: t("game:language-independent") },
			...COMMON_LANGUAGE_CODES.map((code) => ({ value: code, label: getLanguageName(code, i18n.language) })),
		],
		[t, i18n.language],
	);

	const stateEditItems = useMemo(
		() => Object.values(GameState).map((value) => ({ value, label: t(getItemStateTranslationKey(value, false)) })),
		[t],
	);
	const languageEditItems = useMemo(
		() => [
			{ value: LANGUAGE_NONE, label: t("game:language.none") },
			{ value: LANGUAGE_INDEPENDENT, label: t("game:language-independent") },
			...COMMON_LANGUAGE_CODES.map((code) => ({ value: code, label: getLanguageName(code, i18n.language) })),
		],
		[t, i18n.language],
	);

	const columns: ColumnDef<Game>[] = useMemo(
		() => [
			{
				accessorKey: "title",
				header: t("games:columns.title"),
				enableSorting: false,
				cell: ({ row }) => (
					<Link
						to="/games/$gameId"
						params={{ gameId: row.original.id }}
						className="flex items-center gap-2 hover:text-primary"
					>
						<BgtAvatar image={row.original.image} title={row.original.title} size="small" />
						<span>{row.original.title}</span>
					</Link>
				),
			},
			{
				id: "players",
				accessorFn: (game) => game.minPlayers ?? game.maxPlayers ?? 0,
				header: t("games:columns.players"),
				cell: ({ row }) => formatRange(row.original.minPlayers, row.original.maxPlayers),
				meta: { hideOnMobile: true },
			},
			{
				id: "play-time",
				accessorFn: (game) => game.minPlayTime ?? game.maxPlayTime ?? 0,
				header: t("games:columns.play-time"),
				cell: ({ row }) => formatRange(row.original.minPlayTime, row.original.maxPlayTime, "min"),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "weight",
				header: t("games:columns.weight"),
				cell: ({ row }) => (row.original.weight != null ? (RoundDecimal(row.original.weight, 0.1) ?? "-") : "-"),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "rating",
				header: t("games:columns.rating"),
				cell: ({ row }) => (row.original.rating != null ? (RoundDecimal(row.original.rating, 0.1) ?? "-") : "-"),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "language",
				header: t("games:columns.language"),
				cell: ({ row }) => (
					<EditableSelectCell
						value={row.original.language ?? LANGUAGE_NONE}
						items={languageEditItems}
						hasSearch
						onChange={(language) =>
							updateGame({ ...row.original, language: language === LANGUAGE_NONE ? null : language })
						}
					/>
				),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "state",
				header: t("games:columns.state"),
				cell: ({ row }) => (
					<EditableSelectCell
						value={row.original.state}
						items={stateEditItems}
						onChange={(state) => updateGame({ ...row.original, state: state as GameState })}
					/>
				),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "additionDate",
				header: t("games:columns.added"),
				cell: ({ row }) =>
					row.original.additionDate && dateFormat ? format(new Date(row.original.additionDate), dateFormat) : "-",
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "buyingPrice",
				header: t("games:columns.price"),
				cell: ({ row }) => (
					<EditableNumberCell
						value={row.original.buyingPrice}
						step={0.01}
						min={0}
						prefix={currency}
						onChange={(buyingPrice) => updateGame({ ...row.original, buyingPrice })}
					/>
				),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "shopUrl",
				header: t("games:columns.shop"),
				enableSorting: false,
				cell: ({ row }) => {
					const url = SafeHttpUrl(row.original.shopUrl);
					return url ? (
						<a
							href={url}
							target="_blank"
							rel="noopener noreferrer"
							className="inline-flex items-center gap-1 text-primary hover:text-primary/80"
						>
							<SquareOutIcon className="size-4" />
							{t("games:columns.shop")}
						</a>
					) : (
						"-"
					);
				},
			},
			{
				accessorKey: "bggId",
				header: t("games:columns.bgg"),
				enableSorting: false,
				cell: ({ row }) =>
					row.original.bggId ? (
						<a
							href={`https://boardgamegeek.com/boardgame/${row.original.bggId}`}
							target="_blank"
							rel="noopener noreferrer"
							className="inline-flex items-center gap-1 text-primary hover:text-primary/80"
						>
							<SquareOutIcon className="size-4" />
							{t("games:columns.bgg")}
						</a>
					) : (
						"-"
					),
			},
			...(showLivePrices
				? [
						{
							id: "current-price",
							header: t("games:columns.current-price"),
							enableSorting: false,
							cell: ({ row }: { row: { original: Game } }) => {
								const livePrice = priceMap.get(row.original.id);
								if (!livePrice?.available || livePrice.price == null) return "-";
								return `${currency ?? ""}${RoundDecimal(livePrice.price, 0.01)}`;
							},
							meta: { hideOnMobile: true },
						},
						{
							id: "in-stock",
							header: t("games:columns.in-stock"),
							enableSorting: false,
							cell: ({ row }: { row: { original: Game } }) => {
								const livePrice = priceMap.get(row.original.id);
								if (!livePrice?.available || livePrice.inStock == null) return "-";
								return (
									<BgtBadge color={livePrice.inStock ? "green" : "red"} variant="soft">
										{livePrice.inStock ? t("games:in-stock.yes") : t("games:in-stock.no")}
									</BgtBadge>
								);
							},
							meta: { hideOnMobile: true },
						},
					]
				: []),
		],
		[t, currency, dateFormat, formatRange, updateGame, stateEditItems, languageEditItems, showLivePrices, priceMap],
	);

	const columnWidths: (string | null)[] = [null, null, null, null, null, "w-52", "w-52"];

	return (
		<BgtPage>
			<BgtPageHeader header={t("games:table.title")} backAction={() => router.history.back()} />
			<BgtPageContent>
				<div className="flex flex-col md:flex-row md:flex-wrap md:items-center gap-2">
					<BgtSimpleSelect
						className="w-full md:w-44"
						value={stateFilter}
						items={stateItems}
						onValueChange={(value) => setStateFilter(String(value))}
					/>
					<BgtSimpleSelect
						className="w-full md:w-44"
						value={languageFilter}
						items={languageItems}
						onValueChange={(value) => setLanguageFilter(String(value))}
					/>
					{showLivePrices && (
						<BgtButton
							variant="cancel"
							className="w-full md:w-auto md:ml-auto"
							disabled={refreshPricesMutation.isPending}
							onClick={() => refreshPricesMutation.mutate()}
						>
							<Refresh className={refreshPricesMutation.isPending ? "size-4 animate-spin" : "size-4"} />
							{t("games:refresh-prices")}
						</BgtButton>
					)}
				</div>
				<div className="grid grid-cols-2 lg:grid-cols-3 gap-3 xl:gap-6">
					<BgtTextStatistic title={t("games:table.total-games")} content={filtered.length} />
					<BgtTextStatistic
						title={t("games:table.total-price")}
						content={RoundDecimal(totalPrice, 0.1)}
						prefix={currency}
					/>
					<BgtTextStatistic title={t("games:table.mean-price")} content={meanPrice} prefix={currency} />
				</div>
				<BgtDataTable
					columns={columns}
					data={filtered}
					isLoading={isLoading}
					noDataMessage={t("games:table.empty")}
					widths={columnWidths}
				/>
			</BgtPageContent>
		</BgtPage>
	);
}
