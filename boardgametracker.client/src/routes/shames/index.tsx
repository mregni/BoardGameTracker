import { noop } from "@tanstack/react-query";
import { createFileRoute, Link, redirect, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import ArchiveIcon from "@/assets/icons/archive.svg?react";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { getShameStatistics, getShames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { GameNightNudge } from "./-components/GameNightNudge";
import { MoneyOnTheShelf } from "./-components/MoneyOnTheShelf";
import { NoShames } from "./-components/NoShames";
import { ShameFilters } from "./-components/ShameFilters";
import { ShameMonths } from "./-components/ShameMonths";
import { ShameRow, shameGridClasses } from "./-components/ShameRow";
import { ShameSummary } from "./-components/ShameSummary";
import { useShameData } from "./-hooks/useShameData";
import {
	countByShow,
	DEFAULT_SHAME_FILTERS,
	filterShames,
	isNeverPlayed,
	monthOptions,
	SHAME_OWNED,
	SHAME_SHOWS,
	SHAME_SORTS,
	type ShameFilterState,
} from "./-utils/shameFilters";

const shamesSearchSchema = z.object({
	show: z.enum(SHAME_SHOWS).optional().catch(undefined),
	owned: z.enum(SHAME_OWNED).optional().catch(undefined),
	sort: z.enum(SHAME_SORTS).optional().catch(undefined),
	months: z.number().int().positive().optional().catch(undefined),
});

export const Route = createFileRoute("/shames/")({
	component: RouteComponent,
	validateSearch: shamesSearchSchema,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.query({ ...getSettings(), staleTime: "static" });
		if (!settings.shelfOfShameEnabled) {
			throw redirect({ to: "/" });
		}
	},
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getShames()).catch(noop);
		queryClient.query(getShameStatistics()).catch(noop);
	},
});

const withoutDefaults = (filters: ShameFilterState) => ({
	show: filters.show === DEFAULT_SHAME_FILTERS.show ? undefined : filters.show,
	owned: filters.owned === DEFAULT_SHAME_FILTERS.owned ? undefined : filters.owned,
	sort: filters.sort === DEFAULT_SHAME_FILTERS.sort ? undefined : filters.sort,
});

function RouteComponent() {
	const { t } = useTranslation(["shames", "common"]);
	const { canWrite, canManageSettings } = usePermissions();
	const search = Route.useSearch();
	const navigate = useNavigate({ from: "/shames/" });
	const [filtersOpen, setFiltersOpen] = useState(false);
	const filters: ShameFilterState = {
		show: search.show ?? DEFAULT_SHAME_FILTERS.show,
		owned: search.owned ?? DEFAULT_SHAME_FILTERS.owned,
		sort: search.sort ?? DEFAULT_SHAME_FILTERS.sort,
	};
	const { shames, statistics, settings, isLoading, plan, planningId, unplan, unplanningId } = useShameData(
		search.months,
	);

	const setFilters = (next: ShameFilterState) =>
		navigate({ search: (current) => ({ ...current, ...withoutDefaults(next) }), replace: true });
	const setMonths = (months: number) =>
		navigate({
			search: (current) => ({
				...current,
				months: months === settings?.shelfOfShameMonthsLimit ? undefined : months,
			}),
			replace: true,
		});

	const visible = filterShames(shames, filters);
	const counts = useMemo(() => countByShow(shames), [shames]);
	const allNeverPlayed = visible.length > 0 && visible.every(isNeverPlayed);
	const activeFilters = Object.values(withoutDefaults(filters)).filter(Boolean).length;

	return (
		<BgtPage>
			<BgtPageHeader header={t("common:shame")} icon={ArchiveIcon} />
			<BgtPageContent isLoading={isLoading} data={{ statistics, settings }}>
				{({ statistics, settings }) => {
					const configuredMonths = settings.shelfOfShameMonthsLimit;
					const nextNight = statistics.nextGameNight;
					const hasOwnedGames = statistics.ownedCount > 0;

					return (
						<>
							<div className="-mt-2 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-white/60">
								{hasOwnedGames ? (
									<ShameMonths
										months={statistics.months}
										monthChoices={monthOptions(configuredMonths)}
										onMonthsChange={setMonths}
									/>
								) : (
									<span>{t("subtitle", { count: configuredMonths })}</span>
								)}
								{canManageSettings && (
									<>
										<span aria-hidden="true" className="hidden sm:inline">
											·
										</span>
										<Link
											to="/settings"
											search={{ tab: "shelf-of-shame" }}
											className="inline-flex min-h-10 items-center text-primary-text hover:underline md:min-h-0"
										>
											{t("change-in-settings")}
										</Link>
									</>
								)}
							</div>
							{shames.length > 0 && (
								<>
									<ShameSummary statistics={statistics} shames={shames} />
									<MoneyOnTheShelf
										statistics={statistics}
										shames={shames}
										currency={settings.currency}
										canAddPrices={canWrite}
										onSortByPrice={() => setFilters({ ...filters, sort: "price" })}
									/>
								</>
							)}
							{shames.length > 0 && (
								<GameNightNudge
									statistics={statistics}
									shames={shames}
									dateFormat={settings.dateFormat}
									canPlan={canWrite && settings.gameNightsEnabled}
								/>
							)}
							{shames.length === 0 && <NoShames />}
							{shames.length > 0 && (
								<div className="flex flex-col gap-3">
									<div className="flex items-center justify-between gap-3 md:hidden">
										<p aria-live="polite" className="text-sm text-white/60">
											{t("list.count", { count: visible.length, total: shames.length })}
										</p>
										<button
											type="button"
											aria-expanded={filtersOpen}
											aria-controls="shame-filters"
											onClick={() => setFiltersOpen((open) => !open)}
											className="inline-flex min-h-10 items-center rounded-md border border-white/15 px-3 text-sm font-semibold text-white"
										>
											{activeFilters > 0 ? t("filters.toggle-active", { count: activeFilters }) : t("filters.toggle")}
										</button>
									</div>
									<div id="shame-filters" className={cx(!filtersOpen && "hidden", "md:block")}>
										<ShameFilters filters={filters} counts={counts} onFiltersChange={setFilters} />
									</div>
									<p aria-live="polite" className="hidden text-sm text-white/60 md:block">
										{t("list.count", { count: visible.length, total: shames.length })}
									</p>
								</div>
							)}
							{shames.length > 0 && visible.length === 0 && (
								<p className="rounded-lg border border-card-border bg-card px-3 py-6 text-center text-sm text-white/60">
									{t("no-matches")}
								</p>
							)}
							{visible.length > 0 && (
								<div className="flex flex-col gap-1.5">
									<div
										aria-hidden="true"
										className={cx(
											shameGridClasses,
											"hidden px-3 text-[11px] tracking-widest text-white/50 uppercase md:grid",
										)}
									>
										<span />
										<span>{t("list.game")}</span>
										<span>{t("list.last-played")}</span>
										<span className="text-right">{t("list.paid")}</span>
										<span />
									</div>
									<ul aria-label={t("list.label")} className="flex flex-col gap-1.5">
										{visible.map((shame) => (
											<ShameRow
												key={shame.id}
												shame={shame}
												currency={settings.currency}
												dateFormat={settings.dateFormat}
												canWrite={canWrite}
												nextNight={nextNight}
												planning={planningId === shame.id}
												unplanning={unplanningId === shame.id}
												quietNeverPlayed={allNeverPlayed}
												onPlan={plan}
												onUnplan={unplan}
											/>
										))}
									</ul>
								</div>
							)}
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
