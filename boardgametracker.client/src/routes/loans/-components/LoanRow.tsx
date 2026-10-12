import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Check from "@/assets/icons/check.svg?react";
import Pencil from "@/assets/icons/pencil.svg?react";
import Trash from "@/assets/icons/trash.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import type { Game } from "@/models/Games/Game";
import type { Loan } from "@/models/Loan/Loan";
import type { Player } from "@/models/Player/Player";
import { formatLoanDate } from "../-utils/loanStatus";
import { LoanStatusPill } from "./LoanStatusPill";

interface Props {
	loan: Loan;
	game: Game | undefined;
	player: Player | undefined;
	dateFormat: string;
	onReturn?: (loan: Loan) => void;
	onEdit?: (loan: Loan) => void;
	onDelete?: (loan: Loan) => void;
}

export const LoanRow = ({ loan, game, player, dateFormat, onReturn, onEdit, onDelete }: Props) => {
	const { t, i18n } = useTranslation(["loans", "common"]);
	const gameTitle = game?.title ?? "";
	const playerName = player?.name ?? "";

	return (
		<div className="grid grid-cols-[minmax(0,1fr)_auto_auto] items-center gap-x-2 gap-y-2 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2.5 lg:grid-cols-[minmax(8rem,20rem)_minmax(6rem,12rem)_1fr_11rem_4.5rem_8rem] lg:gap-x-3 xl:grid-cols-[minmax(8rem,20rem)_minmax(6rem,12rem)_1fr_9rem_11rem_4.5rem_8rem] xl:gap-x-4">
			<div className="col-start-1 row-start-1 flex min-w-0 items-center gap-2">
				<BgtAvatar size="medium" title={gameTitle} image={game?.image ?? null} />
				{game ? (
					<Link
						to="/games/$gameId"
						params={{ gameId: game.id }}
						className="truncate font-semibold text-white hover:text-primary"
					>
						{gameTitle}
					</Link>
				) : (
					<span className="truncate font-semibold text-white">{gameTitle}</span>
				)}
			</div>
			<div className="col-start-1 row-start-2 flex min-w-0 items-center gap-2 lg:col-start-2 lg:row-start-1">
				<BgtAvatar size="small" title={playerName} image={player?.image ?? null} />
				{player ? (
					<Link
						to="/players/$playerId"
						params={{ playerId: player.id }}
						className="truncate text-sm text-white/80 hover:text-primary"
					>
						{playerName}
					</Link>
				) : (
					<span className="truncate text-sm text-white/80">{playerName}</span>
				)}
			</div>
			<span className="hidden text-sm whitespace-nowrap text-white/50 xl:col-start-4 xl:row-start-1 xl:block">
				{t("since", { date: formatLoanDate(loan.loanDate, dateFormat, i18n.language) })}
			</span>
			<div className="col-span-2 col-start-2 row-start-1 min-w-0 max-w-full justify-self-end lg:col-span-1 lg:col-start-4 lg:justify-self-start xl:col-start-5">
				<LoanStatusPill loan={loan} dateFormat={dateFormat} />
			</div>
			<div className="col-start-2 row-start-2 flex items-center justify-end gap-1.5 lg:col-start-5 lg:row-start-1 xl:col-start-6">
				{onEdit && (
					<BgtIconButton
						intent="subtile"
						icon={<Pencil className="size-4" />}
						aria-label={t("edit.button")}
						onClick={() => onEdit(loan)}
					/>
				)}
				{onDelete && (
					<BgtIconButton
						intent="subtile"
						icon={<Trash className="size-4" />}
						aria-label={t("delete.button")}
						onClick={() => onDelete(loan)}
					/>
				)}
			</div>
			{onReturn && (
				<div className="col-start-3 row-start-2 justify-self-end lg:col-start-6 lg:row-start-1 lg:justify-self-start xl:col-start-7">
					<BgtButton size="1" variant="primary" className="whitespace-nowrap" onClick={() => onReturn(loan)}>
						<Check className="size-4" />
						{t("returned-button")}
					</BgtButton>
				</div>
			)}
		</div>
	);
};
