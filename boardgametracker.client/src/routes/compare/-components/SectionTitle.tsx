import type { ReactNode } from "react";

interface Props {
	id?: string;
	children: ReactNode;
}

export const SectionTitle = ({ id, children }: Props) => (
	<h2
		id={id}
		className="flex items-center gap-2.5 text-lg font-extrabold uppercase tracking-wider after:h-0.5 after:flex-1 after:bg-linear-to-r after:from-player-one/60 after:to-player-two/60"
	>
		{children}
	</h2>
);
