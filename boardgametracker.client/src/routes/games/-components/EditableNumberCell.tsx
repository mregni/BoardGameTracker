import { type KeyboardEvent, useEffect, useRef, useState } from "react";
import { BgtInputContainer } from "@/components/BgtForm";
import { EditableCellButton } from "./EditableCellButton";

interface Props {
	value: number | null;
	editing: boolean;
	onStartEdit: () => void;
	onStopEdit: () => void;
	onChange: (value: number | null) => void;
	step?: number;
	min?: number;
	max?: number;
	prefix?: string;
	suffix?: string;
	format?: (value: number) => string;
	className?: string;
	align?: "left" | "right";
	readOnly?: boolean;
	size?: "xs" | "sm";
}

type EditorProps = Omit<Props, "editing" | "onStartEdit" | "align" | "readOnly" | "format">;

const NumberEditor = ({ value, onStopEdit, onChange, step = 1, min, max, prefix, suffix, className }: EditorProps) => {
	const [draft, setDraft] = useState(value?.toString() ?? "");
	const inputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		inputRef.current?.focus();
	}, []);

	const commit = () => {
		const trimmed = draft.trim();
		const parsed = trimmed === "" ? null : Number(trimmed);
		if (parsed !== value && !(parsed !== null && Number.isNaN(parsed))) {
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
				type="number"
				value={draft}
				step={step}
				min={min}
				max={max}
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

	if (!editing || readOnly) {
		return (
			<EditableCellButton onClick={onStartEdit} className={className} align={align} readOnly={readOnly} size={size}>
				{value == null
					? "-"
					: format
						? format(value)
						: [prefix, value, suffix].filter((part) => part != null && part !== "").join(" ")}
			</EditableCellButton>
		);
	}

	const { format: _format, ...editorProps } = props;
	return <NumberEditor {...editorProps} />;
};
