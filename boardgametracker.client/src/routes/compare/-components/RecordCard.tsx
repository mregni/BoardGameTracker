import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { CompareResult, CompareSide, Player } from "@/models";
import { leaderOf } from "../-utils/compareUtils";
import { FormStrip } from "./FormStrip";

interface Props {
	compare: CompareResult;
	playerOne: Player;
	playerTwo: Player;
}

export const RecordCard = ({ compare, playerOne, playerTwo }: Props) => {
	const { t } = useTranslation("compare");
	const { integer } = useNumberFormat();
	const leader = leaderOf(compare.directWins);
	const { playerOne: one, playerTwo: two } = compare.directWins;

	const score = (side: CompareSide, player: Player, wins: number) => (
		<div className={cx("flex min-w-0 flex-col", side === "playerTwo" && "items-end text-right")}>
			<span
				data-testid={`record-${side}`}
				className={cx(
					"flex items-center gap-2 text-[32px] font-bold leading-none tabular-nums sm:text-[40px]",
					side === "playerOne" ? "text-player-one" : "flex-row-reverse text-player-two",
				)}
			>
				{integer(wins)}
				{leader === side && (
					<Crown
						role="img"
						aria-label={t("record.leads", { player: player.name })}
						className="size-6 text-yellow-400"
					/>
				)}
			</span>
			<span className="text-xs text-white/60">{t("record.wins", { count: wins, player: player.name })}</span>
		</div>
	);

	return (
		<section
			aria-label={t("record.label")}
			className="flex flex-col gap-2.5 rounded-xl border border-card-border bg-card p-3.5"
		>
			<div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-end gap-3">
				{score("playerOne", playerOne, one)}
				<span className="max-w-24 pb-1.5 text-center text-xs text-white/60">
					{t("record.together", { count: compare.totalSessionsTogether })}
				</span>
				{score("playerTwo", playerTwo, two)}
			</div>
			<BgtProgressBar
				size="lg"
				gapped
				legend
				formatValue={integer}
				formatLabel={() =>
					t("record.summary", {
						count: compare.totalSessionsTogether,
						playerOne: playerOne.name,
						playerTwo: playerTwo.name,
						one: integer(one),
						two: integer(two),
						others: integer(compare.wonByOthers),
					})
				}
				segments={[
					{ key: "playerOne", label: t("record.won", { player: playerOne.name }), value: one, tone: "playerOne" },
					{ key: "others", label: t("record.someone-else"), value: compare.wonByOthers, tone: "muted" },
					{ key: "playerTwo", label: t("record.won", { player: playerTwo.name }), value: two, tone: "playerTwo" },
				]}
			/>
			<FormStrip
				sessions={compare.recentSessions}
				streak={compare.streak}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>
		</section>
	);
};
