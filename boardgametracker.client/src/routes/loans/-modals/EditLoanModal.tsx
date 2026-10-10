import type { AnyFieldApi } from "@tanstack/react-form";
import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtDatePicker } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import { CreateLoanSchema } from "@/models/Loan/CreateLoan";
import type { Loan } from "@/models/Loan/Loan";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { parseLocalDate } from "@/utils/localDate";
import { notBeforeValidator, zodValidator } from "@/utils/zodValidator";

const LoanDatesSchema = CreateLoanSchema.pick({ loanDate: true, dueDate: true }).extend({
	returnedDate: z
		.string()
		.optional()
		.transform((val) => parseLocalDate(val) ?? null)
		.nullable(),
});

interface Props {
	loan: Loan | null;
	open: boolean;
	close: () => void;
	disabled?: boolean;
	onSave: (loan: Loan) => Promise<void>;
}

export const EditLoanModal = (props: Props) => {
	const { loan, open, close, disabled = false, onSave } = props;
	const { t } = useTranslation(["loans", "common"]);

	const form = useAppForm({
		defaultValues: {
			loanDate: toInputDate(loan?.loanDate ?? undefined, true),
			dueDate: toInputDate(loan?.dueDate ?? undefined, false),
			returnedDate: toInputDate(loan?.returnedDate ?? undefined, false),
		},
		onSubmit: async ({ value }) => {
			if (loan === null) {
				return;
			}

			const dates = LoanDatesSchema.parse(value);
			await onSave({
				...loan,
				loanDate: dates.loanDate,
				dueDate: dates.dueDate,
				returnedDate: loan.returnedDate ? (dates.returnedDate ?? loan.returnedDate) : null,
			});
			close();
		},
	});

	useEffect(() => {
		form.reset({
			loanDate: toInputDate(loan?.loanDate ?? undefined, true),
			dueDate: toInputDate(loan?.dueDate ?? undefined, false),
			returnedDate: toInputDate(loan?.returnedDate ?? undefined, false),
		});
	}, [loan, form]);

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("edit.title")}</BgtDialogTitle>
					<BgtDialogDescription>{t("edit.description")}</BgtDialogDescription>
					<div className="flex flex-col gap-3 mt-3 mb-3">
						<form.Field name="loanDate" validators={zodValidator(LoanDatesSchema, "loanDate")}>
							{(field: AnyFieldApi) => (
								<BgtDatePicker
									field={field}
									label={t("new.start.label")}
									disabled={disabled}
									placeholder={t("new.start.placeholder")}
								/>
							)}
						</form.Field>
						<form.Field
							name="dueDate"
							validators={notBeforeValidator(LoanDatesSchema, "dueDate", "loanDate", "loans:new.end.before-start")}
						>
							{(field: AnyFieldApi) => (
								<form.Subscribe selector={(state) => state.values.loanDate}>
									{(loanDate) => (
										<BgtDatePicker
											field={field}
											label={t("new.end.label")}
											disabled={disabled}
											placeholder={t("new.end.placeholder")}
											minValue={loanDate || null}
											clearable
										/>
									)}
								</form.Subscribe>
							)}
						</form.Field>
						{loan?.returnedDate && (
							<form.Field
								name="returnedDate"
								validators={notBeforeValidator(
									LoanDatesSchema,
									"returnedDate",
									"loanDate",
									"loans:return.before-start",
								)}
							>
								{(field: AnyFieldApi) => (
									<form.Subscribe selector={(state) => state.values.loanDate}>
										{(loanDate) => (
											<BgtDatePicker
												field={field}
												label={t("return.date")}
												disabled={disabled}
												minValue={loanDate || null}
											/>
										)}
									</form.Subscribe>
								)}
							</form.Field>
						)}
					</div>
					<BgtDialogClose>
						<BgtButton type="button" disabled={disabled} variant="cancel" className="flex-1" onClick={close}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton type="submit" disabled={disabled} className="flex-1" variant="primary">
							{t("edit.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
