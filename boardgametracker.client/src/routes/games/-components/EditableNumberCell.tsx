import { type KeyboardEvent, useEffect, useRef, useState } from "react";
import { BgtInputContainer } from "@/components/BgtForm";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { EMPTY_PRICE, parseDecimal } from "@/utils/numberUtils";
import { EditableCellButton } from "./EditableCellButton";

interface Props {
	value: number | null;
	editing: boolean;
	onStartEdit: () => void;
	onStopEdit: () => void;
	onChange: (value: number | null) => void;
	min?: number;
	max?: number;
	prefix?: string;
	suffix?: string;
	format?: (value: number) => string;
	className?: string;
	align?: "left" | "right";
	readOnly?: boolean;
	size?: "xs" | "sm";
	ariaLabel?: string;
}

type EditorProps = Omit<Props, "editing" | "onStartEdit" | "align" | "readOnly" | "format">;

const isOutOfRange = (value: number, min?: number, max?: number) =>
	(min !== undefined && value < min) || (max !== undefined && value > max);

const NumberEditor = ({ value, onStopEdit, onChange, min, max, prefix, suffix, className, ariaLabel }: EditorProps) => {
	const { input, locale } = useNumberFormat();
	const [draft, setDraft] = useState(() => input(value));
	const inputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		inputRef.current?.focus();
	}, []);

	const commit = () => {
		const trimmed = draft.trim();
		const parsed = parseDecimal(trimmed, locale);
		const invalid = trimmed !== "" && (parsed === null || isOutOfRange(parsed, min, max));
		if (!invalid && parsed !== value) {
			onChange(parsed);
		}
		onStopEdit();
	};

	const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
		if (event.key === "Enter") {
			commit();
		}
		if (event.key === "Escape") {
			onStopEdit();
		}
	};

	return (
		<BgtInputContainer prefix={prefix} suffix={suffix} className={className ?? "h-9 w-24 text-[12px]"}>
			<input
				ref={inputRef}
				type="text"
				inputMode="decimal"
				autoComplete="off"
				aria-label={ariaLabel}
				value={draft}
				onChange={(event) => setDraft(event.target.value)}
				onBlur={commit}
				onKeyDown={onKeyDown}
				className="w-full min-w-0 bg-transparent text-white outline-none"
			/>
		</BgtInputContainer>
	);
};

export const EditableNumberCell = (props: Props) => {
	const { value, editing, onStartEdit, prefix, suffix, format, className, align, readOnly, size } = props;
	const { decimal } = useNumberFormat();

	if (!editing || readOnly) {
		return (
			<EditableCellButton onClick={onStartEdit} className={className} align={align} readOnly={readOnly} size={size}>
				{value == null
					? EMPTY_PRICE
					: format
						? format(value)
						: [prefix, decimal(value, 2), suffix].filter((part) => part != null && part !== "").join(" ")}
			</EditableCellButton>
		);
	}

	const { format: _format, ...editorProps } = props;
	return <NumberEditor {...editorProps} />;
};
