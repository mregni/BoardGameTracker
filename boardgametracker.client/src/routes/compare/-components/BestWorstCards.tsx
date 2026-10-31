import { cva } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { CompareGame, CompareResult, CompareSide, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { SectionTitle } from "./SectionTitle";

export const MINIMUM_GAMES_FOR_BEST_AND_WORST = 3;

const cardVariants = cva("flex flex-col gap-2 rounded-xl border p-3.5", {
	variants: {
		side: {
			playerOne: "border-2 border-player-one/60 bg-linear-to-br from-player-one/20 to-transparent",
			playerTwo: "border-2 border-player-two/60 bg-linear-to-bl from-player-two/20 to-transparent",
		},
	},
});

const pillClasses =
	"whitespace-nowrap rounded-full bg-card-light px-2 py-0.5 text-xs font-semibold tabular-nums text-white/80";

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

	const list = (side: CompareSide, games: CompareGame[]) => {
		if (games.length === 0) {
			return <span className="text-xs text-white/60">{t("best-worst.none")}</span>;
		}

		return (
			<ul className="flex flex-col gap-1.5">
				{games.map((game) => {
					const details = gameById(game.gameId);
					const own = side === "playerOne" ? game.playerOneWins : game.playerTwoWins;
					const other = side === "playerOne" ? game.playerTwoWins : game.playerOneWins;
					return (
						<li key={game.gameId} className="grid grid-cols-[24px_minmax(0,1fr)_auto] items-center gap-2 text-sm">
							<BgtAvatar image={details?.image} title={details?.title ?? ""} size="small" />
							<span className="truncate">{details?.title}</span>
							<span className={pillClasses}>
								{integer(own)} – {integer(other)}
							</span>
						</li>
					);
				})}
			</ul>
		);
	};

	const card = (side: CompareSide, player: Player, opponent: Player, best: number[], worst: number[]) => {
		const heading = t("best-worst.against", { player: player.name, opponent: opponent.name });
		return (
			<section aria-label={heading} data-testid={`best-worst-${side}`} className={cardVariants({ side })}>
				<h3 className="font-bold">{heading}</h3>
				<div className="flex flex-col gap-1.5">
					<span className="text-xs text-white/60">{t("best-worst.best")}</span>
					{list(side, gamesFor(best))}
				</div>
				<div className="flex flex-col gap-1.5">
					<span className="text-xs text-white/60">{t("best-worst.worst")}</span>
					{list(side, gamesFor(worst))}
				</div>
			</section>
		);
	};

	return (
		<section aria-labelledby="best-worst-title" className="flex flex-col gap-1.5">
			<SectionTitle id="best-worst-title">{t("best-worst.title")}</SectionTitle>
			<div className="grid gap-3 sm:grid-cols-2">
				{card("playerOne", playerOne, playerTwo, oneBest, twoBest)}
				{card("playerTwo", playerTwo, playerOne, twoBest, oneBest)}
			</div>
			<span className="text-xs text-white/60">{t("best-worst.note", { count: MINIMUM_GAMES_FOR_BEST_AND_WORST })}</span>
		</section>
	);
};
