import { cx } from "class-variance-authority";
import type { ComponentPropsWithoutRef } from "react";

import { type TextSize, textSizeClasses } from "../BgtText/textStyles";

type HeadingTag = "h1" | "h2" | "h3" | "h4";

interface Props extends Omit<ComponentPropsWithoutRef<"h3">, "color"> {
	size?: TextSize;
	as?: HeadingTag;
}

export const BgtHeading = (props: Props) => {
	const { children, className, size = "8", title, as: Tag = "h3", ...rest } = props;
	const tooltip = title ?? (typeof children === "string" ? children : undefined);

	return (
		<Tag
			className={cx(textSizeClasses[size], "font-bold line-clamp-2 break-words", className)}
			title={tooltip}
			{...rest}
		>
			{children}
		</Tag>
	);
};
