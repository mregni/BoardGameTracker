import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useId } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import type { Game } from "@/models/Games/Game";
import type { Loan } from "@/models/Loan/Loan";
import type { Player } from "@/models/Player/Player";
import { getLoanStatus, getLoanStatusTone } from "../-utils/loanStatus";
import { LoanRow } from "./LoanRow";

interface Props {
	player: Player | null;
	loans: Loan[];
	gameById: (id: number) => Game | null | undefined;
	dateFormat: string;
	onReturn?: (loans: Loan[]) => void;
	onEdit?: (loan: Loan) => void;
	onDelete?: (loan: Loan) => void;
}

export const BorrowerLoanGroup = ({ player, loans, gameById, dateFormat, onReturn, onEdit, onDelete }: Props) => {
	const { t } = useTranslation("loans");
	const headingId = useId();
	const name = player?.name ?? t("unknown-player");
	const statuses = loans.map((loan) => getLoanStatus(loan));
	const overdue = statuses.filter((status) => status.kind === "overdue").length;
	const longOverdue = statuses.some((status) => getLoanStatusTone(status) === "red");

	return (
		<section
			aria-labelledby={headingId}
			className={cx(
				"rounded-xl border bg-primary/5",
				overdue === 0 && "border-primary/20",
				overdue > 0 && (longOverdue ? "border-bad/50" : "border-warn/40"),
			)}
		>
			<header className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b border-white/5 px-3 py-3">
				<BgtAvatar size="large" title={name} image={player?.image ?? null} />
				<div className="flex min-w-40 flex-1 flex-col">
					<h2 id={headingId} className="truncate text-lg font-semibold text-white">
						{player ? (
							<Link to="/players/$playerId" params={{ playerId: player.id }} className="hover:text-primary-text">
								{name}
							</Link>
						) : (
							<span className="italic text-white/60">{name}</span>
						)}
					</h2>
					<p className="text-sm text-white/60">
						{t("group.games", { count: loans.length })}
						{overdue > 0 && (
							<span
								className={cx("font-semibold", longOverdue ? "text-bad" : "text-warn")}
							>{` · ${t("group.overdue", { count: overdue })}`}</span>
						)}
					</p>
				</div>
				{onReturn && loans.length > 1 && (
					<BgtButton
						size="2"
						className="ml-auto min-h-11 whitespace-nowrap text-sm sm:min-h-9"
						onClick={() => onReturn(loans)}
					>
						{t("mark-all-returned-button")}
					</BgtButton>
				)}
			</header>
			<ul className="divide-y divide-white/5">
				{loans.map((loan) => (
					<LoanRow
						key={loan.id}
						loan={loan}
						game={gameById(loan.gameId) ?? undefined}
						dateFormat={dateFormat}
						onReturn={onReturn ? (single) => onReturn([single]) : undefined}
						onEdit={onEdit}
						onDelete={onDelete}
					/>
				))}
			</ul>
		</section>
	);
};
