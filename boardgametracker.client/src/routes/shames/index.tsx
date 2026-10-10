import { createFileRoute, redirect } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Coins from "@/assets/icons/coins.svg?react";
import Game from "@/assets/icons/gamepad.svg?react";
import { BgtCardList } from "@/components/BgtLayout/BgtCardList";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { getShameStatistics, getShames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { formatPrice } from "@/utils/priceUtils";
import { NoShames } from "./-components/NoShames";
import { ShameGame } from "./-components/ShameGame";
import { useShameData } from "./-hooks/useShameData";

export const Route = createFileRoute("/shames/")({
	component: RouteComponent,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.ensureQueryData(getSettings());
		if (!settings.shelfOfShameEnabled) {
			throw redirect({ to: "/" });
		}
	},
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getShames());
		queryClient.prefetchQuery(getShameStatistics());
	},
});

function RouteComponent() {
	const { t } = useTranslation(["shames", "common"]);
	const { shames, statistics, settings, isLoading } = useShameData();

	return (
		<BgtPage>
			<BgtPageHeader header={t("common:shame")} icon={Game} />
			<BgtPageContent isLoading={isLoading} data={{ statistics, shames, settings }}>
				{({ statistics, shames, settings }) => (
					<>
						<div className="grid grid-cols-1 lg:grid-cols-3 gap-3 xl:gap-6">
							<BgtTextStatistic content={statistics.count} title={t("total-shames")} icon={<Game />} />
							<BgtTextStatistic
								content={formatPrice(statistics.totalValue, settings.currency, settings.uiLanguage)}
								title={t("total-value")}
								icon={<Coins />}
							/>
							<BgtTextStatistic
								content={formatPrice(statistics.averageValue, settings.currency, settings.uiLanguage)}
								title={t("average-value-priced", { count: statistics.pricedGameCount })}
								icon={<Coins />}
							/>
						</div>
						{shames.length === 0 && <NoShames />}
						{shames.length !== 0 && (
							<BgtCardList>
								{shames.map((shame) => (
									<ShameGame
										key={shame.id}
										shame={shame}
										dateFormat={settings.dateFormat}
										currency={settings.currency}
									/>
								))}
							</BgtCardList>
						)}
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
