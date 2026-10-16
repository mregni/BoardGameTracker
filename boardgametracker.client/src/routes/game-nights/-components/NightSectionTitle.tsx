import type { ReactNode } from "react";

export const NightSectionTitle = ({ id, children }: { id: string; children: ReactNode }) => (
	<h2
		id={id}
		className="flex items-center gap-3 text-xs tracking-widest text-gray-400 uppercase after:h-px after:flex-1 after:bg-white/10"
	>
		{children}
	</h2>
);
