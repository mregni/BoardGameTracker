import { noop } from "@tanstack/react-query";
import { createFileRoute, Link, redirect } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import ArchiveIcon from "@/assets/icons/archive.svg?react";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { getShameStatistics, getShames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { MoneyOnTheShelf } from "./-components/MoneyOnTheShelf";
import { NoShames } from "./-components/NoShames";
import { ShameFilters } from "./-components/ShameFilters";
import { ShameRow, shameGridClasses } from "./-components/ShameRow";
import { ShameTiles } from "./-components/ShameTiles";
import { useShameData } from "./-hooks/useShameData";
import { countByShow, DEFAULT_SHAME_FILTERS, filterShames, monthOptions } from "./-utils/shameFilters";

export const Route = createFileRoute("/shames/")({
	component: RouteComponent,
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

function RouteComponent() {
	const { t } = useTranslation(["shames", "common"]);
	const { canWrite, canManageSettings } = usePermissions();
	const [months, setMonths] = useState<number | undefined>(undefined);
	const [filters, setFilters] = useState(DEFAULT_SHAME_FILTERS);
	const { shames, statistics, settings, isLoading, plan, planningId } = useShameData(months);

	const visible = useMemo(() => filterShames(shames, filters), [shames, filters]);
	const counts = useMemo(() => countByShow(shames), [shames]);

	return (
		<BgtPage>
			<BgtPageHeader header={t("common:shame")} icon={ArchiveIcon} />
			<BgtPageContent isLoading={isLoading} data={{ statistics, settings }}>
				{({ statistics, settings }) => {
					const configuredMonths = settings.shelfOfShameMonthsLimit;
					const nextNight = statistics.nextGameNight;
					const showNoNight = canWrite && settings.gameNightsEnabled && !nextNight && shames.length > 0;
					const hasOwnedGames = statistics.ownedCount > 0;

					return (
						<>
							<p className="-mt-2 text-sm text-white/60">
								{t("subtitle", { count: configuredMonths })}
								{canManageSettings && (
									<>
										{" · "}
										<Link to="/settings" search={{ tab: "shelf-of-shame" }} className="text-primary hover:underline">
											{t("change-in-settings")}
										</Link>
									</>
								)}
							</p>
							{hasOwnedGames && (
								<>
									<ShameTiles
										statistics={statistics}
										shames={shames}
										currency={settings.currency}
										dateFormat={settings.dateFormat}
									/>
									<ShameFilters
										filters={filters}
										counts={counts}
										months={statistics.months}
										monthChoices={monthOptions(configuredMonths)}
										onFiltersChange={setFilters}
										onMonthsChange={setMonths}
									/>
								</>
							)}
							{showNoNight && (
								<p className="rounded-lg border border-dashed border-primary/30 px-3 py-2 text-sm text-white/60">
									{t("plan.no-night")}{" "}
									<Link to="/game-nights" className="font-semibold text-primary hover:underline">
										{t("plan.plan-night")}
									</Link>
								</p>
							)}
							{shames.length === 0 && <NoShames />}
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
												onPlan={plan}
											/>
										))}
									</ul>
								</div>
							)}
							{hasOwnedGames && (
								<MoneyOnTheShelf
									statistics={statistics}
									shames={shames}
									currency={settings.currency}
									onSortByPrice={() => setFilters((current) => ({ ...current, sort: "price" }))}
								/>
							)}
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
