import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import type { Loan } from "@/models/Loan/Loan";
import { render, screen, userEvent } from "@/test/test-utils";
import { LoanRow } from "./LoanRow";

vi.mock("@tanstack/react-router", () => ({
	Link: ({
		to,
		params,
		children,
		className,
	}: {
		to: string;
		params: Record<string, string | number>;
		children: ReactNode;
		className?: string;
	}) => (
		<a
			className={className}
			href={Object.entries(params).reduce((path, [name, value]) => path.replace(`$${name}`, String(value)), to)}
		>
			{children}
		</a>
	),
}));

const game = { id: 3, title: "Carcassonne", image: null } as unknown as Game;
const player = { id: 4, name: "Kathleen", image: null } as unknown as Player;

const activeLoan: Loan = {
	id: 7,
	loanDate: new Date(2026, 8, 26),
	dueDate: new Date(2026, 9, 8),
	returnedDate: null,
	gameId: 3,
	playerId: 4,
	isActive: true,
};

const returnedLoan: Loan = { ...activeLoan, id: 8, returnedDate: new Date(2026, 9, 1), isActive: false };

describe("LoanRow", () => {
	beforeEach(() => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 12, 0));
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("shows the game, the borrower, the start date and the status", () => {
		render(<LoanRow loan={activeLoan} game={game} player={player} showBorrower dateFormat="dd-MM-yyyy" />);

		expect(screen.getByRole("link", { name: "Carcassonne" })).toHaveAttribute("href", "/games/3");
		expect(screen.getByRole("link", { name: "Kathleen" })).toHaveAttribute("href", "/players/4");
		expect(screen.getByText(/since/)).toBeInTheDocument();
		expect(screen.getByText("status.overdue")).toBeInTheDocument();
	});

	it("leaves the borrower out unless asked", () => {
		render(<LoanRow loan={activeLoan} game={game} player={player} dateFormat="dd-MM-yyyy" />);

		expect(screen.queryByText("Kathleen")).not.toBeInTheDocument();
	});

	it("passes the loan to the edit, delete and returned actions", async () => {
		const onReturn = vi.fn();
		const onEdit = vi.fn();
		const onDelete = vi.fn();
		render(
			<LoanRow
				loan={activeLoan}
				game={game}
				player={player}
				dateFormat="dd-MM-yyyy"
				onReturn={onReturn}
				onEdit={onEdit}
				onDelete={onDelete}
			/>,
		);

		await userEvent.click(screen.getByRole("button", { name: "row.actions" }));
		await userEvent.click(screen.getByRole("menuitem", { name: "edit.button" }));
		await userEvent.click(screen.getByRole("button", { name: "row.actions" }));
		expect(screen.getByRole("menuitem", { name: "delete.button" })).toHaveClass("text-bad");
		await userEvent.click(screen.getByRole("menuitem", { name: "delete.button" }));
		await userEvent.click(screen.getByRole("button", { name: /^mark-returned-button/ }));

		expect(onEdit).toHaveBeenCalledWith(activeLoan);
		expect(onDelete).toHaveBeenCalledWith(activeLoan);
		expect(onReturn).toHaveBeenCalledWith(activeLoan);
	});

	it("labels the row button as an action and keeps the status in the pill", () => {
		render(
			<LoanRow
				loan={activeLoan}
				game={game}
				player={player}
				dateFormat="dd-MM-yyyy"
				onReturn={vi.fn()}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
			/>,
		);

		const button = screen.getByRole("button", { name: "mark-returned-button Carcassonne" });
		expect(button).toHaveTextContent(/^mark-returned-button$/);
		expect(screen.queryByText("status.returned")).not.toBeInTheDocument();
	});

	it("offers no returned button for a returned loan", () => {
		render(
			<LoanRow
				loan={returnedLoan}
				game={game}
				player={player}
				dateFormat="dd-MM-yyyy"
				onEdit={vi.fn()}
				onDelete={vi.fn()}
			/>,
		);

		expect(screen.getByText("status.returned")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "row.actions" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: /^mark-returned-button/ })).not.toBeInTheDocument();
	});

	it("shows no actions without write access", () => {
		render(<LoanRow loan={activeLoan} game={game} player={player} dateFormat="dd-MM-yyyy" />);

		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("shows plain names when the game or player is unknown", () => {
		render(<LoanRow loan={activeLoan} game={undefined} player={undefined} showBorrower dateFormat="dd-MM-yyyy" />);

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
		expect(screen.getByText("unknown-game")).toBeInTheDocument();
		expect(screen.getByText("unknown-player")).toBeInTheDocument();
		expect(screen.getByText("status.overdue")).toBeInTheDocument();
	});
});
