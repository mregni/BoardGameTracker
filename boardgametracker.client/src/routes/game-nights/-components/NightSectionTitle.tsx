import type { ReactNode } from "react";

interface Props {
	id: string;
	children: ReactNode;
}

export const NightSectionTitle = ({ id, children }: Props) => (
	<h2
		id={id}
		className="flex items-center gap-3 text-base font-semibold text-white after:h-px after:flex-1 after:bg-white/10"
	>
		{children}
	</h2>
);
