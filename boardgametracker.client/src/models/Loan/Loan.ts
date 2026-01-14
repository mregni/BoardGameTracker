export interface Loan {
  id: number;
  loanDate: Date;
  dueDate: Date | null;
  returnedDate: Date | null;
  gameId: number;
  playerId: number;
}
