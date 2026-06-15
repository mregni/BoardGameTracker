import { cx } from "class-variance-authority";
import { type KeyboardEvent, useEffect, useState } from "react";

interface Props {
	value: number | null;
	onChange: (value: number | null) => void;
	step?: number;
	min?: number;
	max?: number;
	prefix?: string;
	className?: string;
}

export const EditableNumberCell = ({ value, onChange, step = 1, min, max, prefix, className }: Props) => {
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
		<div
			className={cx(
				"inline-flex items-center gap-1 h-9 w-24 px-3 bg-background text-white text-[12px] rounded-lg border border-primary/30 focus-within:border-primary",
				className,
			)}
		>
			{prefix && <span className="text-cancel">{prefix}</span>}
			<input
				type="number"
				value={draft}
				step={step}
				min={min}
				max={max}
				onChange={(event) => setDraft(event.target.value)}
				onBlur={commit}
				onKeyDown={onKeyDown}
				className="w-full min-w-0 bg-transparent outline-none"
			/>
		</div>
	);
};
