import type { AnyFieldApi } from "@tanstack/react-form";
import i18next from "i18next";
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
import type { Loan } from "@/models/Loan/Loan";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { localDateSchema } from "@/utils/localDate";

const ReturnLoanSchema = z.object({
	returnDate: localDateSchema("loans:return.required"),
});

interface Props {
	loan: Loan | null;
	gameTitle: string;
	open: boolean;
	close: () => void;
	onReturn: (loanId: number, returnDate: Date) => Promise<void>;
}

export const ReturnLoanModal = (props: Props) => {
	const { loan, gameTitle, open, close, onReturn } = props;
	const { t } = useTranslation(["loans", "common"]);
	const loanStart = toInputDate(loan?.loanDate ?? undefined, false);

	const form = useAppForm({
		defaultValues: {
			returnDate: toInputDate(undefined, true),
		},
		onSubmit: async ({ value }) => {
			if (loan === null) {
				return;
			}

			const { returnDate } = ReturnLoanSchema.parse(value);
			await onReturn(loan.id, returnDate);
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
					<BgtDialogTitle>{t("return.title", { game: gameTitle })}</BgtDialogTitle>
					<BgtDialogDescription>{t("return.description")}</BgtDialogDescription>
					<div className="flex flex-col gap-3 mt-3 mb-3">
						<form.Field name="returnDate" validators={{ onChange: validateReturnDate }}>
							{(field: AnyFieldApi) => (
								<BgtDatePicker field={field} label={t("return.date")} minValue={loanStart || null} />
							)}
						</form.Field>
					</div>
					<BgtDialogClose>
						<BgtButton type="button" variant="cancel" className="flex-1" onClick={close}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton type="submit" className="flex-1" variant="primary">
							{t("return.confirm")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
