import { cx } from "class-variance-authority";
import type { ReactNode } from "react";

import { BgtText } from "../BgtText/BgtText";

interface Props {
	children: ReactNode;
	prefix?: string;
	suffix?: string;
	className?: string;
	hasErrors?: boolean;
	disabled?: boolean;
}

export const BgtInputContainer = ({
	children,
	prefix,
	suffix,
	className,
	hasErrors = false,
	disabled = false,
}: Props) => (
	<div
		className={cx(
			"flex flex-row gap-2 items-center rounded-lg bg-background border border-primary/30 px-4 focus-within:border-primary",
			className,
			hasErrors && "border-error bg-error/5!",
			disabled && "opacity-50 cursor-not-allowed",
		)}
	>
		{prefix && <BgtText color="gray">{prefix}</BgtText>}
		{children}
		{suffix && <BgtText color="gray">{suffix}</BgtText>}
	</div>
);
