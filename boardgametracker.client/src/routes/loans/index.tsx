import { noop } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useId, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import LeftRightArrowIcon from "@/assets/icons/left-right-arrow.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { SearchInputField } from "@/components/BgtForm";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { usePermissions } from "@/hooks/usePermissions";
import type { Loan } from "@/models/Loan/Loan";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { getLoans } from "@/services/queries/loans";
import { useGameById } from "../-hooks/useGameById";
import { usePlayerById } from "../-hooks/usePlayerById";
import { BorrowerLoanGroup } from "./-components/BorrowerLoanGroup";
import { LoanRow } from "./-components/LoanRow";
import { useLoanActions } from "./-hooks/useLoanActions";
import { useLoanModals } from "./-hooks/useLoanModels";
import { useLoans } from "./-hooks/useLoans";
import { EditLoanModal } from "./-modals/EditLoanModal";
import NewLoanModal from "./-modals/NewLoanModal";
import { ReturnLoanModal } from "./-modals/ReturnLoanModal";
import {
	getLoanStats,
	groupLoansByBorrower,
	type LoanFilter,
	matchesLoanFilter,
	partitionLoans,
} from "./-utils/loanStatus";

export const Route = createFileRoute("/loans/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getLoans()).catch(noop);
	},
});

function RouteComponent() {
	const { t } = useTranslation(["loans", "common"]);
	const { canWrite } = usePermissions();
	const { loans, settings, deleteLoan, returnLoans, updateLoan, isLoading } = useLoans();
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();
	const [selectedLoan, setSelectedLoan] = useState<Loan | null>(null);
	const [returnTargets, setReturnTargets] = useState<Loan[]>([]);
	const [showHistory, setShowHistory] = useState(false);
	const [filter, setFilter] = useState<LoanFilter>("all");
	const [search, setSearch] = useState("");
	const historyId = useId();

	const modals = useLoanModals();

	const actions = useLoanActions({
		deleteLoan,
		returnLoans,
		onDeleteModalClose: modals.deleteModal.hide,
	});

	const { active: activeLoans, returned: returnedLoans } = useMemo(() => partitionLoans(loans), [loans]);
	const stats = useMemo(() => getLoanStats(activeLoans), [activeLoans]);

	const searchMatcher = (text: string) => {
		const query = text.trim().toLowerCase();
		return (loan: Loan) =>
			!query ||
			(gameById(loan.gameId)?.title ?? "").toLowerCase().includes(query) ||
			(playerById(loan.playerId)?.name ?? "").toLowerCase().includes(query);
	};
	const matchesSearch = searchMatcher(search);
	const searching = search.trim().length > 0;

	const groups = groupLoansByBorrower(
		activeLoans.filter((loan) => matchesLoanFilter(loan, filter) && matchesSearch(loan)),
	);
	const visibleReturned = returnedLoans.filter(matchesSearch);

	const toggleFilter = (value: LoanFilter) => setFilter((current) => (current === value ? "all" : value));

	const changeSearch = (value: string) => {
		setSearch(value);
		const matches = searchMatcher(value);
		if (value.trim() && !activeLoans.some(matches) && returnedLoans.some(matches)) {
			setShowHistory(true);
		}
	};

	const clearFilters = () => {
		setFilter("all");
		setSearch("");
	};

	const filterOptions = [
		{
			value: "all",
			label: t("statistics.on-loan"),
			chip: t("chip.on-loan", { count: activeLoans.length }),
			count: activeLoans.length,
		},
		{
			value: "overdue",
			label: t("statistics.overdue"),
			chip: t("chip.overdue", { count: stats.overdue }),
			count: stats.overdue,
		},
		{
			value: "due-this-week",
			label: t("statistics.due-this-week"),
			chip: t("chip.due-this-week", { count: stats.dueThisWeek }),
			count: stats.dueThisWeek,
		},
	] as const;
	const selectFilter = (value: LoanFilter) => (value === "all" ? setFilter("all") : toggleFilter(value));
	const canSelect = (option: (typeof filterOptions)[number]) => option.count > 0 || filter === option.value;
	const activeFilterLabel = filterOptions.find((option) => option.value === filter && filter !== "all")?.label;
	const overdueTone = stats.longOverdue > 0 ? "red" : "amber";
	const overdueColor = stats.overdue > 0 ? overdueTone : "cyan";

	const requestDelete = (loan: Loan) => {
		setSelectedLoan(loan);
		modals.deleteModal.show();
	};

	const requestEdit = (loan: Loan) => {
		setSelectedLoan(loan);
		modals.editModal.show();
	};

	const gameTitle = (loan: Loan) => gameById(loan.gameId)?.title ?? t("unknown-game");
	const selectedGameTitle = selectedLoan ? gameTitle(selectedLoan) : "";
	const returnTitle =
		returnTargets.length > 1
			? t("return.title-all", {
					count: returnTargets.length,
					name: playerById(returnTargets[0].playerId)?.name ?? t("unknown-player"),
				})
			: t("return.title", { game: returnTargets[0] ? gameTitle(returnTargets[0]) : "" });

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
									icon: PlusIcon,
								},
							]
						: []
				}
			/>
			<BgtPageContent isLoading={isLoading} data={{ loans, settings }}>
				{({ settings }) => (
					<>
						<div className="hidden grid-cols-3 gap-3 sm:grid xl:gap-6">
							{filterOptions.map((option) => (
								<BgtTextStatistic
									key={option.value}
									title={option.label}
									content={option.count}
									valueColor={option.value === "overdue" ? overdueColor : "cyan"}
									onClick={canSelect(option) ? () => selectFilter(option.value) : undefined}
									pressed={canSelect(option) ? filter === option.value : undefined}
								/>
							))}
						</div>
						<div className="flex flex-wrap gap-2 sm:hidden">
							{filterOptions.map((option) => (
								<button
									key={option.value}
									type="button"
									aria-pressed={filter === option.value}
									disabled={!canSelect(option)}
									onClick={() => selectFilter(option.value)}
									className={cx(
										"min-h-11 rounded-full border px-4 text-sm font-semibold transition-colors disabled:opacity-50",
										filter === option.value ? "border-primary bg-primary/30 text-white" : "border-primary/25",
										filter !== option.value &&
											(option.value === "overdue" && option.count > 0 ? "text-warn" : "text-white/80"),
									)}
								>
									{option.chip}
								</button>
							))}
						</div>
						<SearchInputField
							value={search}
							onChange={(event) => changeSearch(event.target.value)}
							placeholder={t("search.placeholder")}
						/>
						{activeFilterLabel && (
							<button
								type="button"
								onClick={() => setFilter("all")}
								className="inline-flex min-h-9 items-center gap-2 self-start rounded-full border border-primary/40 bg-primary/15 px-3 text-sm text-white hover:bg-primary/25"
							>
								{t("filter.showing", { name: activeFilterLabel })}
								<XIcon className="size-4" aria-hidden="true" />
								<span className="sr-only">{t("filter.clear")}</span>
							</button>
						)}
						{activeLoans.length === 0 ? (
							<p className="text-white/60">{t("no-active-loans")}</p>
						) : groups.length === 0 ? (
							<div className="flex flex-wrap items-center gap-3 text-white/60">
								<span>
									{searching && filter === "all" ? t("no-search-matches", { query: search.trim() }) : t("no-matches")}
								</span>
								<BgtButton variant="text" size="2" onClick={clearFilters}>
									{t("clear-filter")}
								</BgtButton>
							</div>
						) : (
							<div className="flex flex-col gap-4">
								{groups.map((group) => (
									<BorrowerLoanGroup
										key={group.playerId}
										player={playerById(group.playerId) ?? null}
										loans={group.loans}
										gameById={gameById}
										dateFormat={settings.dateFormat}
										onReturn={canWrite ? setReturnTargets : undefined}
										onEdit={canWrite ? requestEdit : undefined}
										onDelete={canWrite ? requestDelete : undefined}
									/>
								))}
							</div>
						)}
						{returnedLoans.length > 0 && (
							<div className="flex flex-col gap-2">
								<div className="flex items-center justify-between gap-3 rounded-lg border border-dashed border-primary/30 px-3 py-2 text-sm text-white/60">
									<span>
										{searching
											? t("history.summary-filtered", {
													matched: visibleReturned.length,
													count: returnedLoans.length,
												})
											: t("history.summary", { count: returnedLoans.length })}
									</span>
									<BgtButton
										variant="text"
										size="1"
										aria-expanded={showHistory}
										aria-controls={historyId}
										onClick={() => setShowHistory((value) => !value)}
									>
										{showHistory ? t("history.hide") : t("history.show")}
									</BgtButton>
								</div>
								{showHistory && (
									<ul id={historyId} className="divide-y divide-white/5 rounded-xl border border-white/10">
										{visibleReturned.map((loan) => (
											<LoanRow
												key={loan.id}
												loan={loan}
												game={gameById(loan.gameId) ?? undefined}
												player={playerById(loan.playerId)}
												showBorrower
												dateFormat={settings.dateFormat}
												onEdit={canWrite ? requestEdit : undefined}
												onDelete={canWrite ? requestDelete : undefined}
											/>
										))}
									</ul>
								)}
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
							loans={returnTargets}
							title={returnTitle}
							gameTitles={returnTargets.map(gameTitle)}
							open={returnTargets.length > 0}
							close={() => setReturnTargets([])}
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
