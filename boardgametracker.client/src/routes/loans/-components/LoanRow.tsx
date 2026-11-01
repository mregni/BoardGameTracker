import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtActionMenu } from "@/components/BgtActionMenu/BgtActionMenu";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import type { Game } from "@/models/Games/Game";
import type { Loan } from "@/models/Loan/Loan";
import type { Player } from "@/models/Player/Player";
import { formatLoanDate, getLoanStatus } from "../-utils/loanStatus";
import { LoanStatusPill } from "./LoanStatusPill";

interface Props {
	loan: Loan;
	game: Game | undefined;
	player?: Player | null;
	showBorrower?: boolean;
	dateFormat: string;
	onReturn?: (loan: Loan) => void;
	onEdit?: (loan: Loan) => void;
	onDelete?: (loan: Loan) => void;
}

export const LoanRow = ({ loan, game, player, showBorrower, dateFormat, onReturn, onEdit, onDelete }: Props) => {
	const { t, i18n } = useTranslation(["loans", "common"]);
	const gameTitle = game?.title ?? t("unknown-game");
	const since = t("since", { date: formatLoanDate(loan.loanDate, dateFormat, i18n.language) });
	const muted = !!loan.returnedDate;
	const due =
		getLoanStatus(loan).kind === "overdue"
			? t("due", { date: formatLoanDate(loan.dueDate, dateFormat, i18n.language) })
			: null;

	const menuItems = [
		...(onEdit ? [{ label: t("edit.button"), onSelect: () => onEdit(loan) }] : []),
		...(onDelete ? [{ label: t("delete.button"), onSelect: () => onDelete(loan), destructive: true }] : []),
	];

	return (
		<li className="flex flex-wrap items-center gap-x-3 gap-y-2 px-3 py-2.5 sm:flex-nowrap">
			<div className={cx("flex min-w-0 flex-1 items-center gap-3", muted && "opacity-60")}>
				<BgtAvatar size="large" shape="square" title={gameTitle} image={game?.image ?? null} />
				<div className="flex min-w-0 flex-col">
					{game ? (
						<Link
							to="/games/$gameId"
							params={{ gameId: game.id }}
							className="truncate font-semibold text-white hover:text-primary-text"
							title={gameTitle}
						>
							{gameTitle}
						</Link>
					) : (
						<span className="truncate font-semibold text-white/60 italic">{gameTitle}</span>
					)}
					<span className="truncate text-sm text-white/50">
						{showBorrower && (
							<>
								{player ? (
									<Link
										to="/players/$playerId"
										params={{ playerId: player.id }}
										className="text-white/70 hover:text-primary-text"
									>
										{player.name}
									</Link>
								) : (
									<span className="italic">{t("unknown-player")}</span>
								)}
								{" · "}
							</>
						)}
						{since}
						{due && ` · ${due}`}
					</span>
				</div>
			</div>
			<div className="flex w-full items-center gap-2 sm:w-auto">
				<span className={cx("min-w-0", muted && "opacity-60")}>
					<LoanStatusPill loan={loan} dateFormat={dateFormat} />
				</span>
				<div className="ml-auto flex items-center gap-3 sm:ml-3">
					{onReturn && (
						<BgtButton
							size="2"
							variant="outline"
							className="min-h-11 whitespace-nowrap text-sm sm:min-h-9"
							aria-label={`${t("mark-returned-button")} ${gameTitle}`}
							onClick={() => onReturn(loan)}
						>
							{t("mark-returned-button")}
						</BgtButton>
					)}
					{menuItems.length > 0 && <BgtActionMenu label={t("row.actions", { game: gameTitle })} items={menuItems} />}
				</div>
			</div>
		</li>
	);
};
