import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import ClockIcon from "@/assets/icons/clock.svg?react";
import HistoryIcon from "@/assets/icons/history.svg?react";
import ListIcon from "@/assets/icons/list.svg?react";
import UsersIcon from "@/assets/icons/users.svg?react";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { GameNightRecord, GameNightRecords } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { StatFact, StatsCard, StatsEmpty, type StatsLookup } from "./StatsParts";

interface Props {
	records: GameNightRecords;
	lookup: StatsLookup;
}

export const NightRecordsCard = ({ records, lookup }: Props) => {
	const { t } = useTranslation("game-nights");
	const numbers = useNumberFormat();
	const { longestNight, mostGames, biggestTable, latestFinish } = records;
	const duration = useShortDuration();
	const gameTitle = (id: number | null) => (id === null ? undefined : lookup.games.get(id)?.title);
	const night = (record: GameNightRecord) =>
		t("stats.records.night", {
			title: record.title,
			date: toDisplay(record.startDate, lookup.dateFormat, lookup.uiLanguage),
		});

	const mostlyPlayed = mostGames ? gameTitle(mostGames.gameId) : undefined;
	const latestGame = latestFinish ? gameTitle(latestFinish.gameId) : undefined;

	return (
		<StatsCard id="night-records" title={t("stats.records.title")}>
			{longestNight || mostGames || biggestTable || latestFinish ? (
				<div className="flex flex-col gap-2">
					{longestNight && (
						<StatFact
							icon={<ClockIcon />}
							title={t("stats.records.longest-night")}
							value={duration(longestNight.value)}
							hint={[
								t("stats.records.night-length"),
								night(longestNight),
								t("stats.records.games", { count: longestNight.gamesPlayed }),
							].join(" · ")}
						/>
					)}
					{mostGames && (
						<StatFact
							icon={<ListIcon />}
							title={t("stats.records.most-games")}
							value={numbers.integer(mostGames.value)}
							hint={[night(mostGames), mostlyPlayed && t("stats.records.mostly", { name: mostlyPlayed })]
								.filter(Boolean)
								.join(" · ")}
						/>
					)}
					{biggestTable && (
						<StatFact
							icon={<UsersIcon />}
							title={t("stats.records.biggest-table")}
							value={numbers.integer(biggestTable.value)}
							hint={night(biggestTable)}
						/>
					)}
					{latestFinish?.at && (
						<StatFact
							icon={<HistoryIcon />}
							title={t("stats.records.latest-finish")}
							value={format(latestFinish.at, lookup.timeFormat)}
							hint={t("stats.records.finish", {
								title: latestFinish.title,
								game: latestGame ?? "",
								duration: duration(latestFinish.value),
							})}
						/>
					)}
				</div>
			) : (
				<StatsEmpty>{t("stats.records.empty")}</StatsEmpty>
			)}
		</StatsCard>
	);
};
