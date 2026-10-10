import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import History from "@/assets/icons/history.svg?react";
import LeftRightArrowIcon from "@/assets/icons/left-right-arrow.svg?react";
import Package from "@/assets/icons/package.svg?react";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
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
import { LoanCard } from "./-components/LoanCard";
import { useLoanActions } from "./-hooks/useLoanActions";
import { useLoanModals } from "./-hooks/useLoanModels";
import { useLoans } from "./-hooks/useLoans";
import { EditLoanModal } from "./-modals/EditLoanModal";
import NewLoanModal from "./-modals/NewLoanModal";
import { ReturnLoanModal } from "./-modals/ReturnLoanModal";

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

	const modals = useLoanModals();

	const actions = useLoanActions({
		deleteLoan,
		returnLoan: returnLoan,
		onDeleteModalClose: modals.deleteModal.hide,
	});

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
				action={canWrite ? { label: t("new.title"), onClick: modals.createModal.show } : undefined}
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
									content: "loans:new.title",
								},
							]
						: []
				}
			/>
			<BgtPageContent isLoading={isLoading} data={{ loans, settings }}>
				{({ loans, settings }) => (
					<>
						<div className="grid grid-cols-2 lg:grid-cols-4 gap-3 xl:gap-6">
							<BgtTextStatistic
								title={t("statistics.active")}
								content={loans.filter((loan) => loan.returnedDate === null).length}
							/>
							<BgtTextStatistic
								title={t("statistics.returned")}
								content={loans.filter((loan) => loan.returnedDate !== null).length}
							/>
							<BgtTextStatistic title={t("statistics.total-loans")} content={loans.length} />
						</div>
						<BgtHeading size="5" className="flex items-center gap-2">
							<Package className="text-primary text-2xl" />
							{t("statistics.active")} ({loans.filter((loan) => loan.returnedDate === null).length})
						</BgtHeading>
						{loans.filter((loan) => loan.returnedDate === null).length === 0 ? (
							<BgtText color="primary">{t("no-active-loans")}</BgtText>
						) : (
							<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 gap-6">
								{loans
									.filter((loan) => loan.returnedDate === null)
									.map((loan) => (
										<LoanCard
											key={loan.id}
											loan={loan}
											game={gameById(loan.gameId) ?? undefined}
											player={playerById(loan.playerId) ?? undefined}
											dateFormat={settings.dateFormat}
											onReturn={canWrite ? requestReturn : undefined}
											onEdit={canWrite ? requestEdit : undefined}
											onDelete={canWrite ? requestDelete : undefined}
										/>
									))}
							</div>
						)}
						<BgtHeading size="5" className="flex items-center gap-2">
							<History className="text-primary text-2xl" />
							{t("common:history")} ({loans.filter((loan) => loan.returnedDate !== null).length})
						</BgtHeading>
						{loans.filter((loan) => loan.returnedDate !== null).length === 0 ? (
							<BgtText color="primary">{t("no-returned-loans")}</BgtText>
						) : (
							<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 gap-6">
								{loans
									.filter((loan) => loan.returnedDate !== null)
									.map((loan) => (
										<LoanCard
											key={loan.id}
											loan={loan}
											game={gameById(loan.gameId) ?? undefined}
											player={playerById(loan.playerId) ?? undefined}
											dateFormat={settings.dateFormat}
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
