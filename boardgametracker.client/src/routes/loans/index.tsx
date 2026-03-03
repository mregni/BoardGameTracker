import { createFileRoute } from "@tanstack/react-router";
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
import { getLoans } from "@/services/queries/loans";
import { useGameById } from "../-hooks/useGameById";
import { usePlayerById } from "../-hooks/usePlayerById";
import { LoanCard } from "./-components/LoanCard";
import { useLoanActions } from "./-hooks/useLoanActions";
import { useLoanModals } from "./-hooks/useLoanModels";
import { useLoans } from "./-hooks/useLoans";
import NewLoanModal from "./-modals/NewLoanModal";

export const Route = createFileRoute("/loans/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getLoans());
	},
});

function RouteComponent() {
	const { t } = useTranslation();
	const { canWrite } = usePermissions();
	const { loans, settings, deleteLoan, returnLoan, isLoading } = useLoans();
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();

	const modals = useLoanModals();

	const actions = useLoanActions({
		deleteLoan,
		returnLoan: returnLoan,
		onDeleteModalClose: modals.deleteModal.hide,
	});

	if (loans.length === 0) {
		return (
			<BgtEmptyPage
				header={t("common.loans")}
				icon={LeftRightArrowIcon}
				title={t("loan.empty.title")}
				description={t("loan.empty.description")}
				action={canWrite ? { label: t("loan.new.title"), onClick: modals.createModal.show } : undefined}
			>
				<NewLoanModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				icon={LeftRightArrowIcon}
				header={t("common.loans")}
				actions={
					canWrite
						? [
								{
									onClick: modals.createModal.show,
									variant: "primary",
									content: "loan.new.title",
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
								title={t("loan.statistics.active")}
								content={loans.filter((loan) => loan.returnedDate === null).length}
							/>
							<BgtTextStatistic
								title={t("loan.statistics.returned")}
								content={loans.filter((loan) => loan.returnedDate !== null).length}
							/>
							<BgtTextStatistic title={t("loan.statistics.total-loans")} content={loans.length} />
						</div>
						<BgtHeading size="5" className="flex items-center gap-2">
							<Package className="text-primary text-2xl" />
							{t("loan.statistics.active")} ({loans.filter((loan) => loan.returnedDate === null).length})
						</BgtHeading>
						{loans.filter((loan) => loan.returnedDate === null).length === 0 ? (
							<BgtText color="primary">{t("loan.no-active-loans")}</BgtText>
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
											onReturn={canWrite ? actions.handleReturnLoan : undefined}
											onDelete={canWrite ? actions.handleDelete : undefined}
										/>
									))}
							</div>
						)}
						<BgtHeading size="5" className="flex items-center gap-2">
							<History className="text-primary text-2xl" />
							{t("common.history")} ({loans.filter((loan) => loan.returnedDate !== null).length})
						</BgtHeading>
						{loans.filter((loan) => loan.returnedDate !== null).length === 0 ? (
							<BgtText color="primary">{t("loan.no-returned-loans")}</BgtText>
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
											onDelete={canWrite ? actions.handleDelete : undefined}
										/>
									))}
							</div>
						)}
						<NewLoanModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
