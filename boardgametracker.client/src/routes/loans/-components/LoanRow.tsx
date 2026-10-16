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

	const gameCell = game ? (
		<Link
			to="/games/$gameId"
			params={{ gameId: game.id }}
			className="truncate font-semibold text-white hover:text-primary-text"
		>
			{gameTitle}
		</Link>
	) : (
		<span className="truncate font-semibold text-white">{gameTitle}</span>
	);

	const playerCell = player ? (
		<Link
			to="/players/$playerId"
			params={{ playerId: player.id }}
			className="truncate text-sm text-white/80 hover:text-primary-text"
		>
			{playerName}
		</Link>
	) : (
		<span className="truncate text-sm text-white/80">{playerName}</span>
	);

	return (
		<div className="flex flex-col gap-2 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2.5 lg:grid lg:grid-cols-[minmax(8rem,20rem)_minmax(6rem,12rem)_1fr_auto_11rem_4.5rem_8rem] lg:items-center lg:gap-x-2 xl:gap-x-4">
			<div className="flex items-center justify-between gap-2 lg:contents">
				<div className="flex min-w-0 items-center gap-2 lg:order-1">
					<BgtAvatar size="medium" title={gameTitle} image={game?.image ?? null} />
					{gameCell}
				</div>
				<div className="shrink-0 lg:order-5 lg:min-w-0">
					<LoanStatusPill loan={loan} dateFormat={dateFormat} />
				</div>
			</div>
			<div className="flex items-center gap-2 lg:contents">
				<div className="flex min-w-0 items-center gap-2 lg:order-2">
					<BgtAvatar size="small" title={playerName} image={player?.image ?? null} />
					{playerCell}
				</div>
				<div className="ml-auto flex items-center gap-1.5 lg:order-6 lg:ml-0 lg:justify-end">
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
					<BgtButton
						size="1"
						variant="primary"
						className="whitespace-nowrap lg:order-7 lg:justify-self-start"
						onClick={() => onReturn(loan)}
					>
						<Check className="size-4" />
						{t("returned-button")}
					</BgtButton>
				)}
			</div>
			<div className="hidden lg:order-3 lg:block" />
			<div className="hidden lg:order-4 lg:block">
				<span className="hidden w-36 text-sm whitespace-nowrap text-white/50 xl:block">
					{t("since", { date: formatLoanDate(loan.loanDate, dateFormat, i18n.language) })}
				</span>
			</div>
		</div>
	);
};
