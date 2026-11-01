import { format } from "date-fns";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import type { Game, GameNight, Player, Settings } from "@/models";
import { getDateFnsLocale } from "@/utils/localeUtils";
import { PastGameNightItem } from "./PastGameNightItem";

export const PAST_VISIBLE = 10;

interface Props {
	gameNights: GameNight[];
	settings: Settings;
	players: Player[];
	games: Game[];
}

export const PastGameNightList = ({ gameNights, settings, players, games }: Props) => {
	const { t } = useTranslation("game-nights");
	const [showAll, setShowAll] = useState(false);
	const locale = getDateFnsLocale(settings.uiLanguage);

	const shown = showAll ? gameNights : gameNights.slice(0, PAST_VISIBLE);
	const months = new Map<string, GameNight[]>();
	for (const gameNight of shown) {
		const month = format(new Date(gameNight.startDate), "LLLL yyyy", { locale });
		months.set(month, [...(months.get(month) ?? []), gameNight]);
	}
	const olderCount = gameNights.length - shown.length;

	return (
		<div className="flex flex-col gap-4">
			{[...months].map(([month, nights]) => (
				<div key={month} className="flex flex-col gap-2">
					<h3 className="text-xs font-medium text-gray-400 first-letter:uppercase">{month}</h3>
					<ul className="flex flex-col divide-y divide-white/10 border-y border-white/10">
						{nights.map((gameNight) => (
							<li key={gameNight.id} className="py-1">
								<PastGameNightItem gameNight={gameNight} settings={settings} players={players} games={games} />
							</li>
						))}
					</ul>
				</div>
			))}
			{gameNights.length > PAST_VISIBLE && (
				<BgtButton variant="cancel" size="2" className="self-start" onClick={() => setShowAll(!showAll)}>
					{showAll ? t("past-night.show-fewer") : t("past-night.show-older", { count: olderCount })}
				</BgtButton>
			)}
		</div>
	);
};
