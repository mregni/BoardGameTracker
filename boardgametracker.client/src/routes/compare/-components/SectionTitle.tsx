import type { ReactNode } from "react";

interface Props {
	id?: string;
	children: ReactNode;
}

export const SectionTitle = ({ id, children }: Props) => (
	<h2
		id={id}
		className="flex items-center gap-2.5 text-xs uppercase tracking-widest text-white/60 after:h-px after:flex-1 after:bg-card-border"
	>
		{children}
	</h2>
);
