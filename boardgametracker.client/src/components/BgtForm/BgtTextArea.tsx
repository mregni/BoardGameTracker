import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import { memo, useCallback } from "react";

import { FormFieldWrapper } from "./FormFieldWrapper";

export interface BgtTextAreaProps {
	field: AnyFieldApi;
	disabled?: boolean;
	label: string;
	className?: string;
}

const BgtTextAreaComponent = (props: BgtTextAreaProps) => {
	const { field, disabled = false, className, label } = props;

	const handleChange = useCallback(
		(event: React.ChangeEvent<HTMLTextAreaElement>) => {
			field.handleChange(event.target.value);
		},
		[field],
	);

	return (
		<FormFieldWrapper label={label} errors={field.state.meta.errors}>
			<textarea
				className={cx(
					"w-full rounded-lg border border-primary/30 bg-background px-3 py-2 text-[15px] text-white focus:border-primary focus:outline-none disabled:cursor-not-allowed disabled:opacity-60",
					className,
				)}
				rows={4}
				disabled={disabled}
				value={field.state.value ?? ""}
				onChange={handleChange}
				onBlur={field.handleBlur}
			/>
		</FormFieldWrapper>
	);
};

export const BgtTextArea = memo(BgtTextAreaComponent);
