import { cva, cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import type { Loan } from "@/models/Loan/Loan";
import {
	formatDayCount,
	formatLoanDate,
	getLoanStatus,
	getLoanStatusTone,
	type LoanStatus,
} from "../-utils/loanStatus";

const pillVariants = cva(
	"inline-block max-w-full truncate rounded-full px-2.5 py-0.5 align-middle text-xs font-semibold whitespace-nowrap",
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

const useStatusLabel = (dateFormat: string) => {
	const { t, i18n } = useTranslation("loans");

	return (loan: Loan, status: LoanStatus): string => {
		switch (status.kind) {
			case "overdue":
				return t("status.overdue", { distance: formatDayCount(status.days, i18n.language) });
			case "due-today":
				return t("status.due-today");
			case "due-soon":
				return t("status.due-in", { distance: formatDayCount(status.days, i18n.language) });
			case "due":
				return t("status.due-on", { date: formatLoanDate(status.date, dateFormat, i18n.language) });
			case "returned":
				return t("status.returned", { date: formatLoanDate(loan.returnedDate, dateFormat, i18n.language) });
			default:
				return t("status.no-due-date");
		}
	};
};

interface Props {
	loan: Loan;
	dateFormat: string;
	className?: string;
}

export const LoanStatusPill = ({ loan, dateFormat, className }: Props) => {
	const label = useStatusLabel(dateFormat);
	const status = getLoanStatus(loan);
	const text = label(loan, status);

	return (
		<span title={text} className={cx(pillVariants({ tone: getLoanStatusTone(status) }), className)}>
			{text}
		</span>
	);
};
