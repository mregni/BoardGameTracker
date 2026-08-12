import { TextArea } from "@radix-ui/themes";
import { cx } from "class-variance-authority";
import { type KeyboardEvent, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtButton } from "@/components/BgtButton/BgtButton";

interface Props {
	disabled: boolean;
	pending: boolean;
	placeholder: string;
	onSend: (question: string) => void;
}

export const ChatComposer = ({ disabled, pending, placeholder, onSend }: Props) => {
	const { t } = useTranslation("chat");
	const [value, setValue] = useState("");
	const textAreaRef = useRef<HTMLTextAreaElement>(null);

	const submit = () => {
		const trimmed = value.trim();
		if (trimmed.length === 0 || disabled || pending) {
			return;
		}
		onSend(trimmed);
		setValue("");
		textAreaRef.current?.focus();
	};

	const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
		if (event.key === "Enter" && !event.shiftKey) {
			event.preventDefault();
			submit();
		}
	};

	return (
		<div className="flex items-end gap-2 pt-2">
			<TextArea
				ref={textAreaRef}
				className={cx(
					"flex-1 rounded-lg! border! border-primary/30! bg-background! shadow-none! focus:border-primary!",
				)}
				rows={2}
				value={value}
				disabled={disabled}
				placeholder={placeholder}
				onChange={(event) => setValue(event.target.value)}
				onKeyDown={onKeyDown}
			/>
			<BgtButton onClick={submit} disabled={disabled || pending || value.trim().length === 0}>
				{t("composer.send")}
			</BgtButton>
		</div>
	);
};
