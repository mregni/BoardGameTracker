import { type KeyboardEvent, useEffect, useState } from "react";
import { BgtInputContainer } from "@/components/BgtForm";

interface Props {
	value: number | null;
	onChange: (value: number | null) => void;
	step?: number;
	min?: number;
	max?: number;
	prefix?: string;
	suffix?: string;
	className?: string;
}

export const EditableNumberCell = ({ value, onChange, step = 1, min, max, prefix, suffix, className }: Props) => {
	const [draft, setDraft] = useState(value?.toString() ?? "");

	useEffect(() => {
		setDraft(value?.toString() ?? "");
	}, [value]);

	const commit = () => {
		const trimmed = draft.trim();
		const parsed = trimmed === "" ? null : Number(trimmed);
		if (parsed !== null && Number.isNaN(parsed)) {
			setDraft(value?.toString() ?? "");
			return;
		}
		if (parsed === value) {
			return;
		}
		onChange(parsed);
	};

	const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
		if (event.key === "Enter") {
			event.currentTarget.blur();
		}
		if (event.key === "Escape") {
			setDraft(value?.toString() ?? "");
			event.currentTarget.blur();
		}
	};

	return (
		<BgtInputContainer prefix={prefix} suffix={suffix} className={className ?? "h-9 w-24 text-[12px]"}>
			<input
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
