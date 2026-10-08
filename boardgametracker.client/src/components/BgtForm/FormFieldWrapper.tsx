import { memo, type ReactNode } from "react";

import { BgtFieldLabel } from "./BgtFieldLabel";
import { BgtFormErrors } from "./BgtFormErrors";

interface FormFieldWrapperProps {
	label?: string;
	htmlFor?: string;
	errors?: string[];
	children: ReactNode;
	className?: string;
}

const FormFieldWrapperComponent = ({
	label,
	htmlFor,
	errors = [],
	children,
	className = "",
}: FormFieldWrapperProps) => (
	<div className={`flex flex-col justify-start ${className}`}>
		{label && (
			<div className="flex items-baseline justify-between">
				{htmlFor ? (
					<label htmlFor={htmlFor} className="text-[15px] font-medium leading-7">
						{label}
					</label>
				) : (
					<BgtFieldLabel>{label}</BgtFieldLabel>
				)}
				<BgtFormErrors errors={errors} />
			</div>
		)}
		{children}
	</div>
);

FormFieldWrapperComponent.displayName = "FormFieldWrapper";

export const FormFieldWrapper = memo(FormFieldWrapperComponent);
