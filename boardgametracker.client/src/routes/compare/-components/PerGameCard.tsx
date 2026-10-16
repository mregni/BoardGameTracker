import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { CompareGame, CompareOutcome, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { toDisplay } from "@/utils/dateUtils";
import { type GameSort, initialOf, sortGames } from "../-utils/compareUtils";

export const VISIBLE_GAMES = 5;

const sorts: GameSort[] = ["most-played", "biggest-lead"];

interface Props {
	games: CompareGame[];
	playerOne: Player;
	playerTwo: Player;
	dateFormat: string;
	uiLanguage: string;
}

export const PerGameCard = ({ games, playerOne, playerTwo, dateFormat, uiLanguage }: Props) => {
	const { t } = useTranslation("compare");
	const { gameById } = useGameById();
	const { integer, decimal } = useNumberFormat();
	const [sort, setSort] = useState<GameSort>("most-played");
	const [showAll, setShowAll] = useState(false);

	const sorted = useMemo(() => sortGames(games, sort), [games, sort]);
	const visible = showAll ? sorted : sorted.slice(0, VISIBLE_GAMES);

	const letter = (outcome: CompareOutcome) =>
		({
			playerOne: initialOf(playerOne.name),
			playerTwo: initialOf(playerTwo.name),
			both: "+",
			other: "–",
			sameTeam: "=",
		})[outcome];
	const average = (value: number | null) => (value === null ? "–" : decimal(value, 1));

	if (games.length === 0) {
		return null;
	}

	return (
		<section
			aria-labelledby="per-game-title"
			className="flex flex-col gap-1.5 rounded-xl border border-card-border bg-card p-3.5"
		>
			<div className="flex flex-wrap items-center justify-between gap-2">
				<h2 id="per-game-title" className="font-bold">
					{t("per-game.title")}
				</h2>
				<fieldset className="flex gap-0.5 rounded-lg border border-card-border bg-background p-0.5">
					<legend className="sr-only">{t("per-game.sort")}</legend>
					{sorts.map((option) => (
						<button
							key={option}
							type="button"
							aria-pressed={sort === option}
							onClick={() => setSort(option)}
							className={cx(
								"cursor-pointer rounded-md border px-2.5 py-1 text-xs font-semibold",
								sort === option
									? "border-primary bg-primary/15 text-white"
									: "border-transparent text-white/60 hover:text-white",
							)}
						>
							{t(`per-game.${option}`)}
						</button>
					))}
				</fieldset>
			</div>
			<div className="flex justify-between text-xs font-semibold" aria-hidden="true">
				<span className="text-player-one">■ {playerOne.name}</span>
				<span className="text-player-two">{playerTwo.name} ■</span>
			</div>
			<div
				aria-hidden="true"
				className="hidden grid-cols-[28px_minmax(0,1fr)_minmax(110px,1.2fr)_auto] gap-3 px-2 text-[11px] uppercase tracking-wider text-white/60 sm:grid"
			>
				<span />
				<span>{t("per-game.game")}</span>
				<span>{t("per-game.wins")}</span>
				<span className="text-right">{t("per-game.last")}</span>
			</div>
			<ul className="flex flex-col">
				{visible.map((game) => {
					const details = gameById(game.gameId);
					const title = details?.title ?? "";
					const hasScores = game.playerOneAverageScore !== null || game.playerTwoAverageScore !== null;
					const coopOnly = game.sameTeamCount > 0 && game.sameTeamCount === game.sessionCount;
					const teamText = t("per-game.same-team", { won: game.sameTeamWins, count: game.sameTeamCount });
					return (
						<li
							key={game.gameId}
							data-testid="per-game-row"
							className="grid grid-cols-[28px_minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1 border-t border-card-border px-0.5 py-2 first:border-t-0 sm:grid-cols-[28px_minmax(0,1fr)_minmax(110px,1.2fr)_auto] sm:px-2"
						>
							<span className="row-span-2 self-start sm:row-span-1 sm:self-center">
								<BgtAvatar image={details?.image} title={title} size="medium" />
							</span>
							<span className="flex min-w-0 flex-col">
								<strong className="truncate">{title}</strong>
								<small className="truncate text-[11px] text-white/60">
									{t("games", { count: game.sessionCount })} ·{" "}
									{coopOnly
										? t("per-game.coop")
										: hasScores
											? t("per-game.average", {
													one: average(game.playerOneAverageScore),
													two: average(game.playerTwoAverageScore),
												})
											: t("per-game.no-scores")}
								</small>
							</span>
							<span className="col-span-2 col-start-2 row-start-2 flex min-w-0 flex-col gap-1 sm:col-span-1 sm:col-start-3 sm:row-start-1">
								{coopOnly ? (
									<span className="text-xs text-white/60">{teamText}</span>
								) : (
									<span className="flex justify-between gap-2 text-xs font-bold tabular-nums">
										<span className="text-player-one">{integer(game.playerOneWins)}</span>
										{(game.otherWins > 0 || game.sameTeamCount > 0) && (
											<span className="truncate font-normal text-white/60">
												{[
													game.otherWins > 0 && t("per-game.others", { count: game.otherWins }),
													game.sameTeamCount > 0 && t("per-game.same-team-extra", { count: game.sameTeamCount }),
												]
													.filter(Boolean)
													.join(" · ")}
											</span>
										)}
										<span className="text-player-two">{integer(game.playerTwoWins)}</span>
									</span>
								)}
								<BgtProgressBar
									size="thin"
									animateOnMount
									gapped
									formatValue={integer}
									formatLabel={() =>
										coopOnly
											? `${title}: ${teamText}`
											: t("per-game.row", {
													game: title,
													playerOne: playerOne.name,
													playerTwo: playerTwo.name,
													one: integer(game.playerOneWins),
													two: integer(game.playerTwoWins),
													count: game.otherWins,
												})
									}
									segments={[
										{ key: "playerOne", label: playerOne.name, value: game.playerOneWins, tone: "playerOne" },
										{ key: "others", label: t("record.someone-else"), value: game.otherWins, tone: "muted" },
										{
											key: "sameTeam",
											label: t("record.same-team"),
											value: game.sameTeamCount,
											tone: "subtle",
											pattern: "hatch",
										},
										{ key: "playerTwo", label: playerTwo.name, value: game.playerTwoWins, tone: "playerTwo" },
									]}
								/>
							</span>
							{game.lastOutcome === "sameTeam" ? (
								<CoopResultPill
									won={game.lastTeamWon === true}
									className="col-start-3 row-start-1 justify-self-end sm:col-start-4"
								/>
							) : (
								<span className="col-start-3 row-start-1 justify-self-end whitespace-nowrap rounded-full bg-card-light px-2 py-0.5 text-xs font-semibold tabular-nums text-white/60 sm:col-start-4">
									{letter(game.lastOutcome)} · {toDisplay(game.lastPlayed, dateFormat, uiLanguage)}
								</span>
							)}
						</li>
					);
				})}
			</ul>
			{sorted.length > VISIBLE_GAMES && (
				<BgtButton variant="text" size="1" className="self-start" onClick={() => setShowAll((value) => !value)}>
					{showAll ? t("per-game.show-less") : t("per-game.show-all", { count: sorted.length })}
				</BgtButton>
			)}
		</section>
	);
};
