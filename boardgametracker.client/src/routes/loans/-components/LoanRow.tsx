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
import { toShortDate } from "../-utils/loanStatus";
import { LoanStatusPill } from "./LoanStatusPill";

interface Props {
	loan: Loan;
	game: Game | undefined;
	player: Player | undefined;
	onReturn?: (loan: Loan) => void;
	onEdit?: (loan: Loan) => void;
	onDelete?: (loan: Loan) => void;
}

export const LoanRow = ({ loan, game, player, onReturn, onEdit, onDelete }: Props) => {
	const { t, i18n } = useTranslation(["loans", "common"]);
	const gameTitle = game?.title ?? "";
	const playerName = player?.name ?? "";

	return (
		<div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-3 gap-y-2 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2.5 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_9rem_auto] xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_7rem_9rem_auto] xl:gap-x-4">
			<div className="col-start-1 row-start-1 flex min-w-0 items-center gap-2 lg:col-start-auto lg:row-start-auto">
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
			<div className="col-start-1 row-start-2 flex min-w-0 items-center gap-2 lg:col-start-auto lg:row-start-auto">
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
			<span className="hidden text-sm text-white/50 xl:block">
				{t("since", { date: toShortDate(loan.loanDate, i18n.language) })}
			</span>
			<div className="col-start-2 row-start-1 justify-self-end lg:col-start-auto lg:row-start-auto lg:justify-self-start">
				<LoanStatusPill loan={loan} />
			</div>
			<div className="col-start-2 row-start-2 flex items-center justify-end gap-1.5 lg:col-start-auto lg:row-start-auto">
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
				{onReturn && (
					<BgtButton size="1" variant="primary" onClick={() => onReturn(loan)}>
						<Check className="size-4" />
						{t("returned-button")}
					</BgtButton>
				)}
			</div>
		</div>
	);
};
