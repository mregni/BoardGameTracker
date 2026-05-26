import { cx } from "class-variance-authority";
import { format } from "date-fns";
import type { ChangeEventHandler, ReactNode } from "react";

import { BgtText } from "../BgtText/BgtText";
import { BgtFieldLabel } from "./BgtFieldLabel";

const formatInput = (input: string | number | Date | undefined) => {
	if (input === undefined || input === null) {
		return "";
	}

	if (input instanceof Date) {
		return format(input, "yyyy-MM-dd");
	}

	return input;
};

export interface Props {
	type: "text" | "number" | "date";
	placeholder?: string;
	label?: string;
	value: string | number | Date | undefined;
	prefixLabel?: string | ReactNode;
	suffixLabel?: string | ReactNode;
	className?: string;
	inputClassName?: string;
	disabled?: boolean;
	onChange: ChangeEventHandler<HTMLInputElement> | undefined;
}

export const BgtSimpleInputField = (props: Props) => {
	const {
		type,
		placeholder = "",
		value,
		label = undefined,
		prefixLabel = undefined,
		suffixLabel = undefined,
		className = "",
		inputClassName = "",
		disabled = false,
		onChange,
	} = props;

	return (
		<div className="flex flex-col justify-start w-full">
			<div className="flex items-baseline justify-between">
				<BgtFieldLabel>{label}</BgtFieldLabel>
			</div>
			<div
				className={cx(
					"rounded-lg bg-input px-4 flex flex-row gap-2 items-center text-sm",
					className,
					disabled && "text-gray-500",
				)}
			>
				{prefixLabel && (typeof prefixLabel === "string" ? <BgtText>{prefixLabel}</BgtText> : prefixLabel)}
				<input
					className={cx(inputClassName, "h-9 bg-transparent shadow-none focus:outline-hidden hide-arrow w-full")}
					value={formatInput(value)}
					disabled={disabled}
					type={type}
					onChange={onChange}
					placeholder={placeholder}
				/>
				{suffixLabel && (typeof suffixLabel === "string" ? <BgtText>{suffixLabel}</BgtText> : suffixLabel)}
			</div>
		</div>
	);
};
