import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { memo, useId } from "react";
import { useDecimalInput } from "@/hooks/useDecimalInput";
import { parseLocalDate } from "@/utils/localDate";

import { BgtInputContainer } from "./BgtInputContainer";
import { FormFieldWrapper } from "./FormFieldWrapper";

export interface BgtInputFieldProps {
	field: AnyFieldApi;
	type: "text" | "number" | "decimal" | "datetime-local" | "date" | "password";
	placeholder?: string;
	label?: string;
	prefixLabel?: string;
	suffixLabel?: string;
	className?: string;
	disabled?: boolean;
	inputMode?: React.HTMLAttributes<HTMLInputElement>["inputMode"];
}

const formatDateToLocalInput = (date: Date) => {
	try {
		return format(date, "yyyy-MM-dd'T'HH:mm");
	} catch {
		return "";
	}
};

const formatInput = (input: string | number | Date | undefined, type: BgtInputFieldProps["type"]) => {
	if (input === undefined || input === null || input === "") {
		return "";
	}

	if (type === "datetime-local") {
		return formatDateToLocalInput(new Date(input));
	}
	if (type === "date") {
		try {
			return format(parseLocalDate(input) ?? new Date(input), "yyyy-MM-dd");
		} catch {
			return "";
		}
	}

	return String(input);
};

const BgtInputFieldComponent = (props: BgtInputFieldProps) => {
	const {
		field,
		type,
		placeholder = "",
		label,
		prefixLabel = undefined,
		suffixLabel = undefined,
		className = "",
		disabled = false,
		inputMode,
	} = props;

	const id = useId();
	const hasErrors = field.state.meta.errors.length > 0;
	const isDecimal = type === "decimal";
	const decimal = useDecimalInput(field.state.value, (value) => field.handleChange(value ?? undefined));
	const displayValue = isDecimal ? decimal.value : formatInput(field.state.value, type);

	const handleChange = (raw: string) => {
		if (isDecimal) {
			decimal.onChange(raw);
			return;
		}
		field.handleChange(type === "number" ? (raw === "" ? undefined : Number(raw)) : raw);
	};

	const handleBlur = () => {
		decimal.onBlur();
		field.handleBlur();
	};

	return (
		<FormFieldWrapper label={label} htmlFor={id} errors={field.state.meta.errors} className="w-full">
			<BgtInputContainer
				prefix={prefixLabel}
				suffix={suffixLabel}
				hasErrors={hasErrors}
				disabled={disabled}
				className={cx("h-11 md:h-10 text-[15px]", className)}
			>
				<input
					id={id}
					className="h-full w-full bg-transparent text-white focus:outline-none disabled:cursor-not-allowed"
					value={displayValue}
					disabled={disabled}
					type={isDecimal ? "text" : type}
					inputMode={isDecimal ? (inputMode ?? "decimal") : inputMode}
					autoComplete={isDecimal ? "off" : undefined}
					onChange={(event) => handleChange(event.target.value)}
					onBlur={handleBlur}
					placeholder={placeholder}
				/>
			</BgtInputContainer>
		</FormFieldWrapper>
	);
};

BgtInputFieldComponent.displayName = "BgtInputField";

export const BgtInputField = memo(BgtInputFieldComponent);
