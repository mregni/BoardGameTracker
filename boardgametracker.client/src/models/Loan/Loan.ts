export interface Loan {
  id: string;
  gameId: string;
  playerId: string;
  loanDate: Date;
  returnDate: Date | null;
}
