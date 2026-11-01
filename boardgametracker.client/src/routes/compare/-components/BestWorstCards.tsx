import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { CompareGame, CompareResult, CompareSide, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { SectionTitle } from "./SectionTitle";

export const MINIMUM_GAMES_FOR_BEST_AND_WORST = 3;

interface Props {
	compare: CompareResult;
	playerOne: Player;
	playerTwo: Player;
}

export const BestWorstCards = ({ compare, playerOne, playerTwo }: Props) => {
	const { t } = useTranslation("compare");
	const { gameById } = useGameById();
	const { integer } = useNumberFormat();

	const { playerOne: oneBest, playerTwo: twoBest } = compare.bestGames;
	if (oneBest.length === 0 && twoBest.length === 0) {
		return null;
	}

	const gamesFor = (ids: number[]) =>
		ids
			.map((id) => compare.games.find((game) => game.gameId === id))
			.filter((game): game is CompareGame => game !== undefined);

	const row = (side: CompareSide, player: Player, best: number[]) => {
		const games = gamesFor(best);
		return (
			<li data-testid={`best-worst-${side}`} className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
				<strong className={side === "playerOne" ? "text-player-one" : "text-player-two"}>{player.name}</strong>
				{games.length === 0 ? (
					<span className="text-sm text-white/60">{t("best-worst.none")}</span>
				) : (
					<ul className="flex flex-wrap gap-1.5">
						{games.map((game) => {
							const details = gameById(game.gameId);
							const own = side === "playerOne" ? game.playerOneWins : game.playerTwoWins;
							const other = side === "playerOne" ? game.playerTwoWins : game.playerOneWins;
							return (
								<li
									key={game.gameId}
									className="flex items-center gap-1.5 rounded-full bg-card-light py-0.5 pr-2.5 pl-0.5 text-sm"
								>
									<BgtAvatar image={details?.image} title={details?.title ?? ""} size="small" />
									<span>{details?.title}</span>
									<span className="font-semibold tabular-nums text-white/80">
										{integer(own)} – {integer(other)}
									</span>
								</li>
							);
						})}
					</ul>
				)}
			</li>
		);
	};

	return (
		<section aria-labelledby="best-worst-title" className="flex flex-col gap-1.5">
			<SectionTitle id="best-worst-title">{t("best-worst.title")}</SectionTitle>
			<ul className="flex flex-col gap-2.5 rounded-xl border border-card-border bg-card p-3.5">
				{row("playerOne", playerOne, oneBest)}
				{row("playerTwo", playerTwo, twoBest)}
			</ul>
			<span className="text-xs text-white/60">{t("best-worst.note", { count: MINIMUM_GAMES_FOR_BEST_AND_WORST })}</span>
		</section>
	);
};
