import type { ComponentType, MouseEventHandler, SVGProps } from "react";

export interface Actions {
	variant?: "primary" | "error" | "cancel" | "text";
	onClick: MouseEventHandler<HTMLButtonElement>;
	content: string | React.ReactNode;
	icon?: ComponentType<SVGProps<SVGSVGElement>>;
}
