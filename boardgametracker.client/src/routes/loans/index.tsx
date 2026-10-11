import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import LeftRightArrowIcon from "@/assets/icons/left-right-arrow.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtText } from "@/components/BgtText/BgtText";
import { usePermissions } from "@/hooks/usePermissions";
import type { Loan } from "@/models/Loan/Loan";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { getLoans } from "@/services/queries/loans";
import { useGameById } from "../-hooks/useGameById";
import { usePlayerById } from "../-hooks/usePlayerById";
import { LoanRow } from "./-components/LoanRow";
import { useLoanActions } from "./-hooks/useLoanActions";
import { useLoanModals } from "./-hooks/useLoanModels";
import { useLoans } from "./-hooks/useLoans";
import { EditLoanModal } from "./-modals/EditLoanModal";
import NewLoanModal from "./-modals/NewLoanModal";
import { ReturnLoanModal } from "./-modals/ReturnLoanModal";
import { getLoanStats, isActiveLoan, sortActiveLoans, sortReturnedLoans } from "./-utils/loanStatus";

export const Route = createFileRoute("/loans/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getLoans());
	},
});

function RouteComponent() {
	const { t } = useTranslation(["loans", "common"]);
	const { canWrite } = usePermissions();
	const { loans, settings, deleteLoan, returnLoan, updateLoan, isLoading } = useLoans();
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();
	const [selectedLoan, setSelectedLoan] = useState<Loan | null>(null);
	const [returnModalOpen, setReturnModalOpen] = useState(false);
	const [showHistory, setShowHistory] = useState(false);

	const modals = useLoanModals();

	const actions = useLoanActions({
		deleteLoan,
		returnLoan: returnLoan,
		onDeleteModalClose: modals.deleteModal.hide,
	});

	const stats = useMemo(() => getLoanStats(loans), [loans]);
	const activeLoans = useMemo(() => sortActiveLoans(loans.filter(isActiveLoan)), [loans]);
	const returnedLoans = useMemo(() => sortReturnedLoans(loans.filter((loan) => !isActiveLoan(loan))), [loans]);

	const requestDelete = (loan: Loan) => {
		setSelectedLoan(loan);
		modals.deleteModal.show();
	};

	const requestEdit = (loan: Loan) => {
		setSelectedLoan(loan);
		modals.editModal.show();
	};

	const requestReturn = (loan: Loan) => {
		setSelectedLoan(loan);
		setReturnModalOpen(true);
	};

	const selectedGameTitle = selectedLoan ? (gameById(selectedLoan.gameId)?.title ?? "") : "";

	if (!isLoading && loans.length === 0) {
		return (
			<BgtEmptyPage
				header={t("common:loans")}
				icon={LeftRightArrowIcon}
				title={t("empty.title")}
				description={t("empty.description")}
				action={canWrite ? { label: t("new.button"), onClick: modals.createModal.show } : undefined}
			>
				<NewLoanModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				icon={LeftRightArrowIcon}
				header={t("common:loans")}
				actions={
					canWrite
						? [
								{
									onClick: modals.createModal.show,
									variant: "primary",
									content: "loans:new.button",
								},
							]
						: []
				}
			/>
			<BgtPageContent isLoading={isLoading} data={{ loans, settings }}>
				{() => (
					<>
						<div className="grid grid-cols-2 lg:grid-cols-4 gap-3 xl:gap-6">
							<BgtTextStatistic title={t("statistics.on-loan")} content={stats.onLoan} />
							<BgtTextStatistic title={t("statistics.overdue")} content={stats.overdue} />
							<BgtTextStatistic title={t("statistics.due-this-week")} content={stats.dueThisWeek} />
							<BgtTextStatistic title={t("statistics.returned")} content={stats.returned} />
						</div>
						{activeLoans.length === 0 ? (
							<BgtText color="primary">{t("no-active-loans")}</BgtText>
						) : (
							<div className="flex flex-col gap-2">
								{activeLoans.map((loan) => (
									<LoanRow
										key={loan.id}
										loan={loan}
										game={gameById(loan.gameId) ?? undefined}
										player={playerById(loan.playerId) ?? undefined}
										onReturn={canWrite ? requestReturn : undefined}
										onEdit={canWrite ? requestEdit : undefined}
										onDelete={canWrite ? requestDelete : undefined}
									/>
								))}
							</div>
						)}
						{returnedLoans.length > 0 && (
							<div className="flex flex-col gap-2">
								<div className="flex items-center justify-between gap-3 rounded-lg border border-dashed border-primary/30 px-3 py-2 text-sm text-white/60">
									<span>{t("history.summary", { count: returnedLoans.length })}</span>
									<BgtButton
										variant="text"
										size="1"
										aria-expanded={showHistory}
										onClick={() => setShowHistory((value) => !value)}
									>
										{showHistory ? t("history.hide") : t("history.show")}
									</BgtButton>
								</div>
								{showHistory &&
									returnedLoans.map((loan) => (
										<LoanRow
											key={loan.id}
											loan={loan}
											game={gameById(loan.gameId) ?? undefined}
											player={playerById(loan.playerId) ?? undefined}
											onEdit={canWrite ? requestEdit : undefined}
											onDelete={canWrite ? requestDelete : undefined}
										/>
									))}
							</div>
						)}
						<NewLoanModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
						<EditLoanModal
							loan={selectedLoan}
							open={modals.editModal.isOpen}
							close={modals.editModal.hide}
							onSave={updateLoan}
						/>
						<ReturnLoanModal
							loan={selectedLoan}
							gameTitle={selectedGameTitle}
							open={returnModalOpen}
							close={() => setReturnModalOpen(false)}
							onReturn={actions.handleReturnLoan}
						/>
						<BgtDeleteModal
							title={selectedGameTitle}
							heading={t("delete.title", { game: selectedGameTitle })}
							open={modals.deleteModal.isOpen}
							close={modals.deleteModal.hide}
							onDelete={() => selectedLoan && actions.handleDelete(selectedLoan.id)}
							description={t("delete.description")}
						/>
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
