import type { AnyFieldApi } from "@tanstack/react-form";
import i18next from "i18next";
import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtDatePicker } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import type { Loan } from "@/models/Loan/Loan";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { localDateSchema } from "@/utils/localDate";

const ReturnLoanSchema = z.object({
	returnDate: localDateSchema("loans:return.required"),
});

interface Props {
	loans: Loan[];
	title: string;
	gameTitles: string[];
	open: boolean;
	close: () => void;
	onReturn: (loanIds: number[], returnDate: Date) => Promise<void>;
}

export const ReturnLoanModal = (props: Props) => {
	const { loans, title, gameTitles, open, close, onReturn } = props;
	const { t } = useTranslation(["loans", "common"]);
	const loanStart = loans
		.map((loan) => toInputDate(loan.loanDate, false))
		.reduce((latest, start) => (start > latest ? start : latest), "");

	const form = useAppForm({
		defaultValues: {
			returnDate: toInputDate(undefined, true),
		},
		onSubmit: async ({ value }) => {
			if (loans.length === 0) {
				return;
			}

			const { returnDate } = ReturnLoanSchema.parse(value);
			await onReturn(
				loans.map((loan) => loan.id),
				returnDate,
			);
			close();
		},
	});

	useEffect(() => {
		if (open) {
			form.reset({ returnDate: toInputDate(undefined, true) });
		}
	}, [open, form]);

	const validateReturnDate = ({ value }: { value: unknown }) => {
		const result = ReturnLoanSchema.shape.returnDate.safeParse(value);
		if (!result.success) {
			return i18next.t(result.error.issues[0].message);
		}
		if (loanStart && String(value) < loanStart) {
			return i18next.t("loans:return.before-start");
		}
		return undefined;
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{title}</BgtDialogTitle>
					<BgtDialogDescription>{t("return.description", { count: loans.length })}</BgtDialogDescription>
					{gameTitles.length > 1 && (
						<ul aria-label={t("return.games")} className="mt-3 flex flex-wrap gap-2">
							{gameTitles.map((gameTitle, index) => (
								<li
									key={`${gameTitle}-${loans[index]?.id}`}
									className="rounded-full bg-primary/15 px-3 py-1 text-sm text-white"
								>
									{gameTitle}
								</li>
							))}
						</ul>
					)}
					<div className="flex flex-col gap-3 mt-3 mb-3">
						<form.Field name="returnDate" validators={{ onChange: validateReturnDate }}>
							{(field: AnyFieldApi) => (
								<BgtDatePicker field={field} label={t("return.date")} minValue={loanStart || null} />
							)}
						</form.Field>
					</div>
					<BgtDialogFooter onCancel={close} confirmLabel={t("return.confirm")} confirmType="submit" />
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
