import { cx } from "class-variance-authority";
import type { ComponentPropsWithoutRef } from "react";

/**
 * Shared label for form fields. Centralises the label styling that used to be
 * duplicated across the form components. Tune `leading-7` here to adjust the
 * vertical spacing of every field label app-wide.
 */
export const BgtFieldLabel = ({ className, children, ...rest }: ComponentPropsWithoutRef<"div">) => (
	<div className={cx("text-[15px] font-medium leading-7", className)} {...rest}>
		{children}
	</div>
);
