import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtProgressBar, type BgtProgressSegment } from "@/components/BgtProgressBar/BgtProgressBar";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { GameState, type GameStateChart, type RecentGame } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { cardLinkClasses } from "./dashboardStyles";

interface Props {
	collection: GameStateChart[];
	ownedGames: number;
	totalCollectionValue: number | null;
	pricedGameCount: number;
	recentAddedGames: RecentGame[];
	currency: string;
	uiLanguage: string;
	dateFormat: string;
}

const stateOrder: { state: GameState; tone: BgtProgressSegment["tone"] }[] = [
	{ state: GameState.Owned, tone: "value" },
	{ state: GameState.Wanted, tone: "primary" },
	{ state: GameState.ForTrade, tone: "warn" },
	{ state: GameState.PreviouslyOwned, tone: "muted" },
];

export const CollectionCard = (props: Props) => {
	const {
		collection,
		ownedGames,
		totalCollectionValue,
		pricedGameCount,
		recentAddedGames,
		currency,
		uiLanguage,
		dateFormat,
	} = props;
	const { t } = useTranslation(["dashboard", "game"]);
	const { integer, price } = useNumberFormat();

	const segments: BgtProgressSegment[] = stateOrder.map(({ state, tone }) => ({
		key: state,
		label: t(getItemStateTranslationKey(state, false)),
		value: collection.find((item) => item.type === state)?.gameCount ?? 0,
		tone,
	}));
	const total = segments.reduce((sum, segment) => sum + segment.value, 0);

	return (
		<BgtCard className="gap-3">
			<section aria-labelledby="dashboard-collection" className="flex flex-col gap-3">
				<div className="flex items-center justify-between gap-2">
					<h2 id="dashboard-collection" className="font-semibold text-white">
						{t("collection.title")}
					</h2>
					<Link to="/games" className={cardLinkClasses}>
						{t("collection.games")}
						<CaretRight className="size-3.5" aria-hidden="true" />
					</Link>
				</div>
				{total > 0 && (
					<BgtProgressBar
						size="lg"
						gapped
						legend
						segments={segments}
						formatValue={integer}
						formatLabel={(summary) =>
							t("collection.summary", {
								count: summary.total,
								total: integer(summary.total),
								parts: summary.segments
									.filter((segment) => segment.value > 0)
									.map((segment) => `${segment.label} ${integer(segment.value)}`)
									.join(", "),
							})
						}
					/>
				)}
				<div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1 text-sm">
					<span className="text-white/60">{t("collection.paid")}</span>
					{totalCollectionValue === null ? (
						<span className="text-white/60">{t("collection.no-prices")}</span>
					) : (
						<span>
							<strong className="text-card-value">{price(totalCollectionValue, currency)}</strong>{" "}
							<span className="text-white/60">
								· {t("collection.priced", { priced: integer(pricedGameCount), owned: integer(ownedGames) })}
							</span>
						</span>
					)}
				</div>
				{recentAddedGames.length > 0 && (
					<div className="flex flex-col gap-1.5">
						<h3 className="text-xs uppercase tracking-wider text-white/50">{t("collection.recently-added")}</h3>
						<ul className="flex flex-wrap gap-1.5">
							{recentAddedGames.map((game) => (
								<li key={game.id}>
									<Link to="/games/$gameId" params={{ gameId: game.id }} className="hover:opacity-80">
										<AvatarChip title={game.title} image={game.image}>
											{game.additionDate && (
												<span className="text-white/60">· {toDisplay(game.additionDate, dateFormat, uiLanguage)}</span>
											)}
										</AvatarChip>
									</Link>
								</li>
							))}
						</ul>
					</div>
				)}
			</section>
		</BgtCard>
	);
};
