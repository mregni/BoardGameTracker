import { cx } from "class-variance-authority";
import type { ComponentPropsWithoutRef } from "react";

import { type TextSize, textSizeClasses } from "../BgtText/textStyles";

interface Props extends Omit<ComponentPropsWithoutRef<"h3">, "color"> {
	size?: TextSize;
}

export const BgtHeading = (props: Props) => {
	const { children, className, size = "8", ...rest } = props;

	return (
		<h3 className={cx(textSizeClasses[size], "font-bold line-clamp-1", className)} {...rest}>
			{children}
		</h3>
	);
};
