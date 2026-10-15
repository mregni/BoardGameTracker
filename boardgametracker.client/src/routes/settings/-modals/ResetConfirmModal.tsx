import { useState } from "react";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
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
	const [value, setValue] = useState("");

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{title}</BgtDialogTitle>
				<BgtDialogDescription>{description}</BgtDialogDescription>
				<BgtSimpleInputField
					type="text"
					label={inputLabel}
					value={value}
					onChange={(event) => setValue(event.target.value)}
				/>
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={confirmLabel}
					onConfirm={onConfirm}
					variant="destructive"
					disabled={value !== keyword || isLoading}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
