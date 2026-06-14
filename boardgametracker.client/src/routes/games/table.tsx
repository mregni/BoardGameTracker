import { useQuery } from "@tanstack/react-query";
import { createFileRoute, Link, useRouter } from "@tanstack/react-router";
import type { ColumnDef } from "@tanstack/react-table";
import { format } from "date-fns";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import List from "@/assets/icons/list.svg?react";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtSimpleSelect } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { type Game, GameState } from "@/models";
import { getSettings } from "@/services/queries/settings";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";
import { RoundDecimal } from "@/utils/numberUtils";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { useGamesData } from "./-hooks/useGamesData";

export const Route = createFileRoute("/games/table")({
	component: RouteComponent,
});

const ANY = "any";

function RouteComponent() {
	const { t, i18n } = useTranslation(["games", "game", "common"]);
	const router = useRouter();
	const { games, isLoading } = useGamesData();
	const settingsQuery = useQuery(getSettings());
	const currency = settingsQuery.data?.currency;
	const dateFormat = settingsQuery.data?.dateFormat;

	const [stateFilter, setStateFilter] = useState<string>(GameState.Wanted);
	const [languageFilter, setLanguageFilter] = useState<string>(ANY);

	const languageLabel = useCallback(
		(code: string | null) => {
			if (!code) return "-";
			return code === LANGUAGE_INDEPENDENT ? t("game:language-independent") : getLanguageName(code, i18n.language);
		},
		[t, i18n.language],
	);

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
				cell: ({ row }) => languageLabel(row.original.language),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "state",
				header: t("games:columns.state"),
				cell: ({ row }) => t(getItemStateTranslationKey(row.original.state, row.original.isLoaned)),
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
				cell: ({ row }) => (row.original.buyingPrice != null ? `${currency} ${row.original.buyingPrice}` : "-"),
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
		],
		[t, currency, dateFormat, languageLabel, formatRange],
	);

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("games:table.title")}
				icon={List}
				backAction={() => router.history.back()}
				backText={t("games:back")}
			/>
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
				<BgtDataTable columns={columns} data={filtered} isLoading={isLoading} noDataMessage={t("games:table.empty")} />
			</BgtPageContent>
		</BgtPage>
	);
}
