import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { BgtProgressDots } from "@/components/BgtProgressBar/BgtProgressDots";
import { BgtProgressLegend } from "@/components/BgtProgressBar/BgtProgressLegend";
import type { GameNightReference, NightAttendance } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { StatRow, StatsCard, StatsEmpty, type StatsLookup } from "./StatsParts";

interface Props {
	attendance: NightAttendance[];
	recentNights: GameNightReference[];
	nights: number;
	lookup: StatsLookup;
}

export const NightAttendanceCard = ({ attendance, recentNights, nights, lookup }: Props) => {
	const { t } = useTranslation("game-nights");
	const [selectedId, setSelectedId] = useState<number | null>(null);
	const selected = attendance.find((entry) => entry.playerId === selectedId) ?? attendance[0];

	return (
		<StatsCard
			id="night-attendance"
			title={t("stats.attendance.title")}
			subtitle={t("stats.attendance.of", { count: nights })}
		>
			{selected === undefined ? (
				<StatsEmpty>{t("stats.attendance.empty")}</StatsEmpty>
			) : (
				<>
					<div className="flex flex-col gap-1">
						{attendance.map((entry) => {
							const player = lookup.players.get(entry.playerId);
							const name = player?.name ?? "";
							return (
								<StatRow
									key={entry.playerId}
									avatar={<BgtAvatar title={name} image={player?.image} size="medium" shape="round" />}
									name={name}
									detail={
										entry.nightsHosted > 0 ? t("stats.attendance.hosted", { count: entry.nightsHosted }) : undefined
									}
									bar={
										<BgtProgressBar
											value={entry.nightsAttended}
											max={nights}
											size="thin"
											aria-label={t("stats.attendance.bar", { name, count: entry.nightsAttended, total: nights })}
										/>
									}
									value={entry.nightsAttended}
									pressed={entry.playerId === selected.playerId}
									label={t("stats.attendance.select", { name })}
									onClick={() => setSelectedId(entry.playerId)}
								/>
							);
						})}
					</div>
					<div className="flex flex-col gap-1">
						<span className="text-xs font-semibold text-gray-400">
							{t("stats.attendance.recent", { count: recentNights.length })}
						</span>
						<BgtProgressDots
							items={recentNights.map((night, index) => {
								const came = selected.recentNights[index] === true;
								return {
									key: String(night.gameNightId),
									state: came ? "on" : "off",
									label: t("stats.attendance.dot", {
										title: night.title,
										date: toDisplay(night.startDate, lookup.dateFormat, lookup.uiLanguage),
										state: came ? t("stats.attendance.came") : t("stats.attendance.absent"),
									}),
								};
							})}
						/>
						<BgtProgressLegend
							items={[
								{ key: "came", label: t("stats.attendance.came"), tone: "ok" },
								{ key: "absent", label: t("stats.attendance.absent"), tone: "empty" },
							]}
						/>
					</div>
				</>
			)}
		</StatsCard>
	);
};
