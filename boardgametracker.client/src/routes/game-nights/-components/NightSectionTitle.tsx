import { cx } from "class-variance-authority";
import type { ReactNode } from "react";

interface Props {
	id: string;
	children: ReactNode;
	keepCase?: boolean;
}

export const NightSectionTitle = ({ id, children, keepCase = false }: Props) => (
	<h2
		id={id}
		className={cx(
			"flex items-center gap-3 text-xs tracking-widest text-gray-400 after:h-px after:flex-1 after:bg-white/10",
			!keepCase && "uppercase",
		)}
	>
		{children}
	</h2>
);
