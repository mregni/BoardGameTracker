import { cva, cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import type { Loan } from "@/models/Loan/Loan";
import { getLoanStatus, getLoanStatusTone, type LoanStatus, toShortDate } from "../-utils/loanStatus";

const pillVariants = cva(
	"inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap",
	{
		variants: {
			tone: {
				red: "bg-error/15 text-red-400",
				amber: "bg-amber-400/15 text-amber-400",
				neutral: "bg-white/5 text-white/60",
			},
		},
		defaultVariants: {
			tone: "neutral",
		},
	},
);

const useStatusLabel = () => {
	const { t, i18n } = useTranslation("loans");

	return (loan: Loan, status: LoanStatus): string => {
		switch (status.kind) {
			case "overdue":
				return t("status.overdue", { count: status.days });
			case "due-today":
				return t("status.due-today");
			case "due-soon":
				return t("status.due-in", { count: status.days });
			case "due":
				return t("status.due-on", { date: toShortDate(status.date, i18n.language) });
			case "returned":
				return t("status.returned", { date: toShortDate(loan.returnedDate, i18n.language) });
			default:
				return t("status.no-due-date");
		}
	};
};

interface Props {
	loan: Loan;
	className?: string;
}

export const LoanStatusPill = ({ loan, className }: Props) => {
	const label = useStatusLabel();
	const status = getLoanStatus(loan);

	return (
		<span className={cx(pillVariants({ tone: getLoanStatusTone(status) }), className)}>{label(loan, status)}</span>
	);
};
