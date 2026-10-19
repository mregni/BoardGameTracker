import { useRef, useState } from "react";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtSimpleInputField } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";

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
	note?: { text: string; linkLabel: string; href: string };
}

export const ResetConfirmModal = (props: Props) => {
	const {
		open,
		close,
		onConfirm,
		isLoading = false,
		title,
		description,
		keyword,
		inputLabel,
		confirmLabel,
		note,
	} = props;
	const [value, setValue] = useState("");
	const fieldRef = useRef<HTMLDivElement>(null);

	const focusConfirmField = (event: Event) => {
		const field = fieldRef.current?.querySelector("input");
		if (field) {
			event.preventDefault();
			field.focus();
		}
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent onOpenAutoFocus={focusConfirmField}>
				<BgtDialogTitle>{title}</BgtDialogTitle>
				<BgtDialogDescription>{description}</BgtDialogDescription>
				{note && (
					<BgtText size="2" color="gray">
						{note.text}{" "}
						<a href={note.href} target="_blank" rel="noreferrer" className="text-primary-text underline">
							{note.linkLabel}
						</a>
					</BgtText>
				)}
				<div ref={fieldRef}>
					<BgtSimpleInputField
						type="text"
						label={inputLabel}
						value={value}
						onChange={(event) => setValue(event.target.value)}
					/>
				</div>
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
