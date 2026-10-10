import { cx } from "class-variance-authority";
import type { ComponentPropsWithoutRef } from "react";

import { type TextSize, textSizeClasses } from "../BgtText/textStyles";

interface Props extends Omit<ComponentPropsWithoutRef<"h3">, "color"> {
	size?: TextSize;
}

export const BgtHeading = (props: Props) => {
	const { children, className, size = "8", title, ...rest } = props;
	const tooltip = title ?? (typeof children === "string" ? children : undefined);

	return (
		<h3
			className={cx(textSizeClasses[size], "font-bold line-clamp-2 break-words", className)}
			title={tooltip}
			{...rest}
		>
			{children}
		</h3>
	);
};
