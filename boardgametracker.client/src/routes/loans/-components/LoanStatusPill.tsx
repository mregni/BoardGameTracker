import { cva } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import type { Loan } from "@/models/Loan/Loan";
import { formatDayCount, formatLoanDate, getLoanStatus, LOAN_STATUS_TONE } from "../-utils/loanStatus";

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
	},
);

interface Props {
	loan: Loan;
	dateFormat: string;
}

export const LoanStatusPill = ({ loan, dateFormat }: Props) => {
	const { t, i18n } = useTranslation("loans");
	const status = getLoanStatus(loan);

	let text: string;
	switch (status.kind) {
		case "overdue":
			text = t("status.overdue", { distance: formatDayCount(status.days, i18n.language) });
			break;
		case "due-today":
			text = t("status.due-today");
			break;
		case "due-soon":
			text = t("status.due-in", { distance: formatDayCount(status.days, i18n.language) });
			break;
		case "due":
			text = t("status.due-on", { date: formatLoanDate(status.date, dateFormat, i18n.language) });
			break;
		case "returned":
			text = t("status.returned", { date: formatLoanDate(status.date, dateFormat, i18n.language) });
			break;
		default:
			text = t("status.no-due-date");
	}

	return (
		<span title={text} className={pillVariants({ tone: LOAN_STATUS_TONE[status.kind] })}>
			{text}
		</span>
	);
};
