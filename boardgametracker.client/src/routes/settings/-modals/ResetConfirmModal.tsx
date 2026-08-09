import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog/BgtDialog";
import { BgtSimpleInputField } from "@/components/BgtForm";

interface Props {
	open: boolean;
	close: () => void;
	onConfirm: () => Promise<void>;
	isLoading?: boolean;
	title: string;
	description: string;
	keyword: string;
	inputLabel: string;
	confirmLabel: string;
}

export const ResetConfirmModal = (props: Props) => {
	const { open, close, onConfirm, isLoading = false, title, description, keyword, inputLabel, confirmLabel } = props;
	const { t } = useTranslation();
	const [value, setValue] = useState("");

	return (
		<BgtDialog open={open}>
			<BgtDialogContent>
				<BgtDialogTitle>{title}</BgtDialogTitle>
				<BgtDialogDescription>{description}</BgtDialogDescription>
				<BgtSimpleInputField
					type="text"
					label={inputLabel}
					value={value}
					onChange={(event) => setValue(event.target.value)}
				/>
				<BgtDialogClose>
					<BgtButton variant="cancel" onClick={close}>
						{t("cancel")}
					</BgtButton>
					<BgtButton variant="error" disabled={value !== keyword || isLoading} onClick={onConfirm}>
						{confirmLabel}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
