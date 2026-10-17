import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import Crown from "@/assets/icons/crown.svg?react";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { CompareResult, CompareRow, CompareSide, Player } from "@/models";
import { leaderOf } from "../-utils/compareUtils";

interface Props {
	compare: CompareResult;
	playerOne: Player;
	playerTwo: Player;
}

export const OverallTable = ({ compare, playerOne, playerTwo }: Props) => {
	const { t } = useTranslation("compare");
	const { integer, percent } = useNumberFormat();
	const duration = useShortDuration();

	const rows: { key: string; label: string; values: CompareRow<number>; format: (value: number) => string }[] = [
		{ key: "wins", label: t("overall.wins"), values: compare.winCount, format: integer },
		{ key: "win-rate", label: t("overall.win-rate"), values: compare.winPercentage, format: (value) => percent(value) },
		{ key: "games", label: t("overall.games"), values: compare.sessionCounts, format: integer },
		{ key: "play-time", label: t("overall.play-time"), values: compare.totalDuration, format: duration },
	];

	const cell = (side: CompareSide, row: (typeof rows)[number]) => {
		const higher = leaderOf(row.values) === side;
		return (
			<span
				data-higher={higher ? "true" : undefined}
				className={cx(
					"flex items-center gap-1 font-bold tabular-nums",
					side === "playerTwo" && "flex-row-reverse",
					higher ? "text-card-value" : "text-white/60",
				)}
			>
				{row.format(row.values[side])}
				{higher && (
					<>
						<Crown aria-hidden="true" className="size-3.5 text-yellow-400" />
						<span className="sr-only">{t("overall.higher")}</span>
					</>
				)}
			</span>
		);
	};

	return (
		<details className="group rounded-xl border border-card-border bg-card">
			<summary className="flex cursor-pointer list-none items-center justify-between gap-2.5 px-3.5 py-3 font-bold [&::-webkit-details-marker]:hidden">
				<span>{t("overall.title")}</span>
				<CaretRight aria-hidden="true" className="size-4 text-white/60 transition-transform group-open:rotate-90" />
			</summary>
			<table className="w-full">
				<thead className="sr-only">
					<tr>
						<th scope="col">{playerOne.name}</th>
						<th scope="col">{t("overall.stat")}</th>
						<th scope="col">{playerTwo.name}</th>
					</tr>
				</thead>
				<tbody>
					{rows.map((row) => (
						<tr
							key={row.key}
							data-testid="overall-row"
							className="grid grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)_minmax(0,1fr)] items-center gap-2.5 border-t border-card-border px-3.5 py-2"
						>
							<td>{cell("playerOne", row)}</td>
							<th scope="row" className="text-center text-xs font-normal text-white/60">
								{row.label}
							</th>
							<td>{cell("playerTwo", row)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</details>
	);
};
