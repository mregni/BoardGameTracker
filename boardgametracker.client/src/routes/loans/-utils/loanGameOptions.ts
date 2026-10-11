import { type Game, GameState, type Player } from "@/models";
import type { Loan } from "@/models/Loan/Loan";
import type { LoanGameOption } from "../-components/LoanGamePicker";
import { isActiveLoan } from "./loanStatus";

export const buildLoanGameOptions = (games: Game[], loans: Loan[], players: Player[]): LoanGameOption[] => {
	const borrowerByGame = new Map<number, string | null>();
	for (const loan of loans.filter(isActiveLoan)) {
		borrowerByGame.set(loan.gameId, players.find((player) => player.id === loan.playerId)?.name ?? null);
	}

	return games
		.filter((game) => game.state === GameState.Owned)
		.map((game) => ({
			id: game.id,
			title: game.title,
			image: game.image ?? null,
			borrower: borrowerByGame.get(game.id) ?? null,
			available: !game.isLoaned && !borrowerByGame.has(game.id),
		}))
		.sort((a, b) => Number(b.available) - Number(a.available) || a.title.localeCompare(b.title));
};
